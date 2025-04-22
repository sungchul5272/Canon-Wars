using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainLobbyUI : MonoBehaviour
{
    public PlayerSlotUI playerSlotPrefab;
    public GameObject sessionEndedUI;
    public GameObject kickedUI;
    public ScrollRect playerScrollView;
    public RectTransform playerScrollContent;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI lobbyCodeText;
    public Toggle readyToggle;
    public Button playButton;
    public Button copyCodeButton;
    public Button sessionEndedButton;
    public Button kickedButton;
    public Button backButton;

    List<PlayerSlotUI> _playerSlots = new();

    void Start()
    {
        playerScrollView.gameObject.SetActive(false);
        LobbyManager instance = LobbyManager.Instance;

        // 준비
        readyToggle.onValueChanged.AddListener((_) =>
        {
            readyToggle.interactable = false;
            instance.ReadyPlayer(readyToggle.isOn);
        });

        // 게임 시작
        playButton.onClick.AddListener(() =>
        {
            instance.StartGameAsHost();
        });

        // 로비 코드 복사
        copyCodeButton.onClick.AddListener(() =>
        {
            GUIUtility.systemCopyBuffer = lobbyCodeText.text;
        });

        // 로비가 삭제된 경우 확인 버튼
        sessionEndedButton.onClick.AddListener(() =>
        {
            sessionEndedUI.SetActive(false);
            LeaveMainLobbyUI();
        });

        // 추방된 경우 확인 버튼
        kickedButton.onClick.AddListener(() =>
        {
            kickedUI.SetActive(false);
            LeaveMainLobbyUI();
        });
    }

    void OnEnable()
    {
        readyToggle.isOn = false;
        playerScrollView.gameObject.SetActive(false);
        playButton.gameObject.SetActive(true);
        sessionEndedUI.SetActive(false);
        kickedUI.SetActive(false);

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
            LobbyManager.Instance.LeaveLobby();
        });
    }

    public void EnterMainLobbyUI(string lobbyName, string lobbyCode)
    {
        // 로비 입장
        LobbyManager instance = LobbyManager.Instance;
        instance.createLobbyUI.gameObject.SetActive(false);
        instance.sortLobbyUI.gameObject.SetActive(false);
        gameObject.SetActive(true);
        titleText.SetText(lobbyName);
        lobbyCodeText.SetText(lobbyCode);
        playButton.gameObject.SetActive(instance.IsLobbyHost);
        readyToggle.gameObject.SetActive(!instance.IsLobbyHost);
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
    }

    public void ShowSessionEndedUI()
    {
        // 로비에서 나와졌을 경우
        sessionEndedUI.SetActive(true);
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

    void ShowPlayerSlot(PlayerSlotUI playerSlot, int i)
    {
        LobbyManager instance = LobbyManager.Instance;
        playerSlot.gameObject.SetActive(true);
        playerSlot.ShowPlayerNameUI(instance.LobbyPlayerDatas[i].name);
        playerSlot.ShowReadyUI(instance.LobbyPlayerDatas[i].ready);
    }
}
