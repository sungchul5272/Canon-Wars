using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultUI : MonoBehaviour
{
    public static ResultUI Instance { get; private set; }

    [SerializeField]  Text _resultText;
    [SerializeField]  Button _confirmButton;
    [SerializeField] GameObject _resultUI;

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
        _confirmButton.onClick.RemoveAllListeners();
        _confirmButton.onClick.AddListener(OnClickConfirmButton);

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

    private void OnClickConfirmButton()
    {
        Debug.Log("[ResultUI] 확인 버튼 클릭됨, 로비로 이동");

        SceneManager.LoadScene("2.LobbyScene");
    }
}
