/// <summary>
/// 패킷 메시지 처리를 위한 인터페이스
/// </summary>
public interface IMessageDispatcher
{
    void OnMessage(UserToken user, byte[] buffer);
}