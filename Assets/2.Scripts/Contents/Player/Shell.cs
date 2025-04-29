using Unity.Netcode;
using UnityEngine;

public enum eShellExplosionType
{
    Circle,
    Ellipse,
}
public class Shell : NetworkBehaviour
{
    [Header("Camera Shake")]
    [SerializeField] private float _shakeDurtaion = 0.5f;
    [SerializeField] private float _shakeMagnitude = 1f;

    [Header("Shell Info")]
    [SerializeField] private eShellExplosionType _explosinType;
    public eShellExplosionType ShellExplosionType { get => _explosinType; }

    [SerializeField] private GameObject _explosionParticlePrefab = null;
    [SerializeField] private float _durtaion = 3.5f;        // 객체의 지속시간
    [SerializeField] protected float _radius = 2.5f;
    
    //[SerializeField] private Sprite _debugConflictPoint;

    private Rigidbody2D _rb2D = null;
    protected BoxCollider2D _collider2D = null;
    private NetworkObject _networkObject = null;

    private NetworkVariable<bool> _isShellMapOut = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    //private int _damage = 20;
    private float _endTime = 0f;
    private float _power = 1f;
    private float _curWindForce = 0f;
    private bool _isFire = false;

    private void Awake()
    {
        _collider2D = GetComponent<BoxCollider2D>();
        _rb2D = GetComponent<Rigidbody2D>();
        _networkObject = GetComponent<NetworkObject>();

        // 맵 밖으로 나간 경우 클라이언트쪽에 할당 해제 시키기
        _isShellMapOut.OnValueChanged += (prevValue, changeValue) =>
        {
            if (changeValue == true && IngameManager.Instance.CurShellTrans != null)
            {
                IngameManager.Instance.CurShellTrans = null;
            }
        };
    }

    public void Init()
    {
        //_collider2D = GetComponent<BoxCollider2D>();
        //_rb2D = GetComponent<Rigidbody2D>();
        _isFire = false;
        _endTime = Time.time + _durtaion;
    }

    public void FixedUpdate()
    {
        if (_isFire)
        {
            if (IngameManager.Instance != null)
                _curWindForce = IngameManager.Instance.GetWindForce();

            _rb2D.AddForce(new Vector2(_curWindForce, 0f)); // 지속적으로 바람 영향
        }        
       
        float angle = Mathf.Atan2(_rb2D.linearVelocity.y, _rb2D.linearVelocity.x) * Mathf.Rad2Deg;
        
        // 탱크가 좌측을 바라볼 때 180도 추가
        if (transform.localScale.x < 0)
        {
            angle += 180f;
        }

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, angle);
    }

    public void Update()
    {
        if (!IsServer)
        {
            return;
        }

        CheckExplosion();

        // 지속시간이 끝나면 없애기
        if (Time.time >= _endTime)
        {
            // TODO : 풀링
            NetworkObjectPool.Instance.RemoveNetObj(_networkObject);
            //PoolManager.Instance.Push(gameObject);
        }
    }
    protected void LateUpdate()
    {
        // 포탄이 맵 밖으로 나갔는지 확인
        CheckShellMapOut();
    }

    private void CheckShellMapOut()
    {
        if (GameInitializer.Instance == null || GameInitializer.Instance.GetMapSize() == Vector2.zero)
            return;

        Vector2 mapSize = GameInitializer.Instance.GetMapSize();

        if (IngameManager.Instance.CurShellTrans == null)
            return;

        // 포탄이 양 옆, 하단을 넘어갔는지 확인하기 (위는 제외)
        if(transform.position.x < -mapSize.x / 2f || transform.position.x > mapSize.x / 2f || transform.position.y < -mapSize.y / 2f)
        {
            ReleaseShell();
        }    
    }

    public void Fire(float power)
    {
        _power = power;
        _isFire = true;

        // 발사
        Vector2 fireDir = transform.right * Mathf.Sign(transform.localScale.x);
        _rb2D.AddForce(fireDir * _power, ForceMode2D.Impulse);

        // 값 초기화
        if (IsServer)
            _isShellMapOut.Value = false;
    }

    public virtual void CheckExplosion()
    {
        Debug.DrawRay(transform.position, Vector2.down * 0.2f, Color.magenta);

        // BoxCollider2D의 중심과 크기를 가져오기
        Vector2 colliderCenter = (Vector2)transform.position + _collider2D.offset;
        Vector2 colliderSize = _collider2D.size;

        // BoxCast를 사용하여 플레이어 또는 땅의 충돌 감지
        RaycastHit2D hit = Physics2D.BoxCast(colliderCenter, colliderSize, transform.eulerAngles.z, Vector2.down, 0f, LayerMask.GetMask("Player", "Ground"));

        // 충돌 확인 시
        if (hit.collider != null)
        {
            CheckExplosionClientRpc(colliderCenter);
        }
    }
 
    [ClientRpc]
    private void CheckExplosionClientRpc(Vector2 colliderCenter)
    {
        // 파티클 생성
        CreateExplosionParticle();

        // 카메라 흔들림
        CamShake();

        // CircleCast를 사용하여 폭발 범위에 있는 객체 List 가져오기
        Collider2D[] hitPlayerList = Physics2D.OverlapCircleAll(colliderCenter, _radius, LayerMask.GetMask("Player"));
        Collider2D[] hitGroundList = Physics2D.OverlapCircleAll(colliderCenter, _radius, LayerMask.GetMask("Ground"));
        if (hitPlayerList.Length > 0)
        {
            // 데미지 주기
            foreach (var col in hitPlayerList)
            {
                var player = col.GetComponent<PlayerController>();

                // 맞은 객체가 나인 경우에만 데미지 처리
                if (player != null && player.IsOwner)
                {
                    player.ApplyLocalDamage(20);

                }
            }

        }     

        if (hitGroundList.Length > 0)
        {
            foreach (var hitGround in hitGroundList)
            {
                Ground ground = hitGround.GetComponent<Ground>();
                ground.GroundExplosion(colliderCenter, _radius);

                // 디버그용 원 스프라이트 생성
                //DebugExplosionCircle(colliderCenter, _radius);
            }
        }

        ReleaseShell();
    }



    protected void ReleaseShell()
    {
        // 카메라가 더이상 포탄을 안따라가도록
        IngameManager.Instance.CurShellTrans = null;

        // 클라이언트쪽에서도 해제시키도록
        if(IsServer)
            _isShellMapOut.Value = true;

        // 충돌한 경우에만 Pool
        NetworkObjectPool.Instance.RemoveNetObj(_networkObject);
        //if (IsServer)
        //{
        //    // TODO : 풀링
        //    //PoolManager.Instance.Push(gameObject);
        //}
    }

    protected void CreateExplosionParticle()
    {
        // 폭발 파티클 재생
        if (_explosionParticlePrefab != null)
        {
            GameObject go = PoolManager.Instance.Pop(_explosionParticlePrefab);

            if (go == null)
            {
                go = Instantiate(_explosionParticlePrefab);
                PoolManager.Instance.Push(go);
            }
            go.transform.position = transform.position;

            ParticleSystem particle = go.GetComponent<ParticleSystem>();
            particle.Play();
        }
    }

    protected void CamShake()
    {
        CameraShake camShake = Camera.main.GetComponent<CameraShake>();
       
        if (camShake == null)
            return;

        camShake.Shake(_shakeDurtaion, _shakeMagnitude);
    }
    //private void DebugExplosionCircle(Vector2 position, float radius)
    //{
    //    GameObject debugCircle = new GameObject("ExplosionDebugCircle");
    //    debugCircle.transform.position = position;
    //    debugCircle.transform.localScale = Vector3.one * 0.3f;

    //    SpriteRenderer sr = debugCircle.AddComponent<SpriteRenderer>();
    //    sr.sprite = _debugConflictPoint; // "DebugCircle"은 미리 만들어 둔 원형 스프라이트
    //    sr.color = new Color(1, 0, 0, 1); // 빨간색
    //    sr.sortingOrder = 3;
    //}
}
