using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbySlotUI : MonoBehaviour
{
    public RectTransform rectTransform;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI gameModeText;
    public Button joinButton;

    void OnEnable()
    {
        LobbyManager instance = LobbyManager.Instance;

        joinButton.enabled = true;

        // 참가 버튼
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() =>
        {
            joinButton.enabled = false;
            instance.JoinLobby(string.Empty, transform.GetSiblingIndex(), string.Empty);
        });
    }

    public void ShowLobbyUI(string name, string gameMode)
    {
        nameText.SetText(name);
        gameModeText.SetText(gameMode);
    }
}
