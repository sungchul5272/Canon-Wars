using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultUI : MonoBehaviour
{
    public static ResultUI Instance { get; private set; }

    [SerializeField]  Text _resultText;
    [SerializeField]  Button _confirmButton;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        gameObject.SetActive(false);

        _confirmButton.onClick.RemoveAllListeners();
        _confirmButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("2.LobbyScene");
        });
    }

    public void ShowResult(string resultType)
    {
        Debug.Log($"[ResultUI] ShowResult È£ÃâµÊ: {resultType}");

        gameObject.SetActive(true);

        switch (resultType)
        {
            case "½Â":
                _resultText.text = "½Â¸®!";
                break;
            case "ÆÐ":
                _resultText.text = "ÆÐ¹è...";
                break;
            case "¹«":
                _resultText.text = "¹«½ÂºÎ!";
                break;
            default:
                _resultText.text = "°á°ú ¾Ë ¼ö ¾øÀ½";
                break;
        }
    }
}
