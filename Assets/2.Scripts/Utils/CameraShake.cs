using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class CameraShake : MonoBehaviour
{
    private CameraController _camController = null;
    private Coroutine _corCamShake;
    Vector3 _originPos;

    public void Shake(float duration, float magnitude)
    {
        if (_corCamShake != null)
            StopCoroutine(_corCamShake);

        if(_camController == null )
            _camController = GetComponent<CameraController>();

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

            Vector3 newPos = SetClampedPosition(shakePos);
            transform.localPosition = newPos;
            yield return null;
        }

        // 가끔 맵 밖으로 나가는거 방지
        transform.localPosition = SetClampedPosition(_originPos);
    }

    private Vector3 SetClampedPosition(Vector3 tartgetPos)
    {
        (var bottomLeft, var topRight) = _camController.GetMapBoundPos();

        // 맵 경계 범위로 제한
        float clampedX = Mathf.Clamp(tartgetPos.x, bottomLeft.x, topRight.x);
        float clampedY = Mathf.Clamp(tartgetPos.y, bottomLeft.y, topRight.y);

        return new Vector3(clampedX,clampedY, _originPos.z);
    }

    public void StopShake()
    {
        if (_corCamShake != null)
            StopCoroutine(_corCamShake);

        transform.localPosition = _originPos;
    }
}
