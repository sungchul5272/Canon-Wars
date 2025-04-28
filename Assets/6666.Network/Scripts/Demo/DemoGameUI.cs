using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DemoGameUI : MonoBehaviour
{
    //public Button leaveButton;
    public Button BackToLobbyButton;

    void Start()
    {
        //leaveButton.onClick.AddListener(() =>
        //{
        //    LeaveGame();
        //    Debug.Log("Leave game.");
        //});

        BackToLobbyButton.onClick.AddListener(() =>
        {
            BackToLobby();
        });
    }

    //void LeaveGame()
    //{
    //    NetworkManager.Singleton.Shutdown();
    //}

    void BackToLobby()
    {
        Debug.Log("Back to lobby.");
        // ∞‘¿” æ¿ ∑ŒµÂ
        NetworkManager.Singleton.SceneManager.LoadScene("2.LobbyScene", LoadSceneMode.Single);
        //NetworkManager.Singleton.Shutdown();
    }
}
