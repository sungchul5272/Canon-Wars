using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DemoGameUI : MonoBehaviour
{
    public Button leaveButton;
    //public Button endButton;

    void Start()
    {
        leaveButton.onClick.AddListener(() =>
        {
            IngameManager.Instance.LeaveGame();
        });

        //endButton.onClick.AddListener(() =>
        //{
        //    IngameManager.Instance.BackToLobby();
        //});
    }
}
