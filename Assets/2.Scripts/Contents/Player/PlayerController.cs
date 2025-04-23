using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    private const float FULL_FUEL = 100f;
    private const float FUEL_USE_SPEED = 25f;
    private const float DEAD_HEIGHT = -20f;  //  죽는 높이
    private const float GRAVITY_SCALE_GROUND = 10f;
    private const float GRAVITY_SCALE_AERIAL = 1f;

    [Header("Body")]
    [SerializeField] private float _moveAngleThreshold = 70f;
    [SerializeField] private float _speed = 4f;
    [SerializeField] private float _curFuel = 0f;

    [Header("Artillery")]
    [SerializeField] private Transform _artilleryTrans;
    [SerializeField] private Transform _shellFireTrans;
    [SerializeField] private float _artilleryRotateSpeed = 10f;
    [SerializeField] private float _minAngleArtillery = 0f;
    [SerializeField] private float _maxAngleArtillery = 85f;

    [Header("Shell")]
    [SerializeField] private List<GameObject> _shellList = new List<GameObject>();
    [SerializeField] private float _maxShellPower = 20f;
    [SerializeField] private float _minShellPower = 0.1f;
    [SerializeField] private float _curShellPower = 1f;
    [SerializeField] private float _powerGaugeSpeed = 1f;

    [Header("Prediction Point")]
    [SerializeField] private GameObject _predictionPointPrefab = null;
    [SerializeField] private int _predictionPointNum = 20;                      // 예측 지점 수

    [Header("Sprite")]
    [SerializeField] private SpriteRenderer _srBody = null;
    [SerializeField] private SpriteRenderer _srWheel = null;
    [SerializeField] private SpriteRenderer _srAritllery = null;

    private Rigidbody2D _rb2D = null;               // Tank RigidBody 2D
    private CircleCollider2D _colider2D = null;     // Tank CircleCollider 2D
    private GameObject _curShell = null;            // 현재 선택된 포탄    
    private GameObject[] _predictionPoints;        // 예측 지점 오브젝트 배열

    private float _dirY = 0;
    private float _dirX = 0;
    private bool _isMoveAngle = false;
    private float _curGauge = 0f;
    private float _prevShellPower = 0f;           // 이전에 쏜 파워 값

    private bool _isGround = false;
    private bool _isDead = false;                   // 죽음 확인    
    private bool _isFire = false;
    private bool _bAfterDeadEvent = false;       // 죽음 이후 이벤트 한번만 실행하기 위한 bool

    private bool _isMyTurn = false;
    private bool _isCanFire = false;

    private NetworkObject _curSpawnedShell;
    public NetworkVariable<int> _hp = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private void Awake()
    {
        _rb2D = GetComponent<Rigidbody2D>();
        _colider2D = GetComponent<CircleCollider2D>();
    }

    public override void OnNetworkSpawn()
    {
        Init();

    }

    public void Init()
    {
        // Default
        _curShell = _shellList[0];

        // 예측 점 생성
        _predictionPoints = new GameObject[_predictionPointNum];

        for(int i = 0; i < _predictionPointNum; i++)
        {
            _predictionPoints[i] = Instantiate(_predictionPointPrefab, Vector3.zero, Quaternion.identity);
            _predictionPoints[i].transform.parent = this.transform;
        }

        _isDead = false;
        _bAfterDeadEvent = false;
        _isMyTurn = false;

        HidePredictionsPoints();
    }

    // Update is called once per frame
    private void Update()
    {
        if (CheckDead() == true)
        {
            // 죽음 이후 메소드 한번 실행
            if(_bAfterDeadEvent == false)
            {
                _bAfterDeadEvent = true;
                AfterDeadEvent();
            }

            return;
        }

        // 땅에 있는지 확인 및 중력값 실시간 조정
        _isGround = GroundCheckAndGravityUpdate();

        // 내 턴일 경우에만 움직임
        if (_isMyTurn == false)
            return;

        // 포탄 변경
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (_shellList[0] == null)
            {
                Debug.LogWarning("Shell 1 is Null");
            }
            else
            {
                // 1번 폭탄
                _curShell = _shellList[0];
            }
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (_shellList[1] == null)
            {
                Debug.LogWarning("Shell 2 is Null");
            }
            else
            {
                // 2번 폭탄
                _curShell = _shellList[1];
            }
        }

        // 파워게이지 증가 시키기
        if (Input.GetKey(KeyCode.Space) && _isCanFire)
        {
            if(_isFire == true)
                _isFire = false;

            _curGauge += Time.deltaTime * _powerGaugeSpeed;
            _curShellPower = Mathf.Lerp(_minShellPower, _maxShellPower, _curGauge);
        }

        // 포탄 발사
        if (Input.GetKeyUp(KeyCode.Space) && _isCanFire)
        {
            _isFire = true;
            _curGauge = 0f;
            _isCanFire = false;

            // 포탄 생성
            GenerationShellServerRpc(_curShellPower);

            // 이전 파워 값 저장
            _prevShellPower = _curShellPower;

            // 예측 점 비활성화
            HidePredictionsPoints();

            // 턴 종료
            EndTurn();
        }

        // 포 각도 조절
        _dirY = Input.GetAxis("Vertical");

        // 새로운 회전값 계산 (현재 회전 + 입력값)
        float currentZ = _artilleryTrans.localEulerAngles.z;

        if (currentZ > 180f)
            currentZ -= 360f; // 180° 이상이면 -180° ~ 0°로 변환

        float newZ = Mathf.Clamp(currentZ + _dirY * Time.deltaTime * _artilleryRotateSpeed, _minAngleArtillery, _maxAngleArtillery);
        Quaternion quaternion = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.eulerAngles.y, newZ);
        _artilleryTrans.localRotation = Quaternion.Lerp(_artilleryTrans.localRotation, quaternion, Time.deltaTime * 10);

        // 예측 지점 보여주기
        if (_isFire == false)
        {
            ShowPredictionPoints(0.1f);
        }

        // 땅 위에 있는지 확인
        if (_isGround)
        {
            _dirX = Input.GetAxis("Horizontal");

            // 포 각도 조절중에는 움직일 수 없음
            if (_dirY == 0 && _curFuel > 0f)
               transform.Translate(_dirX * _speed * Time.deltaTime, 0, 0, Space.World);

            if (_dirX != 0f)
            {
                // 연료 사용
                UseFuel();

                // 좌우 반전
                Flip(_dirX);

                // 이동중에 예측지점 숨기기
                HidePredictionsPoints();
            }
        }
        else
        {
            // 공중일땐 수평 
            transform.rotation = Quaternion.Euler(0, 0, 0);
            HidePredictionsPoints();
        }
    }

    //private void LateUpdate()
    //{
    //    // Input 문제로 인해 LateUpdate에서 확인
    //    if(_isMyTurn == true && _isCanFire == false)
    //    {
    //        EndTurn();
    //    }
    //}

    [ServerRpc(RequireOwnership = false)]
    private void GenerationShellServerRpc(float shellPower)
    {
        //_curSpawnedShell = PoolManager.Instance.Pop(_curShell).GetComponent<NetworkObject>();
        //if (!_curSpawnedShell.IsSpawned)
        //{
        //    _curSpawnedShell.Spawn();
        //}

        if (!IsServer)
        {
            return;
        }

        Debug.Log($"서버 포탄 파워: {shellPower}.");

        // 포탄 풀링
        _curSpawnedShell = NetworkObjectPool.Instance.CreateNetObj(_curShell.GetComponent<NetworkObject>());

        // 포탄 위치와 회전
        GameObject shellGameObj = _curSpawnedShell.gameObject;
        shellGameObj.transform.position = _shellFireTrans.position;
        shellGameObj.transform.rotation = Quaternion.Euler(0, 0, _artilleryTrans.eulerAngles.z);

        // 포탄 방향 
        Vector3 newScale = _curSpawnedShell.transform.localScale;
        newScale.x = Mathf.Abs(newScale.x) * Mathf.Sign(transform.localScale.x); // 좌우 반전
        shellGameObj.transform.localScale = newScale;

        // 포탄 초기화
        Shell shell = _curSpawnedShell.GetComponent<Shell>();
        switch (shell.ShellExplosionType)
        {
            case eShellExplosionType.Circle:
                shell.Init();
                break;

            case eShellExplosionType.Ellipse:
                ShellEllipse shellEllipse = shell.GetComponent<ShellEllipse>();
                shellEllipse.Init();
                break;
        }

        // 발사
        shell.Fire(shellPower);

        SendSpawnedObjectClientRpc(_curSpawnedShell.NetworkObjectId, shellPower);
    }

    [ClientRpc]
    void SendSpawnedObjectClientRpc(ulong networkObjectId, float shellPower)
    {
        Debug.Log($"클라이언트 포탄 파워: {shellPower}.");
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var netObj))
        {
            // 포탄 위치 정보 건네주기
            GameInitializer.Instance.CurShellTrans = netObj.transform;

            // 발사한 포탄 저장
            if (_isMyTurn)
            {
                _curShell = netObj.gameObject;
            }
        }
    }

    private bool GroundCheckAndGravityUpdate()
    {
        if (IsGround())
        {
            if (_rb2D.gravityScale != GRAVITY_SCALE_GROUND)
                _rb2D.gravityScale = GRAVITY_SCALE_GROUND;

            return true;
        }
        else
        {
            if (_rb2D.gravityScale != GRAVITY_SCALE_AERIAL)
                _rb2D.gravityScale = GRAVITY_SCALE_AERIAL;

            transform.rotation = Quaternion.Euler(0, 0, 0); // 공중에서 회전 초기화
            HidePredictionsPoints();

            // 떨어질때는 카메라 포커싱 
            if (IngameManager.Instance != null)
                IngameManager.Instance.PlayerCameraFocusing(this);

            return false;
        }
    }

    public void Flip(float dirX)
    {
        Vector3 newScale = transform.localScale;
        newScale.x = Mathf.Abs(newScale.x) * Mathf.Sign(dirX); // 좌우 반전
        transform.localScale = newScale;
    }

    // 연료 사용하는 메소드
    private void UseFuel()
    {
        if (_curFuel > 0f)
        {
            _curFuel -= Time.deltaTime * FUEL_USE_SPEED;
            if (IsOwner)
            {
                IngameUIController.Instance.SetFuel(_curFuel);
            }
        }
        else
        {
            _curFuel = 0f;
        }
    }

    // 연료 채우는 메소드
    public void FillFuel()
    {
        _curFuel = FULL_FUEL; ;
        IngameUIController.Instance.SetFuel(_curFuel);
    }


    public void IsMyTurn()
    {
        _isMyTurn = true;
        _isCanFire = true;
        _isFire = false;
    }

    private void EndTurn()
    {
        _isMyTurn = false;
        // TODO : 임시이므로 나중에 게임매니저에게 알려주기
        IngameManager.Instance.PlayerTurnEnd();
    }

    private bool CheckDead()
    {

        // 일정 높이에서 떨어지면 죽음 처리
        if(transform.position.y <= DEAD_HEIGHT)
        {
            _isDead = true;
        }

        return _isDead;
    }

    private void AfterDeadEvent()
    {
        // 일단 destroy
        Destroy(gameObject);
    }

    //private Shell GenerationShell()
    //{
    //    GameObject go = PoolManager.Instance.Pop(_curShell.gameObject);

    //    if (go == null)
    //    {
    //        go = Instantiate(_curShell, _shellFireTrans.position, Quaternion.identity);
    //        PoolManager.Instance.Push(go);
    //    }
            
    //    go.transform.position = _shellFireTrans.position;
    //    go.transform.rotation = Quaternion.Euler(0, 0, _artilleryTrans.eulerAngles.z);
        
    //    // 포탄 방향 

    //    Vector3 newScale = go.transform.localScale;
    //    newScale.x = Mathf.Abs(newScale.x) * Mathf.Sign(transform.localScale.x); // 좌우 반전
    //    go.transform.localScale = newScale;

    //    Shell shell = go.GetComponent<Shell>();

    //    switch(shell.ShellExplosionType)
    //    {
    //        case eShellExplosionType.Circle:
    //            shell.Init();
    //            return shell;
            
    //        case eShellExplosionType.Ellipse:
    //            ShellEllipse shellEllipse = go.GetComponent<ShellEllipse>();
    //            shellEllipse.Init();
    //            return shellEllipse;
    //    }
    //    return null;
    //}

    private bool IsGround()
    {
        // CircleCollider2D 중심 가져오기
        Vector2 center = (Vector2)_colider2D.bounds.center;

        // 항상 콜라이더의 기울기에 따라 ray 시작 위치를 계산
        Vector2 ray1 = center + (Vector2)(-transform.right * _colider2D.radius); // 왼쪽 끝
        Vector2 ray2 = center + (Vector2)(transform.right * _colider2D.radius);  // 오른쪽 끝

        float distance = 0.9f;// 0.75f;

        // 시각화 
        Debug.DrawRay(ray1, -transform.up * distance, Color.red);
        Debug.DrawRay(ray2, -transform.up * distance, Color.blue);

        // 방향
        Vector2 direction = (Vector2)transform.up * -1f;

        // Raycast
        RaycastHit2D leftHit = Physics2D.Raycast(ray1, direction, distance, LayerMask.GetMask("Ground"));
        RaycastHit2D rightHit = Physics2D.Raycast(ray2, direction, distance, LayerMask.GetMask("Ground"));

        //Linecast 
        RaycastHit2D area = Physics2D.BoxCast(transform.position, new Vector2(_colider2D.radius * 2, _colider2D.radius * 2), 0f,direction, distance,LayerMask.GetMask("Ground"));

        Debug.DrawLine(ray1 + direction * distance, ray2 + direction * distance, Color.magenta);

        return leftHit.collider != null || rightHit.collider || area.collider != null; //hits.Length > 0f;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Ground")) 
            return; 

        Vector2 averageNormal = Vector2.zero;
        int contactCount = collision.contactCount;

        // 모든 충돌 지점의 법선 벡터 합산
        for (int i = 0; i < contactCount; i++)
        {
            averageNormal += collision.contacts[i].normal;
        }

        if (contactCount > 0)
        {
            averageNormal /= contactCount; // 평균값 계산
            averageNormal.Normalize(); // 단위 벡터화

            // 목표 회전 각도 계산
            float targetAngle = Mathf.Atan2(averageNormal.y, averageNormal.x) * Mathf.Rad2Deg - 90f;

            _isMoveAngle = CanMoveAngle(targetAngle);
 
            if (_isMoveAngle)
            {          
                transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, targetAngle);
            }
        }
    }

    // 움직일 수 있는 각도인지 확인하는 함수
    private bool CanMoveAngle(float targetAngle)
    {
        // 목표 각도도 같은 방식으로 변환
        if (targetAngle > 180f)
        {
            targetAngle -= 360f;
        }

        // 허용된 기울기 내에 있는지 확인
        return Mathf.Abs(targetAngle) <= _moveAngleThreshold;
    }

    private void ShowPredictionPoints(float time)
    {
        // 내턴이 아니라면 안보여줌
        if (_isMyTurn == false)
            return;

        for(int i = 0; i < _predictionPointNum; i++)
        {
            _predictionPoints[i].transform.position = PredictionPointsPos(i * time);
            _predictionPoints[i].SetActive(true);
        }
    }

    private void HidePredictionsPoints()
    {
        for (int i = 0; i < _predictionPointNum; i++)
        {
            _predictionPoints[i].SetActive(false);
        }
    }

    // 예측 지점 계산
    private Vector2 PredictionPointsPos(float time)
    {
        Vector2 pos = Vector2.zero;

        float fireAngle = _artilleryTrans.eulerAngles.z * Mathf.Deg2Rad;

        if (transform.localScale.x < 0)
        {
            fireAngle = Mathf.PI - fireAngle;
        }

        // Rigidbody2D의 질량 가져오기 (필수)
        float mass = _curShell.GetComponent<Rigidbody2D>().mass;

        // AddForce에서 적용한 힘
        Vector2 force = new Vector2(Mathf.Cos(fireAngle), Mathf.Sin(fireAngle) * Mathf.Sign(transform.localScale.x)) * _curShellPower;

        // 초기 속도 (F = m * a -> v = F / m)
        Vector2 initialVelocity = force / mass;

        pos = (Vector2)_shellFireTrans.position + initialVelocity * time + Physics2D.gravity * (time * time) * 0.5f;
        
        return pos;
    }

    public void ChangeSpriteOrder(int value)
    {
        _srBody.sortingOrder += value;
        _srWheel.sortingOrder += value; 
        _srAritllery.sortingOrder += value;
    }



    public void ApplyLocalDamage(int dmg)
    {
        _hp.Value -= dmg;

        if (IsOwner)
        {
            IngameUIController.Instance.SetHP(_hp.Value);
        }

        if (_hp.Value <= 0)
        {
            Debug.Log("사망");
            gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    public void BroadcastHPToOthersClientRpc(int hp, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner)
        {
            //여기서 상대 HP를 가지고와서 상대 체력 UI 조정 예정
        }
    }

    public int GetCurrentHP()
    {
        return _hp.Value;
    }
}
