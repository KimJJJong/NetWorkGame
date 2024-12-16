using System;

public class MessageResolver
{
    // 받은데이터를 저장, 패킷으로 자른다.
    private byte[] _messageBuffer;
    // 처리 해야될 데이터까지의 위치
    private int _curPosition;

    public MessageResolver(int bufferSize)
    {
        _messageBuffer = new byte[bufferSize];
    }

    /// <summary>
    /// 받은 바이트 배열을 처리
    /// </summary>
    /// <param name="bufffer">받은 바이트 배열</param>
    /// <param name="offset">받은 바이트 배열 위치</param>
    /// <param name="transferred">받은 길이</param>
    /// <param name="onComplete">하나의 패킷이 만들어 졌을시 호출</param>
    public void OnReceive(byte[] bufffer, int offset, int transferred, Action<byte[]> onComplete)
    {
        // 패킷구조
        // [body길이:2byte][protocolID:2byte][body:보낼만큼]

        // 받은 바이트배열을 메시지버퍼 현재위치 뒤쪽에 복사 한다.
        // (복사할바이트 배열, 복사할바이트 배열 위치, 대상바이트 배열, 대상바이트위치, 길이)
        Array.Copy(bufffer, offset, _messageBuffer, _curPosition, transferred);
        // 받은만큼 위치를 옮긴다.
        _curPosition += transferred;

        while (_curPosition > 0)
        {
            // 현재 받은 크기가 헤더보다 작다면 더받아야된다.
            if (_curPosition < NetDefine.HEADER_SIZE)
            {
                return;
            }

            // 헤더 정보를 가져온다.
            // BitConverter : 바이트 배열을 읽어서 특정 자료형으로 변환.
            short size = BitConverter.ToInt16(_messageBuffer, 0);
            short protocolID = BitConverter.ToInt16(_messageBuffer, 2);

            // 패킷이 완성되지 않았다면 더받아야된다.
            if (_curPosition < size)
            {
                return;
            }

            // 하나의 패킷이 완성됨, 배열에 복사해서 넘김
            byte[] clone = new byte[size];
            Array.Copy(_messageBuffer, clone, size);

            // 처리함수 호출
            onComplete?.Invoke(clone);

            // 하나의 패킷을 처리했으므로 _curPosition 위치를 옮김, _messageBuffer를 처리한만큼의 앞쪽으로 옮김
            _curPosition -= size;
            Array.Copy(_messageBuffer, size, _messageBuffer, 0, _curPosition);
        }
    }
}