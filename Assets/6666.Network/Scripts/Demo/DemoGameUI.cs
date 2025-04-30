using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DemoGameUI : MonoBehaviour
{
    public Button leaveButton;
    public Button endButton;

    string _lobbySceneName = "2.LobbyScene";

    void Start()
    {
        leaveButton.onClick.AddListener(() =>
        {
            LeaveGame();
        });

        endButton.onClick.AddListener(() =>
        {
            EndGame();
        });
    }

    void LeaveGame()
    {
        Debug.Log("Leave game and back to lobby.");

        // 연결 해제
        NetworkManager.Singleton.Shutdown();

        // 본인만 로비 씬 로드
        SceneManager.LoadSceneAsync(_lobbySceneName, LoadSceneMode.Single);
    }

    void EndGame()
    {
        Debug.Log("End game and back to lobby.");

        if (NetworkManager.Singleton.IsListening)
        {
            // 지금은 호스트만 가능
            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("Only host can end game.");
                return;
            }

            // 로비 씬 로드
            NetworkManager.Singleton.SceneManager.LoadScene(_lobbySceneName, LoadSceneMode.Single);
        }
        else
        {
            // 연결이 끊긴 경우 본인만 로비 씬 로드
            SceneManager.LoadSceneAsync(_lobbySceneName, LoadSceneMode.Single);
        }
    }
}
