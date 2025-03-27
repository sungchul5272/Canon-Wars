using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEditor.U2D.Aseprite;

public class InfoPanel : MonoBehaviour
{
    [SerializeField] Text _nicknameText;
    [SerializeField] Text _uidText;
    [SerializeField] Image _selectedTankImage;
    [SerializeField] Transform _tankSlotContainer;
    [SerializeField] GameObject _tankSlotPrefab;

    [SerializeField] Button _applyBtn;
    [SerializeField] Button _confirmBtn;
    [SerializeField] Button[] _closeBtns;
    [SerializeField] Sprite defaultTankSprite;


    string selectedTankId;


    void Start()
    {
        _applyBtn.onClick.AddListener(OnClickApply);
        _confirmBtn.onClick.AddListener(OnClickConfirm);
        foreach (Button btn in _closeBtns)
        {
            btn.onClick.AddListener(OnClickClose);
        }
    }

    void LoadOwnedTankSlots(List<string> tankList)
    {
        // ���� ���� ����
        foreach (Transform child in _tankSlotContainer)
            Destroy(child.gameObject);

        foreach (string tankId in tankList)
        {
            GameObject slot = Instantiate(_tankSlotPrefab, _tankSlotContainer);
            Image image = slot.GetComponent<Image>();

            Button button = slot.GetComponent<Button>();
            button.onClick.AddListener(() => OnSelectTank(tankId, image));
        }
    }

    Image previouslySelectedImage;

    void OnSelectTank(string tankId, Image image)
    {
        // �׵θ� ó��
        if (previouslySelectedImage != null)
            previouslySelectedImage.color = Color.white;

        image.color = Color.green;
        previouslySelectedImage = image;

        selectedTankId = tankId;
    }


    void OnClickApply()
    {
        Debug.Log("���� ��ư Ŭ��");
    }
    void OnClickConfirm()
    {
        Debug.Log("Ȯ�� ��ư Ŭ��");
    }

    void OnClickClose()
    {
        gameObject.SetActive(false);
    }


}
