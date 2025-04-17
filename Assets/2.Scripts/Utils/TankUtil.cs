using System.Collections.Generic;
using UnityEngine;

public static class TankUtil
{
    /// <summary>
    /// 현재 탱크 키에 해당하는 탱크 스프라이트를 리소스에서 찾아 반환합니다.
    /// </summary>
    public static Sprite GetTankSprite(string tankKey)
    {
        if (string.IsNullOrEmpty(tankKey)) return null;

        TankDataSO[] tankArray = Resources.LoadAll<TankDataSO>("Tank");

        foreach (var tank in tankArray)
        {
            if (tank._tankName == tankKey)
                return tank._tankSprite;
        }

        return null;
    }

    /// <summary>
    /// 현재 탱크 키에 해당하는 탱크 데이터 자체를 반환합니다.
    /// </summary>
    public static TankDataSO GetTankData(string tankKey)
    {
        if (string.IsNullOrEmpty(tankKey)) return null;

        TankDataSO[] tankArray = Resources.LoadAll<TankDataSO>("Tank");

        foreach (var tank in tankArray)
        {
            if (tank._tankName == tankKey)
                return tank;
        }

        return null;
    }
}