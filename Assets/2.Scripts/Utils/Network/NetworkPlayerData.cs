using UnityEngine;

public static class NetworkPlayerData
{
    public static EGameMode GameMode { get; private set; } = EGameMode.Mode1vs1;
    public static eMapType SelectedMapType { get; private set; } = eMapType.Random;
    public static eTankType SelectedTank { get; private set; } = eTankType.Random;

    public static string LobbyName { get; private set; } = string.Empty;
    public static string InternalLobbyCode { get; private set; } = string.Empty;
    public static bool IsPrivateLobby { get; private set; } = false;
    public static bool IsHost { get; private set; } = false;
    public static bool IsGameAborted { get; private set; } = false;

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

    public static void SetGameInfo(EGameMode gameMode, eMapType mapType, eTankType tankType,
        string lobbyName, string internalLobbyCode, bool isPrivateLobby, bool isHost)
    {
        GameMode = gameMode;
        SelectedMapType = mapType;
        SelectedTank = tankType;
        LobbyName = lobbyName;
        InternalLobbyCode = internalLobbyCode;
        IsPrivateLobby = isPrivateLobby;
        IsHost = isHost;
    }

    public static void RemoveGameInfo()
    {
        GameMode = EGameMode.Mode1vs1;
        SelectedMapType = eMapType.Random;
        SelectedTank = eTankType.Random;
        LobbyName = string.Empty;
        InternalLobbyCode = string.Empty;
        IsPrivateLobby = false;
        IsHost = false;
        IsGameAborted = false;
    }

    public static void GameAborted()
    {
        IsGameAborted = true;
    }
}
