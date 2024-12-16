using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// 소켓 통신 서버
/// </summary>
public class NetServer
{
    private bool _run;
    private Listener _listener = new Listener();

    public event System.Action<UserToken> onClientConnected;

    public void Start(int backlog)
    {
        if (_run)
        {
            return;
        }

        _listener.onClientConnected += OnClientConnected;
        _listener.Start(NetDefine.PORT, backlog);

        Debug.Log("서버 시작");
        _run = true;
    }

    public void End()
    {
        if (!_run)
        {
            return;
        }

        _listener.Stop();
        _listener.onClientConnected -= OnClientConnected;

        Debug.Log("서버 종료");
        _run = false;
    }

    private void OnClientConnected(Socket socket)
    {
        UserToken userToken = new UserToken(socket, PacketMessageDispatcher.Instance);
        userToken.onSessionClosed += OnSessionClosed;
        userToken.OnConnected();
        userToken.StartReceive();

        MainThreadDispatcher.Instance.Add(() =>
        {
            onClientConnected?.Invoke(userToken);
        });

        Debug.Log("유저 접속");
    }

    private void OnSessionClosed(UserToken token)
    {
        Debug.Log("OnSessionClosed");
    }
}