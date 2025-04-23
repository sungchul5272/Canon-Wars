using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum eTankType
{
    Random = 0,
    Green,
    Yellow,
    Max
}

public class ButtonTankSelect : MonoBehaviour
{
    public List<Sprite> _textureTankList = new List<Sprite>();
    public Image _imgTank = null;

    private Button _btnSelectTank = null;
    private Action<eTankType> _callback;
    private eTankType _selectTankType = eTankType.Random;

    public void Set(eTankType type, Action<eTankType> pCallback)
    {
        _btnSelectTank = GetComponent<Button>();
        _btnSelectTank.onClick.AddListener(OnClick_Tank);

        _callback = pCallback;

        switch(type)
        {
            case eTankType.Random:
                _imgTank.sprite = _textureTankList[(int)eTankType.Random];
                _selectTankType = eTankType.Random;
                break;
            case eTankType.Green:
                _imgTank.sprite = _textureTankList[(int)eTankType.Green];
                _selectTankType = eTankType.Green;
                break;
            case eTankType.Yellow:
                _imgTank.sprite = _textureTankList[(int)eTankType.Yellow];
                _selectTankType = eTankType.Yellow;
                break;
        }
    }

    public void OnClick_Tank()
    {
        if (_callback != null)
            _callback.Invoke(_selectTankType);
    }
}
