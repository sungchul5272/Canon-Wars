using UnityEngine;

public static class NetworkPlayerData
{
    public static EGameMode GameMode { get; private set; }
    public static eMapType SelectedMapType { get; private set; } = eMapType.Random;
    public static eTankType SelectedTank { get; private set; }

    public static string LobbyName { get; private set; }
    public static string InternalLobbyCode { get; private set; }
    public static bool IsPrivateLobby { get; private set; }
    public static bool IsHost { get; private set; }

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

    public static void SetGameInfo(EGameMode gameMode, eMapType mapType, eTankType tankType, string lobbyName, string internalLobbyCode, bool isPrivateLobby, bool isHost)
    {
        GameMode = gameMode;
        SelectedMapType = mapType;
        SelectedTank = tankType;
        LobbyName = lobbyName;
        IsPrivateLobby = isPrivateLobby;
        InternalLobbyCode = internalLobbyCode;
        IsHost = isHost;
    }
}
