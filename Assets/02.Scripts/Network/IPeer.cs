public interface IPeer
{
    void ProcessMessage(short protocolID, byte[] buffer);
    void Remove();
    void Send(Packet packet);
}