using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
<<<<<<< HEAD
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

=======

public class InfoPanel : MonoBehaviour
{
    [Header("���� ���� �ؽ�Ʈ")]
    [SerializeField] private Text _nicknameText;
    [SerializeField] private Text _uidText;

    [Header("��ư")]
    [SerializeField] private Button _applyBtn;
    [SerializeField] private Button _confirmBtn;
    [SerializeField] private Button[] _closeBtns;

    [Header("��ũ ���� ����")]
    [SerializeField] private Transform[] tankSlots; // ���� ������ �̹����� �־�� ��
    [SerializeField] private Image _equippedTankImage; // ���� ��ũ �̸����� �̹���

    private List<TankDataSO> allTankDataList = new List<TankDataSO>();
    private TankDataSO _selectedTank; // ���õ� ��ũ ������

    private Image _previousSlotImage;
    private Color _defaultColor = Color.white;
    private Color _selectedColor = Color.yellow;
>>>>>>> 7b40d61 (0328 탱크 선택 기능 추가)

    void Start()
    {
        _applyBtn.onClick.AddListener(OnClickApply);
        _confirmBtn.onClick.AddListener(OnClickConfirm);
        foreach (Button btn in _closeBtns)
        {
            btn.onClick.AddListener(OnClickClose);
        }
<<<<<<< HEAD
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
=======

        InitTankData();
    }

    void InitTankData()
    {
        allTankDataList.Clear();
        TankDataSO[] tankArray = Resources.LoadAll<TankDataSO>("Tank");
        allTankDataList.AddRange(tankArray);

        for (int i = 0; i < tankSlots.Length; i++)
        {
            if (i < allTankDataList.Count)
            {
                Transform imageTransform = tankSlots[i].GetChild(0);
                Image image = imageTransform.GetComponent<Image>();
                image.sprite = allTankDataList[i]._tankSprite;
                image.gameObject.SetActive(true);

                // �ڵ鷯 �ڵ� ����
                TankSlotClickHandler handler = imageTransform.GetComponent<TankSlotClickHandler>();
                if (handler == null)
                {
                    handler = imageTransform.gameObject.AddComponent<TankSlotClickHandler>();
                }

                handler.slotIndex = i;
                handler.infoPanel = this;
            }
            else
            {
                tankSlots[i].gameObject.SetActive(false);
            }
        }
    }


    void OnClickApply()
    {
        if (_selectedTank != null)
        {
            _equippedTankImage.sprite = _selectedTank._tankSprite;
            Debug.Log($" ��ũ ���� �Ϸ�: {_selectedTank._tankName}");
        }
        else
        {
            Debug.LogWarning(" ���õ� ��ũ�� �����ϴ�.");
        }
    }

>>>>>>> 7b40d61 (0328 탱크 선택 기능 추가)
    void OnClickConfirm()
    {
        Debug.Log("Ȯ�� ��ư Ŭ��");
    }

    void OnClickClose()
    {
        gameObject.SetActive(false);
    }

<<<<<<< HEAD
=======
    public void OnSelectTankFromSlot(int index)
    {
        _selectedTank = allTankDataList[index];
        Debug.Log($" ���õ� ��ũ: {_selectedTank._tankName}");

        // ���� ���� �̹���
        Image currentSlotImage = tankSlots[index].GetComponent<Image>();

        if (currentSlotImage != null)
        {
            // ���� ���� ����
            if (_previousSlotImage != null)
                _previousSlotImage.color = _defaultColor;

            // ���� ���� ����
            currentSlotImage.color = _selectedColor;
            _previousSlotImage = currentSlotImage;
        }
    }

>>>>>>> 7b40d61 (0328 탱크 선택 기능 추가)

}
