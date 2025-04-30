using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ShellEllipse : Shell
{
    [Header("Radius Y = Radius / RadiusY Ratio")]
    [SerializeField] private float _radiusYRatio = 2f;

    public override void CheckExplosion()
    {
        Debug.DrawRay(transform.position, Vector2.down * 0.5f, Color.magenta);

        // BoxCollider2D의 중심과 크기를 가져오기
        Vector2 colliderCenter = (Vector2)transform.position + _collider2D.offset;
        Vector2 colliderSize = _collider2D.size + (Vector2.one * 0.05f);

        // BoxCast를 사용하여 포탄이 플레이어 또는 땅의 충돌 감지
        RaycastHit2D hit = Physics2D.BoxCast(colliderCenter, colliderSize, transform.eulerAngles.z, Vector2.down, 0f, LayerMask.GetMask("Player", "Ground"));

        // 충돌 확인 시 
        if (hit.collider != null)
        {
            CheckExplosionClientRpc(colliderCenter);
        }
    }

    [ClientRpc]
    public override void CheckExplosionClientRpc(Vector2 colliderCenter)
    {
        // 파티클 재생
        CreateExplosionParticle();

        // 카메라 흔들림
        CamShake();

        // 현재 원 모양 상태로 폭발범위 닿는곳을 가져옴
        float radiusY = _radius / _radiusYRatio;
        Collider2D[] hitAllPlayerList = Physics2D.OverlapCircleAll(colliderCenter, Mathf.Max(_radius, radiusY), LayerMask.GetMask("Player"));

        List<Collider2D> filteredPlayerList = new List<Collider2D>(hitAllPlayerList);

        // 타원 밖에 있는 객체 제거
        filteredPlayerList.RemoveAll(hitObject =>
        {
            Vector2 pos = hitObject.transform.position;
            float dx = (pos.x - colliderCenter.x) / _radius;
            float dy = (pos.y - colliderCenter.y) / radiusY;

            return (dx * dx) + (dy * dy) > 1;  // 타원 방정식 검사에 실패한 경우 제거
        });

        Collider2D[] hitGroundList = Physics2D.OverlapCircleAll(colliderCenter, _radius, LayerMask.GetMask("Ground"));

        if (filteredPlayerList.Count > 0)
        {
            // 데미지 주기
        }

        if (hitGroundList.Length > 0)
        {
            foreach (var hitGround in hitGroundList)
            {
                Ground ground = hitGround.GetComponent<Ground>();
                ground.GroundExplosion(colliderCenter, _radius, radiusY, eShellExplosionType.Ellipse);
            }
        }

        ReleaseShell();
    }

    private void OnEnable()
    {
        // 풀에서 꺼낸 직후에 TrailRenderer 초기화
        var trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear();
        }
    }
}

