using UnityEngine;
using UnityEngine.UI;

public class IngameUIController : MonoBehaviour
{
    public static IngameUIController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Text _ingameTimer;
    [SerializeField] private GameObject _wind;
    [SerializeField] private Text _windText;
    [SerializeField] private Slider _hpBar;
    [SerializeField] private Slider _fuelBar;

    [Header("미사일 UI")]
    [SerializeField] private GameObject _firstMissile;
    [SerializeField] private GameObject _secondMissile;

    public void Init()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        HandleMissileSelection();
    }

    void HandleMissileSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetMissileUI(_firstMissile, _secondMissile);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetMissileUI(_secondMissile, _firstMissile);
        }
    }

    void SetMissileUI(GameObject selected, GameObject unselected)
    {
        selected.transform.SetAsLastSibling();   // 앞으로
        unselected.transform.SetAsFirstSibling(); // 뒤로
    }


    // 체력 업데이트
    public void SetHP(float value)
    {
        _hpBar.value = Mathf.Clamp(value, 0, 100);
    }

    // 연료 업데이트
    public void SetFuel(float value)
    {
        _fuelBar.value = Mathf.Clamp(value, 0, 100);
    }

    // 바람 세기 및 방향 표시
    public void SetWind(float wind)
    {
        _wind.SetActive(true);

        if (wind > 0)
            _windText.text = $"> {wind:F1}";
        else if (wind < 0)
            _windText.text = $"< {-wind:F1}";
        else
            _windText.text = "-";
    }

    // 게임 타이머 표시
    public void SetTimer(float seconds)
    {
        int min = Mathf.FloorToInt(seconds / 60f);
        int sec = Mathf.FloorToInt(seconds % 60f);
        _ingameTimer.text = $"{min:00}:{sec:00}";
    }
}
