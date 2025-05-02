using UnityEngine;

public static class NetworkPlayerData
{
    public static EGameMode GameMode { get; private set; }
    public static eMapType SelectedMapType { get; private set; } = eMapType.Random;
    public static eTankType SelectedTank { get; private set; }

    public static string LobbyName { get; private set; }
    public static string InternalLobbyCode { get; private set; }
    public static bool IsPrivateLobby { get; private set; }

    public static int GetMaxPlayer()
    {
        int maxPlayer = 2 * ((int)GameMode + 1);
        return maxPlayer;
    }

    public static int SetMaxPlayer(EGameMode gameMode)
    {
        int maxPlayer = 2 * ((int)gameMode + 1);
        return maxPlayer;
    }

    public static void SetGameInfo(EGameMode gameMode, eMapType mapType, eTankType tankType, string lobbyName, string internalLobbyCode, bool isPrivateLobby)
    {
        GameMode = gameMode;
        SelectedMapType = mapType;
        SelectedTank = tankType;
        LobbyName = lobbyName;
        IsPrivateLobby = isPrivateLobby;
        InternalLobbyCode = internalLobbyCode;
    }
}
