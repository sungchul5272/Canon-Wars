using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DemoGameUI : MonoBehaviour
{
    public Button leaveButton;
    public Button endTurnButton;
    public Button endGameButton;

    NetworkVariable<int> turnIndex = new();

    void Start()
    {
        leaveButton.onClick.AddListener(() =>
        {
            LeaveGame();
            Debug.Log("Leave game.");
        });

        endTurnButton.onClick.AddListener(() =>
        {
            EndTurn();
            Debug.Log("End turn.");
        });

        endGameButton.onClick.AddListener(() =>
        {
            EndGame();
            Debug.Log("End game.");
        });
    }

    void LeaveGame()
    {
        NetworkManager.Singleton.Shutdown();
    }

    void EndTurn()
    {

    }

    void EndGame()
    {

    }
}
