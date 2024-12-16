using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// 서버 Bind, Listen, Accept를 한다.
/// </summary>
class Listener
{
    private SocketAsyncEventArgs _acceptArgs;
    private Socket _listenSocket;

    // 접속 완료시 호출되는 event
    public event Action<Socket> onClientConnected;

    public bool Start(int port, int backlog)
    {
        _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, port);

        try
        {
            _listenSocket.Bind(endpoint);
            _listenSocket.Listen(backlog);

            _acceptArgs = new SocketAsyncEventArgs();
            _acceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            StartAccept();
        }
        catch (Exception e)
        {
            Debug.LogError($"Start Error {e.Message}");
            return false;
        }

        return true;
    }

    public void Stop()
    {
        _listenSocket.Close();
    }

    private void StartAccept()
    {
        // 재사용을 위해 null을 대입
        _acceptArgs.AcceptSocket = null;
        bool pending = false;
        try
        {
            // 비동기 Accept
            pending = _listenSocket.AcceptAsync(_acceptArgs);
        }
        catch
        {
        }

        // 대기하지않고 바로 Accept가 되었다면 수행
        if (!pending)
        {
            OnAcceptCompleted(null, _acceptArgs);
        }
    }

    /// <summary>
    /// AcceptAsync의 콜백 매소드
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">AcceptAsync 매소드 호출시 사용된 EventArgs</param>
    void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            Debug.Log("Accept Success");
            onClientConnected?.Invoke(e.AcceptSocket);
        }
        else
        {
            Debug.Log("Accept Fail");
        }

        // 다음 연결을 받는다.
        StartAccept();
    }
}