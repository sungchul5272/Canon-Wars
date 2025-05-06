using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Coroutine _corCamShake;
    Vector3 _originPos;

    public void Shake(float duration, float magnitude)
    {
        if (_corCamShake != null)
            StopCoroutine(_corCamShake);

        _originPos = transform.localPosition;
        _corCamShake = StartCoroutine(CorShake(duration,magnitude));
    }

    private IEnumerator CorShake(float duration, float magnitude)
    {
        // 흔들림 끝나는 시간
        float endTime = Time.time + duration;

        while(Time.time <= endTime)
        {
            Vector2 offset = Random.insideUnitCircle * magnitude;
            Vector3 shakePos = new(_originPos.x + offset.x, _originPos.y + offset.y, _originPos.z);

            transform.localPosition = shakePos;

            yield return null;
        }

        transform.localPosition = _originPos;
    }

    public void StopShake()
    {
        if (_corCamShake != null)
            StopCoroutine(_corCamShake);

        transform.localPosition = _originPos;
    }
}
