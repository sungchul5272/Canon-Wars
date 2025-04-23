using UnityEngine;

public static class NetworkPlayerData
{
    public static eMapType selectedMapType = eMapType.Random;
    public static eTankType selectedTank;

    static int _maxPlayer;

    public static int GetMaxPlayer()
    {
        return _maxPlayer;
    }

    public static int SetMaxPlayer(EGameMode gameMode)
    {
        _maxPlayer = 2 * ((int)gameMode + 1);
        return _maxPlayer;
    }

    public static void SetGameInfo(eMapType mapType, eTankType tankType)
    {
        selectedMapType = mapType;
        selectedTank = tankType;
    }
}
