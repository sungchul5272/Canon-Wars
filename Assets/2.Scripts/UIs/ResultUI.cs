using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections;

public class ResultUI : MonoBehaviour
{
    [SerializeField] Text _resultText;
    [SerializeField] Text _waitText;
    [SerializeField] Text _failText;
    [SerializeField] Button _backToMainBtn;
    [SerializeField] Button _backToRoomBtn;
    [SerializeField] Button _waitCancleBtn;
    [SerializeField] GameObject _statusUI;
    [SerializeField] GameObject _connectWaitingUI;
    [SerializeField] GameObject _spinnerIcon;
    [SerializeField] GameObject _failIcon;

    Coroutine _waitHostCoroutine;

    float _waitHostTimeout = 20;

    void Awake()
    {
        Debug.Log("[ResultUI] Awake 호출됨");

        _backToMainBtn.onClick.AddListener(OnClickBackToMainButton);
        _backToRoomBtn.onClick.AddListener(OnClickBackToRoomButton);
        _waitCancleBtn.onClick.AddListener(OnClickWaitCancelButton);
    }

    public void ShowResult(string resultType)
    {
        Debug.Log($"[ResultUI] ShowResult 호출됨: {resultType}");

        _statusUI.SetActive(true);

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

    void OnClickBackToMainButton()
    {
        // 떠나기
        NetworkPlayerData.RemoveGameInfo();
        StartCoroutine(IngameManager.Instance.LeaveGameAsync());
    }

    void OnClickBackToRoomButton()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            // 호스트는 바로 복귀 가능
            StartCoroutine(IngameManager.Instance.SetHostToLobby());
        }
        else
        {
            _connectWaitingUI.SetActive(true);
            SetWaitingState(true);

            // 클라이언트는 호스트가 복귀를 누를 떄까지 대기
            _waitHostCoroutine = StartCoroutine(WaitHostAsync());
        }
    }

    void SetWaitingState(bool waiting)
    {
        _waitText.gameObject.SetActive(waiting);
        _spinnerIcon.gameObject.SetActive(waiting);
        _failText.gameObject.SetActive(!waiting);
        _failIcon.gameObject.SetActive(!waiting);
    }

    IEnumerator WaitHostAsync()
    {
        float timer = 0;
        while (!IngameManager.Instance.HostBackToLobby)
        {
            if (timer > _waitHostTimeout)
            {
                SetWaitingState(false);
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        StartCoroutine(IngameManager.Instance.LeaveGameAsync());
    }

    void OnClickWaitCancelButton()
    {
        _connectWaitingUI.SetActive(false);

        // 로비 복귀 대기 취소
        if (_waitHostCoroutine != null)
        {
            StopCoroutine(_waitHostCoroutine);
            _waitHostCoroutine = null;
            Debug.Log("로비 복귀 대기 취소.");
        }
    }
}
