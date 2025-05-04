using UnityEngine;
using UnityEngine.UI;

public class DemoGameUI : MonoBehaviour
{
    public Button leaveButton;

    void Start()
    {
        leaveButton.onClick.AddListener(() =>
        {
            NetworkPlayerData.RemoveGameInfo();
            StartCoroutine(IngameManager.Instance.LeaveGameAsync());
        });
    }
}
