using UnityEngine;

public class PlayerUIController : MonoBehaviour
{
    [SerializeField] Transform _target;
    [SerializeField] Vector3 _offset = new Vector3(0, 2f, 0);

    void LateUpdate()
    {
        if (_target == null) return;

        // 위치 고정 (탱크의 회전 무시하고 위로만 offset)
        transform.position = _target.position + Vector3.up * _offset.y;

        // 회전 무시
        transform.rotation = Quaternion.identity;

        // 부모가 뒤집혔을 경우를 감지해서 보정
        Vector3 correctedScale = transform.localScale;
        correctedScale.x = Mathf.Abs(correctedScale.x) * Mathf.Sign(_target.lossyScale.x);
        transform.localScale = correctedScale;
    }
}
