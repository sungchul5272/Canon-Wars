using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private const float EDGE_THRESHOLD = 20f;
    [SerializeField] private float _camMoveSpeed = 5f;

    [Range(10f, 15f)]
    [SerializeField] private float _camMaxSize = 0f;
    [SerializeField] private float _camZoomOutTime = 2f;

    private Camera _camera;
    private Transform _trans;
    private Transform _curShellTrans;

    private Vector2 _mapSize = Vector2.zero;
    private Vector2 _mapBottomLeft = Vector2.zero;
    private Vector2 _mapTopRight = Vector2.zero;

    private float _camSizeOriginWidth = 0f;
    private float _camSizeOriginHeight = 0f;

    private float _camSizeWidth;
    private float _camSizeHegiht;
    private float _newPosX = 0f;
    private float _newPosY = 0f;

    private float _zoomTime = 0f;
    private bool _isInit = false;

    void Awake()
    {
        _camera = GetComponent<Camera>();
        _trans = transform;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void Init()//Action pCallback)
    {
        // 원래 카메라 크기 저장
        _camSizeOriginHeight = _camera.orthographicSize;
        _camSizeOriginWidth = _camSizeOriginHeight * _camera.aspect;

        // 맵 정보 불러오기
        MapSizeCheck();

        _isInit = true;

        //if (pCallback != null)
        //    pCallback.Invoke();
    }

    public void PlayerFocusing(PlayerController curPlayer)
    {
        if (curPlayer == null)
            return;

        RestoreCamSize();

        // 플레이어 포커싱
        _newPosX = Mathf.Clamp(curPlayer.transform.position.x, _mapBottomLeft.x, _mapTopRight.x);
        _newPosY = Mathf.Clamp(curPlayer.transform.position.y, _mapBottomLeft.y, _mapTopRight.y);

        Vector3 newPos = new Vector3(_newPosX, _newPosY, _trans.position.z);

        _trans.position = newPos;
    }

    // 원래 카메라 사이즈로 복구
    private void RestoreCamSize()
    {
        _zoomTime = 0f;
        Camera.main.orthographicSize = _camSizeOriginHeight;
        MapSizeCheck();
    }

    // Update is called once per frame
    void Update()
    {
        if (_isInit == true && GameInitializer.Instance != null)
        {
            if (IngameManager.Instance.CurShellTrans != null)
            {
                CameraZoomOut();
                // 포탄 포커싱
                _curShellTrans = IngameManager.Instance.CurShellTrans;

                // 카메라가 맵 범위 밖으로 안나가도록 제한
                _newPosX = Mathf.Clamp(_curShellTrans.position.x, _mapBottomLeft.x, _mapTopRight.x);
                _newPosY = Mathf.Clamp(_curShellTrans.position.y, _mapBottomLeft.y, _mapTopRight.y);

                Vector3 newPos = new Vector3(_newPosX, _newPosY, _trans.position.z);
                _trans.position = newPos;
            }
            else
            {
                // 포탄을 쏘기 전까지는 화면 움직임 가능
                MoveCameraAroundMap();
            }
        }
    }

    // 점진적으로 카메라 줌아웃 하는 메소드
    private void CameraZoomOut()
    {
        float mapMaxCamSize = _mapSize.x / _camera.aspect;
        float camMaxSize = Mathf.Min(_camMaxSize, mapMaxCamSize);

        if (_zoomTime < _camZoomOutTime)
        {
            _zoomTime += Time.deltaTime;
           
            float progress = _zoomTime / _camZoomOutTime;

            _camera.orthographicSize = Mathf.Lerp(_camSizeOriginHeight, camMaxSize, progress * progress);
        }
        else
        {
            _camera.orthographicSize = camMaxSize;
        }

        MapSizeCheck();
    }

    private void MoveCameraAroundMap()
    {
        // 화면 내에서만 인식하도록
        if (Input.mousePosition.x < -1 || Input.mousePosition.x > Screen.width+1 ||
            Input.mousePosition.y < -1 || Input.mousePosition.y > Screen.height+1)
        {
            return;
        }

        // 마우스 위치 알아오기
        Vector3 mousePos = Input.mousePosition;

        // 카메라와 마우스간의 거리 확인
        float topDis = Camera.main.pixelHeight - mousePos.y;
        float bottomDis = mousePos.y;
        float rightDis = Camera.main.pixelWidth - mousePos.x;
        float leftDis = mousePos.x;

        Vector3 moveDir = Vector3.zero;

        // 한계치보다 작은경우에만 이동 (화면 끝에 거의 다다를때쯤)
        if (topDis <= EDGE_THRESHOLD)
            moveDir += Vector3.up;
        if (bottomDis <= EDGE_THRESHOLD)
            moveDir += Vector3.down;
        if (rightDis <= EDGE_THRESHOLD)
            moveDir += Vector3.right;
        if (leftDis <= EDGE_THRESHOLD)
            moveDir += Vector3.left;

        Vector3 movePos = moveDir.normalized * Time.deltaTime * _camMoveSpeed;

        // 카메라 위치 갱신
        float newPosX = Mathf.Clamp(transform.position.x + movePos.x, _mapBottomLeft.x, _mapTopRight.x);
        float newPosY = Mathf.Clamp(transform.position.y + movePos.y, _mapBottomLeft.y, _mapTopRight.y);

        Vector3 newPos = new Vector3(newPosX, newPosY, _trans.position.z);
        transform.position = newPos;
    }

    private void MapSizeCheck()
    {
        _mapSize = GameInitializer.Instance.GetMapSize() * 0.5f;

        // 맵 사이즈가 존재하지 않는 경우
        if (_mapSize == Vector2.zero)
            return;

        _camSizeHegiht = _camera.orthographicSize;
        _camSizeWidth = _camSizeHegiht * _camera.aspect;

        _mapBottomLeft = new Vector2(-_mapSize.x + _camSizeWidth, -_mapSize.y+ _camSizeHegiht);
        _mapTopRight = new Vector2(_mapSize.x - _camSizeWidth, _mapSize.y - _camSizeHegiht);
    }
}
