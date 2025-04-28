using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingUIPlayerSlot : MonoBehaviour
{
    [SerializeField] Text _playerNameText;
    [SerializeField] Image _tankImage;
    [SerializeField] Text _winLossText;
    [SerializeField] Text _loadingStatusText;

    Coroutine _dotCoroutine;

    // 초기 셋업
    public void Setup(string nickname, Sprite tankSprite, int wins, int losses)
    {
        _playerNameText.text = nickname;
        _tankImage.sprite = tankSprite;
        _winLossText.text = $"{wins}승  {losses}패";
        _loadingStatusText.text = "로딩 중";
        StartLoadingTextAnimation();
    }

    // 로딩중 애니메이션 시작
    public void StartLoadingTextAnimation()
    {
        if (_dotCoroutine != null)
            StopCoroutine(_dotCoroutine);

        _dotCoroutine = StartCoroutine(AnimateDots());
    }

    IEnumerator AnimateDots()
    {
        string baseText = "로딩 중";
        int dotCount = 0;

        while (true)
        {
            _loadingStatusText.text = baseText + new string('.', dotCount % 4);
            dotCount++;
            yield return new WaitForSeconds(0.5f);
        }
    }

    // 로딩 완료 시 호출
    public void SetLoaded()
    {
        if (_dotCoroutine != null)
            StopCoroutine(_dotCoroutine);

        _loadingStatusText.text = "로딩 완료!!";
    }

}
