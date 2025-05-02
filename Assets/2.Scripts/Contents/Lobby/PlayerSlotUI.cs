using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSlotUI : MonoBehaviour
{
    public RectTransform rectTransform;
    public TextMeshProUGUI nickNameText;
    public Toggle readyToggle;
    public Button profileButton;
    public Button kickButton;

    [Header("Tank")]
    public Image _imgSelectTank = null;
    public Sprite _spriteRandom = null;

    void OnEnable()
    {
        LobbyManager instance = LobbyManager.Instance;
        HideKickButtonUI();
        int index = transform.GetSiblingIndex();

        // 프로필 보기
        profileButton.onClick.RemoveAllListeners();
        profileButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlayButtonClick();
            instance.mainLobbyUI.HideOtherKickButtonsUI(index);
            kickButton.gameObject.SetActive(!kickButton.gameObject.activeSelf);
            kickButton.interactable = instance.IsLobbyHost && !instance.IsPlayer(index);
        });

        // 추방
        kickButton.onClick.RemoveAllListeners();
        kickButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlayButtonClick();
            instance.KickPlayer(index);
            HideKickButtonUI();
        });
    }

    public void ShowPlayerNameUI(string value)
    {
        nickNameText.SetText(value);
    }

    public void ShowReadyUI(bool value)
    {
        readyToggle.isOn = value;
    }

    public void HideKickButtonUI()
    {
        kickButton.gameObject.SetActive(false);
    }

    public void ShowPlayerTank(eTankType selectTankType)
    {
        TankDataSO tankData = SODataManager.instance.GetTankData(selectTankType);
        _imgSelectTank.sprite = tankData == null ? _spriteRandom : tankData._tankSprite;
    }
}
