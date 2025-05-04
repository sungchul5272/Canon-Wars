using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("UI")]
    [SerializeField] GameObject _turnMark;
    [SerializeField] Text _playerNickName;
    [SerializeField] Slider _playerHpBar;

    private Rigidbody2D _rb2D = null;               // Tank RigidBody 2D
    private CircleCollider2D _colider2D = null;     // Tank CircleCollider 2D
   // private GameObject _curShell = null;            // 현재 선택된 포탄    
    private GameObject[] _predictionPoints;        // 예측 지점 오브젝트 배열

    private float _dirY = 0;
    private float _dirX = 0;
    private float _curGauge = 0f;
    private float _prevShellPower = 0f;           // 이전에 쏜 파워 값

    private int _curShellIndex = 0; 

    private bool _isMoveAngle = false;
    private bool _isGround = false;
    private bool _isDead = false;                   // 죽음 확인    
    private bool _isFire = false;
    private bool _bAfterDeadEvent = false;       // 죽음 이후 이벤트 한번만 실행하기 위한 bool
    private bool _isCanFire = false;
    private bool _isMoving = false;

    private Coroutine _corGenerateShell = null;

    private NetworkObject _curSpawnedShell;
    public NetworkVariable<int> _hp = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString32Bytes> _NickName =
        new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<eTankType> _tankType = new(eTankType.Max, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        _rb2D = GetComponent<Rigidbody2D>();
        _colider2D = GetComponent<CircleCollider2D>();

        CameraController _camController = Camera.main.GetComponent<CameraController>();
        _camController.Init();
        Debug.Log($"Camera Size : {Camera.main.orthographicSize}");
        Debug.Log("카메라 초기화 완료");
    }

    public override void OnNetworkSpawn()
    {
        _tankType.OnValueChanged += (oldVal, newVal) =>
        {
            Debug.Log($"[PlayerController] tankType {oldVal} -> {newVal}");
            if(newVal != eTankType.Random && newVal != eTankType.Max)
            {
                SetTankTypeAndShellList(newVal);
            }
        };

        // 서버의 탱크가 클라이언트 입장에서 초기화 안되는 현상 보완
        if (IsClient && !IsOwner) 
        {
            if (_tankType.Value != eTankType.Random && _tankType.Value != eTankType.Max)
            {
                SetTankTypeAndShellList(_tankType.Value);
                Debug.Log($"[Client Init] 서버 탱크 초기화: {_tankType.Value}");
            }
        }

        Init();

        _hp.OnValueChanged += (oldVal, newVal) =>
        {
            _playerHpBar.value = Mathf.Clamp(newVal, 0, 100);
        };

        if (IsOwner)
        {
            _NickName.Value = FirebaseManager._instance.userVO.NickName;
            Debug.Log($"[OnNetworkSpawn] 내 닉네임 설정: {_NickName.Value}");
        }

        _NickName.OnValueChanged += (oldVal, newVal) =>
        {
            Debug.Log($"[OnValueChanged] 닉네임 변경 감지: {oldVal} -> {newVal}");
            _playerNickName.text = newVal.ToString();
        };

        // 처음 값도 바로 UI에 반영
        _playerNickName.text = _NickName.Value.ToString();
    }

    public void Init()
    {
        // 예측 점 생성
        _predictionPoints = new GameObject[_predictionPointNum];

        for (int i = 0; i < _predictionPointNum; i++)
        {
            _predictionPoints[i] = Instantiate(_predictionPointPrefab, Vector3.zero, Quaternion.identity);
            _predictionPoints[i].transform.parent = this.transform;
        }

        _isDead = false;
        _bAfterDeadEvent = false;

        HidePredictionsPoints();
        SetTurnMarkVisible(false);

        if (transform.position.x > 0)
        {
            Flip(-1);
        }
    }
  
    // Update is called once per frame
    private void Update()
    {
        if (CheckDead() == true)
        {
            // 죽음 이후 메소드 한번 실행
            if (_bAfterDeadEvent == false)
            {
                _bAfterDeadEvent = true;
                AfterDeadEvent();
            }

            return;
        }

        // 땅에 있는지 확인 및 중력값 실시간 조정
        _isGround = GroundCheckAndGravityUpdate();

        // 포탄 변경 ( 내 턴이 아니여도 포탄 선택 가능)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (_shellList[0] == null)
            {
                Debug.LogWarning("Shell 1 is Null");
            }
            else
            {
                // 1번 폭탄
                _curShellIndex = 0;
                Debug.Log($"Selected Shell = {_shellList[_curShellIndex].name}");
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
                _curShellIndex = 1;

                Debug.Log($"Selected Shell = {_shellList[_curShellIndex].name}");
            }
        }

        // 내 턴이고 포탄을 쏠 수 있을 때에만 움직임
        if (!IngameManager.Instance.IsMyTurn() || !_isCanFire)
        {
            return;
        }

        // 파워게이지 증가 시키기
        if (Input.GetKey(KeyCode.Space) && _isCanFire)
        {
            if (_isFire == true)
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

            // 포탄 선택 확정
            IngameManager.Instance.SetSelectedShellIndex(_curShellIndex);

            // 포탄 생성
            if (_corGenerateShell != null)
                StopCoroutine(_corGenerateShell);

            _corGenerateShell = StartCoroutine(CorGenerateShell());

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

        if (_isGround)
        {
            _dirX = Input.GetAxis("Horizontal");

            if (_dirX != 0f)
            {
                Flip(_dirX); // 항상 가능해야 함

                if (_dirY == 0 && _curFuel > 0f)
                {
                    transform.Translate(_dirX * _speed * Time.deltaTime, 0, 0, Space.World);
                    UseFuel();
                    HidePredictionsPoints();

                    if (!_isMoving)
                    {
                        _isMoving = true;
                        NotifyTankMoveServerRpc(true);
                    }
                }
                else
                {
                    if (_isMoving)
                    {
                        _isMoving = false;
                        NotifyTankMoveServerRpc(false);
                    }
                }
            }
            else
            {
                if (_isMoving)
                {
                    _isMoving = false;
                    NotifyTankMoveServerRpc(false);
                }
            }
        }
        else
        {
            // 공중일땐 수평 
            transform.rotation = Quaternion.Euler(0, 0, 0);
            HidePredictionsPoints();
        }
    }

    private IEnumerator CorGenerateShell()
    {
        // 정보용
        Shell shell = _shellList[_curShellIndex].GetComponent<Shell>();

        int createCount = shell.GetCreateCount();
        float createInterval = shell.GetCreateInterval();

        for(int i =0; i < createCount; i++)
        {
            GenerationShellServerRpc(_curShellPower);

            yield return new WaitForSeconds(createInterval);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void GenerationShellServerRpc(float shellPower)
    {
        if (!IsServer)
        {
            return;
        }
        // 인덱스 가져오기
        int selectedShellIndex = IngameManager.Instance.GetSelectShellIndex();
        // 포탄 풀링
        _curSpawnedShell = NetworkObjectPool.Instance.CreateNetObj(_shellList[selectedShellIndex].GetComponent<NetworkObject>());
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
                ShellEllipse ellipseShell = shell.GetComponent<ShellEllipse>();
                ellipseShell.Init();

                shell = ellipseShell;
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
            IngameManager.Instance.CurShellTrans = netObj.transform;

            // 내 턴이면 발사한 포탄 저장
            //if (IngameManager.Instance.IsMyTurn())
            //{
            //    _curShell = netObj.gameObject;
            //}
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

    public void SetMyTurn()
    {
        _isCanFire = true;
        _isFire = false;
    }

    private void EndTurn()
    {
        IngameManager.Instance.PlayerTurnEndServerRpc();
    }

    private bool CheckDead()
    {

        // 일정 높이에서 떨어지면 죽음 처리
        if (transform.position.y <= DEAD_HEIGHT)
        {
            _isDead = true;
        }
        else if (_hp.Value <= 0 && !_isDead)
        {
            _isDead = true;
        }

        return _isDead;
    }

    private void AfterDeadEvent()
    {
        IngameManager.Instance.NotifyDeathServerRpc();
    }

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
        RaycastHit2D area = Physics2D.BoxCast(transform.position, new Vector2(_colider2D.radius * 2, _colider2D.radius * 2), 0f, direction, distance, LayerMask.GetMask("Ground"));

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
        // 내 탱크와 내 턴이 아니라면 안 보여줌
        if (!IngameManager.Instance.IsMyTurn() || !IsOwner)
        {
            return;
        }

        for (int i = 0; i < _predictionPointNum; i++)
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
        //float mass = _curShell.GetComponent<Rigidbody2D>().mass;
        float mass = _shellList[_curShellIndex].GetComponent<Rigidbody2D>().mass;
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
            BroadcastHPToOthersClientRpc(_hp.Value);
        }
    }

    [ClientRpc]
    public void BroadcastHPToOthersClientRpc(int hp, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner)
        {
            _playerHpBar.value = Mathf.Clamp(hp, 0, 100);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReportNicknameToServerRpc(string nick)
    {
        _NickName.Value = nick;
    }

    public int GetCurrentHP()
    {
        return _hp.Value;
    }

    public void SetTurnMarkVisible(bool visible)
    {
        if (_turnMark != null)
            _turnMark.SetActive(visible);
    }

    public void SetTankTypeAndShellList(eTankType type)
    {
        // 포탄 리스트 불러오기
        _shellList.Clear();
        _shellList = SODataManager.instance.GetTankData(type)._shellList;

        // Default
        _curShellIndex = 0;
    }

    [ServerRpc(RequireOwnership = false)]
    void NotifyTankMoveServerRpc(bool isMoving)
    {
        NotifyTankMoveClientRpc(isMoving);
    }

    [ClientRpc]
    void NotifyTankMoveClientRpc(bool isMoving)
    {
        if (isMoving)
            SoundManager.Instance.StartTankMoveLoop();
        else
            SoundManager.Instance.StopTankMoveLoop();
    }
}
