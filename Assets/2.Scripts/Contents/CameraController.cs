using System;
using System.Collections;
using System.Collections.Generic;
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
    private bool _cursorLock = true;
    private bool _isFocusingAndZoomOut = false;

    private Coroutine _corCameraZoomOut = null;

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

    public void Init()
    {
        // 원래 카메라 크기 저장
        _camSizeOriginHeight = _camera.orthographicSize;
        _camSizeOriginWidth = _camSizeOriginHeight * _camera.aspect;

        // 카메라 가동 범위 확인
        CheckCameraMovementRange();

        _isInit = true;
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
    public void RestoreCamSize()
    {
        _zoomTime = 0f;
        Camera.main.orthographicSize = _camSizeOriginHeight;
        CheckCameraMovementRange();
    }

    // Update is called once per frame
    void Update()
    {
        if (_isInit == true && GameInitializer.Instance != null && !_isFocusingAndZoomOut)
        {
            MoveCameraAroundMap();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _cursorLock = !_cursorLock;
        }

        Cursor.lockState = _cursorLock ? CursorLockMode.Confined : CursorLockMode.None;
    }

    // 점진적으로 카메라 줌아웃 하는 메소드
    public void StartCameraFocusingAndZoomOut()
    {
        if (IngameManager.Instance == null || IngameManager.Instance.CurShellTrans == null)
            return;

        if (_corCameraZoomOut != null)
        {
            RestoreCamSize();
            StopCoroutine(_corCameraZoomOut);
        }

        _isFocusingAndZoomOut = true;
        _corCameraZoomOut = StartCoroutine(CorCameraFocusingAndZoomOut());
    }


    private IEnumerator CorCameraFocusingAndZoomOut()
    {
        yield return new WaitForSeconds(0.1f);

        float mapMaxCamSize = _mapSize.x / _camera.aspect;
        float camMaxSize = Mathf.Min(_camMaxSize, mapMaxCamSize);
        float camCurSize = 0f;
        Transform prevShellPos = null;

        while (_zoomTime < _camZoomOutTime)
        {
            // 포탄 포커싱
            _curShellTrans = IngameManager.Instance.CurShellTrans;
            prevShellPos = _curShellTrans;

            if (_curShellTrans == null)
                break;

            // 카메라 포커싱
            CameraFocusing(_curShellTrans);

            //줌 아웃
            _zoomTime += Time.deltaTime;

            float progress = _zoomTime / _camZoomOutTime;
           
            camCurSize = Mathf.Lerp(_camSizeOriginHeight, camMaxSize, progress * progress);
            _camera.orthographicSize = camCurSize;   // 외곽을 좀더 벗어나는 경우가 있어서 -0.5f 처리

            yield return null;
        }

        CameraFocusing(prevShellPos);
        _isFocusingAndZoomOut = false;
    }

    private void CameraFocusing(Transform shellTrans)
    {
        if (shellTrans == null)
            return;

        // 카메라 가동 범위 확인
        CheckCameraMovementRange();

        // 카메라가 맵 범위 밖으로 안나가도록 제한
        _newPosX = Mathf.Clamp(shellTrans.position.x, _mapBottomLeft.x, _mapTopRight.x);
        _newPosY = Mathf.Clamp(shellTrans.position.y, _mapBottomLeft.y, _mapTopRight.y);

        Vector3 newPos = new Vector3(_newPosX, _newPosY, _trans.position.z);
        _trans.position = newPos;

        Debug.Log($"Camera Transform : {newPos}");
    }

    private void MoveCameraAroundMap()
    {
        if (!_cursorLock)
        {
            return;
        }

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

        if (moveDir != Vector3.zero)
        {
            Vector3 movePos = _camMoveSpeed * Time.deltaTime * moveDir.normalized;

            // 카메라 위치 갱신
            float newPosX = Mathf.Clamp(transform.position.x + movePos.x, _mapBottomLeft.x, _mapTopRight.x);
            float newPosY = Mathf.Clamp(transform.position.y + movePos.y, _mapBottomLeft.y, _mapTopRight.y);

            Vector3 newPos = new Vector3(newPosX, newPosY, _trans.position.z);
            transform.position = newPos;
        }
    }

    private void CheckCameraMovementRange()
    {
        _mapSize = GameInitializer.Instance.GetMapSize() * 0.5f;

        // 맵 사이즈가 존재하지 않는 경우
        if (_mapSize == Vector2.zero)
            return;

        _camSizeHegiht = _camera.orthographicSize;
        _camSizeWidth = _camSizeHegiht * _camera.aspect;

        float threshhold = 0.1f;

        _mapBottomLeft = new Vector2(-_mapSize.x + _camSizeWidth + threshhold, -_mapSize.y+ _camSizeHegiht + threshhold);
        _mapTopRight = new Vector2(_mapSize.x - _camSizeWidth - threshhold, _mapSize.y - _camSizeHegiht - threshhold);
    }

    public (Vector2 bottomLeft, Vector2 topRight) GetMapBoundPos()
    {
        return (_mapBottomLeft, _mapTopRight); 
    }
}
