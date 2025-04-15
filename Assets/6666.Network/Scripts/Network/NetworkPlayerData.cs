using UnityEngine;

public static class NetworkPlayerData
{
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
}
