using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// ���� ��� ����
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

        Debug.Log("���� ����");
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

        Debug.Log("���� ����");
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

        Debug.Log("���� ����");
    }

    private void OnSessionClosed(UserToken token)
    {
        Debug.Log("OnSessionClosed");
    }
}