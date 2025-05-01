using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class ResultUI : MonoBehaviour
{
    public static ResultUI Instance { get; private set; }

    [SerializeField] Text _resultText;
    [SerializeField] Button _backToMainBtn;
    [SerializeField] Button _backToRoomBtn;
    [SerializeField] Button _waitCancleBtn;
    [SerializeField] GameObject _resultUI;
    [SerializeField] GameObject _waitingUI;

    private void Awake()
    {
        Debug.Log("[ResultUI] Awake 호출됨");

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        _backToMainBtn.onClick.RemoveAllListeners();
        _backToMainBtn.onClick.AddListener(OnClickBackToMainButton);
        _backToRoomBtn.onClick.RemoveAllListeners();
        _backToRoomBtn.onClick.AddListener(OnClickBackToRoomButton);
        _waitCancleBtn.onClick.RemoveAllListeners();
        _waitCancleBtn.onClick.AddListener(OnClickWaitCancleButton);

    }

    public void ShowResult(string resultType)
    {
        Debug.Log($"[ResultUI] ShowResult 호출됨: {resultType}");

        _resultUI.SetActive(true);

        switch (resultType)
        {
            case "승":
                _resultText.text = "승리!";
                break;
            case "패":
                _resultText.text = "패배...";
                break;
            case "무":
                _resultText.text = "무승부!";
                break;
            default:
                _resultText.text = "결과 알 수 없음";
                break;
        }
    }

    private void OnClickBackToMainButton()
    {
        IngameManager.Instance.LeaveGame();
    }

    private void OnClickBackToRoomButton()
    {

        //if(만약 호스트가 아닌 클라이언트라면)
        //{
        //    _waitingUI.SetActive(true);
        //    멀티서버에 연결을 대기하는 기능 추가 필요
        //}

        if(NetworkManager.Singleton.IsServer)
        {
            IngameManager.Instance.BackToLobby();
        }

    }
    private void OnClickWaitCancleButton()
    {
        _waitingUI.SetActive(false);
        // 연결시도 해제하는 기능 추가 필요
    }
}
