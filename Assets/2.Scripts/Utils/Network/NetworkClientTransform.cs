using Unity.Netcode.Components;

public class NetworkClientTransform : NetworkTransform
{
    // 클라이언트 측 변경 사항과 변환을 동기화 (호스트 포함)
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
