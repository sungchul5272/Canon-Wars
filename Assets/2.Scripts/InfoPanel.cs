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
    [Header("유저 정보 텍스트")]
    [SerializeField] private Text _nicknameText;
    [SerializeField] private Text _uidText;

    [Header("버튼")]
    [SerializeField] private Button _applyBtn;
    [SerializeField] private Button _confirmBtn;
    [SerializeField] private Button[] _closeBtns;

    [Header("탱크 슬롯 관련")]
    [SerializeField] private Transform[] tankSlots; // 슬롯 하위에 이미지가 있어야 함
    [SerializeField] private Image _equippedTankImage; // 장착 탱크 미리보기 이미지

    private List<TankDataSO> allTankDataList = new List<TankDataSO>();
    private TankDataSO _selectedTank; // 선택된 탱크 데이터

    private Image _previousSlotImage;
    private Color _defaultColor = Color.white;
    private Color _selectedColor = Color.yellow;
>>>>>>> 7b40d61 (0328 �꺊�겕 �꽑�깮 湲곕뒫 異붽��)

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
        // 기존 슬롯 제거
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
        // 테두리 처리
        if (previouslySelectedImage != null)
            previouslySelectedImage.color = Color.white;

        image.color = Color.green;
        previouslySelectedImage = image;

        selectedTankId = tankId;
    }


    void OnClickApply()
    {
        Debug.Log("적용 버튼 클릭");
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

                // 핸들러 자동 부착
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
            Debug.Log($" 탱크 장착 완료: {_selectedTank._tankName}");
        }
        else
        {
            Debug.LogWarning(" 선택된 탱크가 없습니다.");
        }
    }

>>>>>>> 7b40d61 (0328 �꺊�겕 �꽑�깮 湲곕뒫 異붽��)
    void OnClickConfirm()
    {
        Debug.Log("확인 버튼 클릭");
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
        Debug.Log($" 선택된 탱크: {_selectedTank._tankName}");

        // 현재 슬롯 이미지
        Image currentSlotImage = tankSlots[index].GetComponent<Image>();

        if (currentSlotImage != null)
        {
            // 이전 선택 해제
            if (_previousSlotImage != null)
                _previousSlotImage.color = _defaultColor;

            // 현재 선택 강조
            currentSlotImage.color = _selectedColor;
            _previousSlotImage = currentSlotImage;
        }
    }

>>>>>>> 7b40d61 (0328 �꺊�겕 �꽑�깮 湲곕뒫 異붽��)

}
