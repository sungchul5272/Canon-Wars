using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainLobbyUI : MonoBehaviour
{
    public PlayerSlotUI playerSlotPrefab;
    public GameObject sessionEndedUI;
    public GameObject connectionFailedUI;
    public GameObject gameStartFailedUI;
    public GameObject kickedUI;
    public ScrollRect playerScrollView;
    public RectTransform playerScrollContent;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI lobbyCodeText;
    public Toggle readyToggle;
    public Button playButton;
    public Button copyCodeButton;
    public Button sessionEndedButton;
    public Button connectionFailedButton;
    public Button gameStartFailedButton;
    public Button kickedButton;
    public Button backButton;
    public Button mapChangeButton;

    [Header("Map")]
    public RectTransform contentMap;
    public Image mapImage;
    public Button leftMapButton;
    public Button rightMapButton;
    public TextMeshProUGUI mapNameText;

    [Header("Tank")]
    public Transform contentTank;
    public ButtonTankSelect tankSelectButtonPrefab;

    List<PlayerSlotUI> _playerSlots = new();
    List<Button> _tankSelectButtons = new();

    eMapType _changedMapType = eMapType.Random;

    public void Init()
    {
        playerScrollView.gameObject.SetActive(false);
        LobbyManager instance = LobbyManager.Instance;

        // 준비
        readyToggle.onValueChanged.AddListener((_) =>
        {
            SoundManager.Instance.PlayButtonClick();
            readyToggle.interactable = false;
            instance.ReadyPlayer(readyToggle.isOn);
        });

        // 게임 시작
        playButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlayButtonClick();
            instance.StartGameAsHost();
        });

        // 로비 코드 복사
        copyCodeButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlayButtonClick();
            GUIUtility.systemCopyBuffer = lobbyCodeText.text;
        });

        // 로비가 삭제된 경우 확인 버튼
        sessionEndedButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlayButtonClick();
            sessionEndedUI.SetActive(false);
            LeaveMainLobbyUI();
        });

        // 연결이 끊어진 경우 확인 버튼
        connectionFailedButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlayButtonClick();
            connectionFailedUI.SetActive(false);
        });

        // 게임 시작이 실패한 경우 확인 버튼
        gameStartFailedButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlayButtonClick();
            gameStartFailedUI.SetActive(false);
        });

        // 추방된 경우 확인 버튼
        kickedButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlayButtonClick();
            kickedUI.SetActive(false);
            LeaveMainLobbyUI();
        });

        // 맵 변경 적용 버튼
        mapChangeButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlayButtonClick();
            mapChangeButton.interactable = false;
            leftMapButton.interactable = false;
            rightMapButton.interactable = false;
            ApplySelectedMap(_changedMapType);
        });

        // 맵 선택 화살표 버튼
        leftMapButton.onClick.AddListener(OnClick_MapLeft);
        rightMapButton.onClick.AddListener(OnClick_MapRight);

        // 탱크 버튼 프리팹
        InitMapSelect();
        InitSelectTankButtons();
    }

    void OnEnable()
    {
        readyToggle.isOn = false;
        playerScrollView.gameObject.SetActive(false);
        sessionEndedUI.SetActive(false);
        kickedUI.SetActive(false);
        playButton.gameObject.SetActive(true);
        playButton.interactable = false;

        // 슬롯 전부 비활성화
        for (int i = 0; i < _playerSlots.Count; i++)
        {
            _playerSlots[i].gameObject.SetActive(false);
            _playerSlots[i].ShowReadyUI(false);
        }

        // 뒤로가기
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlayButtonClick();
            LobbyManager.Instance.LeaveLobby();
        });
    }

    // 맵 선택창 세팅
    void InitMapSelect()
    {
        // 위치 초기화
        contentMap.offsetMin = new Vector2(0, -256f);

        // 맵 이미지 인스턴스
        for (int i = (int)eMapType.Valley; i < (int)eMapType.Max; i++)
        {
            Image img = Instantiate(mapImage, contentMap);
            img.sprite = SODataManager.instance.GetMapData((eMapType)i).mapSprite;
        }
    }

    // 탱크 선택창 세팅
    void InitSelectTankButtons()
    {
        tankSelectButtonPrefab.gameObject.SetActive(false);
        for (int i = (int)eMapType.Random; i < (int)eTankType.Max; i++)
        {
            ButtonTankSelect btnTankSelect = Instantiate(tankSelectButtonPrefab, contentTank);
            btnTankSelect.gameObject.SetActive(true);
            btnTankSelect.Set((eTankType)i, GetSelectTankType);
            _tankSelectButtons.Add(btnTankSelect._btnSelectTank);
        }
    }

    void OnClick_MapLeft()
    {
        SoundManager.Instance.PlayButtonClick();
        LobbyManager instance = LobbyManager.Instance;

        // 맵
        _changedMapType--;
        if ((int)_changedMapType < (int)eMapType.Random)
        {
            _changedMapType = eMapType.Random;
        }

        // 위치
        float moveX = contentMap.offsetMin.x + 512f;
        contentMap.offsetMin = new Vector2(moveX, contentMap.offsetMin.y);

        // 호스트에게만 화살표 버튼이 나오도록 
        bool isLeftEnd = (int)_changedMapType == -1; //moveX >= 0f ? true : false;
        leftMapButton.gameObject.SetActive(instance.IsLobbyHost && !isLeftEnd);
        rightMapButton.gameObject.SetActive(instance.IsLobbyHost);

        // 맵 이름
        mapNameText.text = _changedMapType.ToString();

        // 맵 변경 버튼 활성화 여부
        mapChangeButton.gameObject.SetActive(_changedMapType != instance.SelectedMapType);
    }

    void OnClick_MapRight()
    {
        SoundManager.Instance.PlayButtonClick();
        LobbyManager instance = LobbyManager.Instance;

        // 맵
        _changedMapType++;
        if ((int)_changedMapType >= (int)eMapType.Max)
        {
            _changedMapType = eMapType.Max - 1;
        }

        // 위치
        float moveX = contentMap.offsetMin.x - 512f;
        contentMap.offsetMin = new Vector2(moveX, contentMap.offsetMin.y);

        // 호스트에게만 화살표 버튼이 나오도록 
        leftMapButton.gameObject.SetActive(instance.IsLobbyHost);
        bool isRightEnd = (int)_changedMapType == (int)eMapType.Max - 1; //moveX <= -2560f ? true : false;
        rightMapButton.gameObject.SetActive(instance.IsLobbyHost && !isRightEnd);

        // 맵 이름
        mapNameText.text = _changedMapType.ToString();

        // 맵 변경 버튼 활성화 여부
        mapChangeButton.gameObject.SetActive(_changedMapType != instance.SelectedMapType);
    }

    public void EnterMainLobbyUI(string lobbyName, string lobbyCode)
    {
        // 로비 입장
        LobbyManager instance = LobbyManager.Instance;

        // 탱크 랜덤으로 리셋
        instance.SelectedTankType = eTankType.Random;

        // 맵 랜덤으로 리셋
        contentMap.offsetMin = new Vector2(0, -256f);
        _changedMapType = eMapType.Random;
        instance.SelectedMapType = eMapType.Random;
        mapNameText.text = instance.SelectedMapType.ToString();

        // UI 리셋
        instance.createLobbyUI.gameObject.SetActive(false);
        instance.sortLobbyUI.gameObject.SetActive(false);
        gameObject.SetActive(true);
        titleText.SetText(lobbyName);
        lobbyCodeText.SetText(lobbyCode);
        playButton.gameObject.SetActive(instance.IsLobbyHost);
        readyToggle.gameObject.SetActive(!instance.IsLobbyHost);
        leftMapButton.gameObject.SetActive(false);
        rightMapButton.gameObject.SetActive(instance.IsLobbyHost);
    }

    public void LeaveMainLobbyUI()
    {
        // 로비 퇴장
        LobbyManager.Instance.startLobbyUI.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    public void RefreshPlayersUI(int playerIndex, bool gameReady)
    {
        LobbyManager instance = LobbyManager.Instance;
        for (int i = 0; i < instance.LobbyPlayerDatas.Count || i < _playerSlots.Count; i++)
        {
            if (i >= _playerSlots.Count)
            {
                // 슬롯이 부족하면 새로 생성
                PlayerSlotUI playerSlot = Instantiate(playerSlotPrefab, playerScrollContent);
                _playerSlots.Add(playerSlot);
                ShowPlayerSlot(playerSlot, i);
            }
            else if (i < instance.LobbyPlayerDatas.Count)
            {
                // 아니면 기존 슬롯 활성화
                ShowPlayerSlot(_playerSlots[i], i);
            }

            // 남은 슬롯은 비활성화
            if (i >= instance.LobbyPlayerDatas.Count)
            {
                _playerSlots[i].gameObject.SetActive(false);
            }
        }

        float slotHeight = playerSlotPrefab.rectTransform.sizeDelta.y;
        playerScrollContent.sizeDelta = new Vector2(playerScrollContent.sizeDelta.x, instance.LobbyPlayerDatas.Count * slotHeight);
        readyToggle.interactable = readyToggle.isOn == LobbyManager.Instance.LobbyPlayerDatas[playerIndex].ready;
        playButton.interactable = gameReady;
        playerScrollView.gameObject.SetActive(true);
        SetTankSelectInteractable(true);

        if (instance.IsLobbyHost && _changedMapType == instance.SelectedMapType)
        {
            leftMapButton.interactable = true;
            rightMapButton.interactable = true;
            mapChangeButton.interactable = true;
            mapChangeButton.gameObject.SetActive(false);
        }
    }

    public void ShowSessionEndedUI()
    {
        // 로비에서 나와졌을 경우
        sessionEndedUI.SetActive(true);
    }

    public void ShowConnectionFailedUI()
    {
        // 연결이 끊어졌을 경우
        connectionFailedUI.SetActive(true);
    }

    public void ShowGameStartFailedUI()
    {
        // 게임 시작이 실패한 경우
        gameStartFailedUI.SetActive(true);
    }

    public void ShowKickedUI()
    {
        // 추방되었을 경우
        kickedUI.SetActive(true);
    }

    public void HideOtherKickButtonsUI(int index)
    {
        // 누르지 않은 플레이어의 추방 버튼 비활성화
        for (int i = 0; i < LobbyManager.Instance.LobbyPlayerDatas.Count; i++)
        {
            if (i != index)
            {
                _playerSlots[i].HideKickButtonUI();
            }
        }
    }

    public void ShowSelectedMap(eMapType mapType)
    {
        LobbyManager instance = LobbyManager.Instance;
        instance.SelectedMapType = mapType;

        float moveX = -512f * ((int)instance.SelectedMapType + 1);
        contentMap.offsetMin = new Vector2(moveX, contentMap.offsetMin.y);

        mapNameText.text = instance.SelectedMapType.ToString();
    }

    public void SetTankSelectInteractable(bool value)
    {
        for (int i = 0; i < _tankSelectButtons.Count; i++)
        {
            _tankSelectButtons[i].interactable = value;
        }
    }

    void ShowPlayerSlot(PlayerSlotUI playerSlot, int i)
    {
        LobbyManager instance = LobbyManager.Instance;
        playerSlot.gameObject.SetActive(true);
        playerSlot.ShowPlayerNameUI(instance.LobbyPlayerDatas[i].name);
        playerSlot.ShowReadyUI(instance.LobbyPlayerDatas[i].ready);
        playerSlot.ShowPlayerTank(instance.LobbyPlayerDatas[i].selectedTank);
    }

    void GetSelectTankType(eTankType tankType)
    {
        LobbyManager instance = LobbyManager.Instance;
        instance.SelectedTankType = tankType;
        instance.ChangeTank(instance.SelectedTankType);
    }

    void ApplySelectedMap(eMapType mapType)
    {
        LobbyManager instance = LobbyManager.Instance;
        instance.ChangeMap(mapType);
    }
}
