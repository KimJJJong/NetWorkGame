using System.Collections.Concurrent;
using UnityEngine;

/// <summary>
/// 수신된 메시지를 유니티의 메인스레드에서 처리하게 한다.
/// </summary>
public class PacketMessageDispatcher : MonoBehaviour, IMessageDispatcher
{
    // 싱글턴(Singleton) 패턴 : 인스턴스를 하나만 생성, 전역접근 가능
    private static PacketMessageDispatcher _instance;
    public static PacketMessageDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject container = new GameObject("PacketMessageDispatcher");
                container.name = "PacketMessageDispatcher";
                _instance = container.AddComponent<PacketMessageDispatcher>();
            }

            return _instance;
        }
    }

    public struct PacketMessage
    {
        public UserToken token; // 보낸유저토큰
        public byte[] buffer;   // 패킷

        public PacketMessage(UserToken token, byte[] buffer)
        {
            this.token = token;
            this.buffer = buffer;
        }
    }

    // 스레드에 안전한 Queue
    private ConcurrentQueue<PacketMessage> _messageQueue;

    public void Init()
    {
       _messageQueue = new ConcurrentQueue<PacketMessage>();
    }

    // 유니티의 Update에서 Queue에 들어있는 패킷을 계속 처리하게 한다.
    private void Update()
    {
        // TryDequeue 안전하게 처음 요소를 가져온다.
        while (_messageQueue.TryDequeue(out PacketMessage msg))
        {
            if (!msg.token.IsConnected)
                continue;

            msg.token.OnMessage(msg.buffer);
        }
    }

    // 받은 패킷을 추가한다.
    public void OnMessage(UserToken token, byte[] buffer)
    {
        _messageQueue.Enqueue(new PacketMessage(token, buffer));
    }
}