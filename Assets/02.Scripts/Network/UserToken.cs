using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// 소켓을 사용 하여 송,수신등을 하기위한 클래스
/// </summary>
public class UserToken
{
    // 소켓에 대한 상태
    private enum EState
    {
        Idle,               // 대기중
        Connected,          // 연결됨
        ReserveClosing,     // 종료가 예약됨, 남아있는 패킷을 모두 보낸 뒤 끊도록 하기 위한 상태값
        Closed              // 소켓이 완전 종료됨
    }

    private Socket _socket;
    private EState _curState = EState.Idle;

    private SocketAsyncEventArgs _receiveEventArgs;
    private SocketAsyncEventArgs _sendEventArgs;
    private MessageResolver _messageResolver = new MessageResolver(NetDefine.BUFFER_SIZE * 3);
    private IPeer _peer; // Peer 객체. 어플리케이션에서 구현하여 사용. 패킷처리나 추가적인 동작을 한다.
    private List<byte[]> _sendingList = new List<byte[]>();
    private IMessageDispatcher _dispatcher; // 패킷 메시지를 메인스레드에서 처리하기위한 Dispatcher

    public Socket Socket => _socket;
    public SocketAsyncEventArgs ReceiveEventArgs => _receiveEventArgs;
    public SocketAsyncEventArgs SendEventArgs => _sendEventArgs;
    public bool IsConnected => _curState == EState.Connected;
    public IPeer Peer => _peer;

    public event Action<UserToken> onSessionClosed; // Close었을시 호출되는 event

    public UserToken(Socket socket, IMessageDispatcher dispatcher)
    {
        _socket = socket;
        _dispatcher = dispatcher;

        // Receive용 SocketAsyncEventArgs 생성
        _receiveEventArgs = new SocketAsyncEventArgs();
        _receiveEventArgs.UserToken = this;
        _receiveEventArgs.Completed += OnReceiveCompleted;
        _receiveEventArgs.SetBuffer(new byte[NetDefine.BUFFER_SIZE], 0, NetDefine.BUFFER_SIZE);
        // Send용 SocketAsyncEventArgs 생성
        _sendEventArgs = new SocketAsyncEventArgs();
        _sendEventArgs.UserToken = this;
        _sendEventArgs.Completed += OnSendComplteted;
    }

    public void OnConnected()
    {
        _curState = EState.Connected;
    }

    public void SetPeer(IPeer peer)
    {
        _peer = peer;
    }

    public void StartReceive()
    {
        bool pending = false;
        try
        {
            // 비동기 Receive
            pending = _socket.ReceiveAsync(_receiveEventArgs);
        }
        catch
        {
        }

        // 대기하지않고 바로 Receive가 되었다면 수행
        if (!pending)
        {
            OnReceiveCompleted(null, _receiveEventArgs);
        }
    }

    private void OnReceiveCompleted(object sender, SocketAsyncEventArgs e)
    {
        if (e.LastOperation == SocketAsyncOperation.Receive)
        {
            _messageResolver.OnReceive(e.Buffer, e.Offset, e.BytesTransferred, OnMessageComplete);
        }
        else
        {
            _socket.Close();
        }

        // 다시 Receive 시작
        StartReceive();
    }

    private void OnMessageComplete(byte[] buffer)
    {
        // 메인스레드에서 패킷을 처리하게 한다.
        _dispatcher.OnMessage(this, buffer);
    }

    public void OnMessage(byte[] buffer)
    {
        if (_peer == null)
            return;

        // protocolID 가져온다
        short protocolID = BitConverter.ToInt16(buffer, 2);
        _peer.ProcessMessage(protocolID, buffer);
    }

    public void Close()
    {
        if (_curState == EState.Closed)
        {
            return;
        }

        _curState = EState.Closed;
        _socket.Close();

        _socket = null;

        _sendingList.Clear();

        MainThreadDispatcher.Instance.Add(() =>
        {
            onSessionClosed?.Invoke(this);
        });
       
        _peer.Remove();

        Debug.Log("Close");
    }

    public void Send(byte[] data)
    {
        lock (_sendingList)
        {
            _sendingList.Add(data);

            if (_sendingList.Count > 1)
            {
                // 큐에 무언가가 들어 있다면 아직 이전 전송이 완료되지 않은 상태이므로 큐에 추가만 하고 리턴한다.
                // 현재 수행중인 SendAsync가 완료된 이후에 큐를 검사하여 데이터가 있으면 SendAsync를 호출하여 전송해줄 것이다.
                return;
            }

            StartSend();
        }
    }

    public void Send(Packet packet)
    {
        Send(packet.ToByte());
    }

    /// <summary>
    /// 비동기 전송을 시작한다.
    /// </summary>
    void StartSend()
    {
        try
        {
            // Send는 이때 버퍼를 채워준다.
            _sendEventArgs.SetBuffer(_sendingList[0], 0, _sendingList[0].Length);

            // 비동기 전송 시작.
            bool pending = _socket.SendAsync(_sendEventArgs);
            if (!pending)
            {
                OnSendComplteted(null, _sendEventArgs);
            }
        }
        catch (Exception e)
        {
            if (_socket == null)
            {
                Close();
                return;
            }

            Debug.LogError("send error!! close socket. " + e.Message);
        }
    }

    public void OnSendComplteted(object sender, SocketAsyncEventArgs e)
    {
        if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success)
        {
            // 연결이 끊겨서 이미 소켓이 종료된 경우일 것이다.
            return;
        }

        lock (_sendingList)
        {
            // 보낸 내용 삭제
            _sendingList.RemoveAt(0);
            // 보낼것이 남아있다면 보낸다.
            if (_sendingList.Count > 0)
            {
                StartSend();
                return;
            }

            // 종료가 예약된 경우, 보낼건 다 보냈으니 진짜 종료 처리를 진행한다.
            if (_curState == EState.ReserveClosing)
            {
                _socket.Shutdown(SocketShutdown.Send);
            }
        }
    }

    /// <summary>
    /// 클라이언트에서 연결 종료시 사용.
    /// </summary>
    public void Disconnect()
    {
        try
        {
            if (_sendingList.Count <= 0)
            {
                _socket.Shutdown(SocketShutdown.Send);
                return;
            }

            // 보낼게 남아있다면
            _curState = EState.ReserveClosing;
        }
        catch (Exception)
        {
            Close();
        }
    }
}
