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

    string _opponentTurn = "상대턴";
    string _timerEnd = "시간 초과";

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
        UpdateTurnUI();
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

    public void SetWind(float wind)
    {
        float absWind = Mathf.Abs(wind);
        _windText.text = $"{absWind:F1} m/s";

        Vector3 scale = _wind.transform.localScale;
        scale.x = wind < 0 ? -1 : 1;
        _wind.transform.localScale = scale;

    }

    void UpdateTurnUI()
    {
        if (IngameManager.Instance == null || !IngameManager.Instance.IsSpawned)
            return;

        if (IngameManager.Instance.IsMyTurn())
        {
            float turnTime = IngameManager.Instance.GetTurnTime();
            int intTurnTime = Mathf.FloorToInt(turnTime);
            _ingameTimer.text = (turnTime < 0) ? _timerEnd : intTurnTime.ToString();
        }
        else
        {
            _ingameTimer.text = _opponentTurn;
        }
    }
}
