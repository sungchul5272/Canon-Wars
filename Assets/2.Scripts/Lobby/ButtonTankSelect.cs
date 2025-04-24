using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonTankSelect : MonoBehaviour
{
    public Sprite _textureRandom = null;
    public Image _imgTank = null;

    private Button _btnSelectTank = null;
    private Action<eTankType> _callback;
    private TankDataSO _tankData = null;

    public void Set(eTankType type, Action<eTankType> pCallback)
    {
        _btnSelectTank = GetComponent<Button>();
        _btnSelectTank.onClick.AddListener(OnClick_Tank);

        _callback = pCallback;

        _tankData = SODataManager.instance.GetTankData(type);

        // null 인경우 Random
        _imgTank.sprite = _tankData == null ? _textureRandom : _tankData._tankSprite;
    }

    public void OnClick_Tank()
    {
        if (_callback != null)
        {
            // null 인경우 Random
            if( _tankData == null ) 
            {
                _callback.Invoke(eTankType.Random);
            }
            else
            {
                _callback.Invoke(_tankData._tankType);
            }
        }
    }
}
