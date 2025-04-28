using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum EGameMode
{
    Mode1vs1,
    Mode2vs2
}

public class LobbyManager : MonoBehaviour
{
    [Header("메뉴")]
    public GameObject startPanel;
    public GameObject infoPanel;
    public GameObject optionPanel;
    public Button startButton;
    public Button infoButton;
    public Button optionButton;
    public Button exitButton;

    [Header("로비")]
    public StartLobbyUI startLobbyUI;
    public CreateLobbyUI createLobbyUI;
    public SortLobbyUI sortLobbyUI;
    public MainLobbyUI mainLobbyUI;
    public GameObject loadingUI;
    public string gameSceneName;

    public static LobbyManager Instance { get; private set; }

    public List<LobbyData> PublicLobbyDatas { get; private set; } = new();
    public List<PlayerData> LobbyPlayerDatas { get; private set; } = new();

    public bool IsLobbyHost => _joinedLobby != null && (_joinedLobby.HostId == AuthenticationService.Instance.PlayerId);

    public eMapType selectedMapType = eMapType.Random;
    public eTankType selectedTankType = eTankType.Random;

    Lobby _joinedLobby;

    string _playerName;
    string _playerNameDataKey = "PlayerName";
    string _playerReadyDataKey = "PlayerReady";
    string _playerSelectTankDataKey = "PlayerSelectTank";       // 선택한 탱크
    string _gameModeDataKey = "GameMode";
    string _gameStartDataKey = "GameStart";
    string _gameMapDataKey = "SelectMap";                       // 선택한 맵
    string _lobbyNotFoundError = "lobby not found";
    float _maintainLobbyTime = 20;
    float _checkLobbyTime = 1.1f;
    bool _isGameStart;

    void Awake()
    {
        Instance = this;
    }

    async void Start()
    {
        _joinedLobby = null;
        _isGameStart = false;

        // 익명으로 유니티 로그인
        try
        {
            await UnityServices.InitializeAsync();
            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log($"Signed in: {AuthenticationService.Instance.PlayerId}.");
            };

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError(ex.Message);
        }

        // 플레이어 아이디 부여
        if (FirebaseManager._instance != null)
        {
            _playerName = FirebaseManager._instance.userVO.UserID;
        }
        else
        {
            _playerName = $"player {Random.Range(0, 100)}";
        }

        // 시작하기
        startButton.onClick.AddListener(() =>
        {
            startPanel.SetActive(true);
            infoPanel.SetActive(false);
            optionPanel.SetActive(false);
        });

        // 내 정보
        infoButton.onClick.AddListener(() =>
        {
            startPanel.SetActive(false);
            infoPanel.SetActive(true);
            optionPanel.SetActive(false);
        });

        // 옵션
        optionButton.onClick.AddListener(() =>
        {
            startPanel.SetActive(false);
            infoPanel.SetActive(false);
            optionPanel.SetActive(true);
        });

        // 종료
        exitButton.onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }

    public async void CreateLobby(EGameMode gameMode, string lobbyName, bool isPrivate)
    {
        try
        {
            // 로비 생성
            loadingUI.SetActive(true);
            _joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, NetworkPlayerData.SetMaxPlayer(gameMode), new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = GetPlayer(true),
                Data = new Dictionary<string, DataObject>
                {
                    // 게임 모드
                    { _gameModeDataKey, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString(), DataObject.IndexOptions.S1) },

                    // 로비 멤버에게만 게임 시작 여부 공개
                    { _gameStartDataKey, new DataObject(DataObject.VisibilityOptions.Member, "0") },

                    {_gameMapDataKey,  new DataObject(DataObject.VisibilityOptions.Member, eMapType.Random.ToString()) },
                }
            });

            mainLobbyUI.EnterMainLobbyUI(lobbyName, _joinedLobby.LobbyCode);
            ReadyPlayer(true);
            InvokeRepeating(nameof(MaintainLobby), _maintainLobbyTime, _maintainLobbyTime);
            InvokeRepeating(nameof(RefreshPlayers), _checkLobbyTime, _checkLobbyTime);
            Debug.Log($"Created Lobby: {lobbyName}, Code: {_joinedLobby.LobbyCode}.");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public async void RefreshPublicLobbies(EGameMode gameMode)
    {
        try
        {
            // 찾은 로비 표시
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
                {
                    // 빈 자리가 0 보다 큰 로비만 표시
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),

                    // 동일한 게임 모드만 표시
                    new QueryFilter(QueryFilter.FieldOptions.S1, gameMode.ToString(), QueryFilter.OpOptions.EQ)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Name)
                },
            });

            PublicLobbyDatas.Clear();
            for (int i = 0; i < queryResponse.Results.Count; i++)
            {
                // 로비 정보 저장
                Lobby lobby = queryResponse.Results[i];
                eMapType mayType = lobby.Data.ContainsKey(_gameMapDataKey) ? 
                    (eMapType)System.Enum.Parse(typeof(eMapType), lobby.Data[_gameMapDataKey].Value) : eMapType.Random;

                PublicLobbyDatas.Add(new LobbyData
                {
                    id = lobby.Id,
                    name = lobby.Name,
                    gameMode = (EGameMode)System.Enum.Parse(typeof(EGameMode), lobby.Data[_gameModeDataKey].Value),
                    selectedMap = mayType,
                });

                Debug.Log($"Lobbies found:{lobby.Name}, Mode: {lobby.Data[_gameModeDataKey].Value}.");
            }

            sortLobbyUI.RefreshLobbiesUI();
        }
        catch(LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public async void JoinLobby(string lobbyCode, int index)
    {
        try
        {
            loadingUI.SetActive(true);
            if (lobbyCode == null)
            {
                // 공개 로비 참가
                _joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(PublicLobbyDatas[index].id, new JoinLobbyByIdOptions
                {
                    Player = GetPlayer(false),
                });
            }
            else
            {
                // 비공개 로비 참가
                _joinedLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions
                {
                    Player = GetPlayer(false),
                });
            }

            NetworkPlayerData.SetMaxPlayer((EGameMode)System.Enum.Parse(typeof(EGameMode), _joinedLobby.Data[_gameModeDataKey].Value));
            PublicLobbyDatas.Clear();
            mainLobbyUI.EnterMainLobbyUI(_joinedLobby.Name, _joinedLobby.LobbyCode);
            InvokeRepeating(nameof(RefreshPlayers), _checkLobbyTime, _checkLobbyTime);
            Debug.Log($"Joined lobby: {_joinedLobby.Name}.");
        }
        catch (LobbyServiceException ex)
        {
            if (ex.Message.Equals(_lobbyNotFoundError))
            {
                Debug.Log(ex.Message);
            }
            else
            {
                Debug.LogError(ex.Message);
            }

            sortLobbyUI.ShowJoinFailedUI();
            _joinedLobby = null;
        }
    }

    public async void LeaveLobby()
    {
        loadingUI.SetActive(true);
        CancelInvoke(nameof(RefreshPlayers));
        if (IsLobbyHost)
        {
            // 호스트가 나갈 경우 로비 삭제
            DeleteLobby();
            Debug.Log("Host left the lobby.");
        }
        else
        {
            // 아니면 로비에서 본인 제거
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                Debug.Log($"{_playerName} left the lobby.");
            }
            catch (LobbyServiceException ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        _joinedLobby = null;
        loadingUI.SetActive(false);
        mainLobbyUI.LeaveMainLobbyUI();
    }

    public async void MaintainLobby()
    {
        // 30초가 지나면 로비가 사라지므로 지속적으로 갱신
        if (_joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(_joinedLobby.Id);
            }
            catch (LobbyServiceException ex)
            {
                Debug.LogError(ex.Message);
            }
        }
    }

    async void DeleteLobby()
    {
        // 로비 삭제
        try
        {
            CancelInvoke(nameof(MaintainLobby));
            await LobbyService.Instance.DeleteLobbyAsync(_joinedLobby.Id);
            Debug.Log("Lobby deleted.");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public async void KickPlayer(int index)
    {
        // 플레이어 추방
        try
        {
            Unity.Services.Lobbies.Models.Player player = _joinedLobby.Players[index];
            if (player.Id != _joinedLobby.HostId)
            {
                loadingUI.SetActive(true);
                await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, player.Id);
                Debug.Log($"{player.Data[_playerNameDataKey].Value} kicked.");
            }
            else
            {
                // 호스트는 추방 불가
                Debug.Log($"{player.Data[_playerNameDataKey].Value} is host and can't be kicked.");
            }
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public async void ReadyPlayer(bool value)
    {
        // 준비
        try
        {
            _joinedLobby = await LobbyService.Instance.UpdatePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    // 로비 멤버에게만 준비 상태 공개
                    { _playerReadyDataKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, value ? "1" : "0") }
                }
            });

            Debug.Log($"Ready: {value}");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    // 탱크 바꾼 상태 보여주기
    public async void ChangeTank(eTankType value)
    {
        // 준비
        try
        {
            _joinedLobby = await LobbyService.Instance.UpdatePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    // 로비 멤버에게만 탱크 상태 보여주기
                    { _playerSelectTankDataKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, value.ToString()) }
                }
            });

            Debug.Log($"SelectTank : {value}");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    // 맵 변경 보여주기
    public async void ChangeMap(eMapType mapType)
    {
        if (!IsLobbyHost || _joinedLobby == null)
            return;

        try
        {
            _joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { _gameMapDataKey, new DataObject(DataObject.VisibilityOptions.Member, mapType.ToString()) }
                }
            });

            selectedMapType = mapType;
            Debug.Log($"Map changed to: {mapType}");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public async void StartGameAsHost()
    {
        // 호스트로 게임 시작
        try
        {
            _isGameStart = true;
            loadingUI.SetActive(true);
            string relayCode = await CreateRelay();
            _joinedLobby = await Lobbies.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions
            {
                // 게임 시작 알림
                Data = new Dictionary<string, DataObject>
                {
                    // 로비 멤버에게만 공개
                    { _gameStartDataKey, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                }
            });

            CancelInvoke(nameof(MaintainLobby));
            CancelInvoke(nameof(RefreshPlayers));
            Invoke(nameof(LoadGameScene), _checkLobbyTime);
            NetworkPlayerData.SetGameInfo(selectedMapType, selectedTankType);
            _joinedLobby = null;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public bool IsPlayer(int index)
    {
        // 해당 플레이어가 본인인지 확인
        return _joinedLobby.Players[index].Id == AuthenticationService.Instance.PlayerId;
    }

    async void RefreshPlayers()
    {
        // 로비 갱신
        try
        {
            _joinedLobby = await LobbyService.Instance.GetLobbyAsync(_joinedLobby.Id);

            // 맵 갱신
            if (_joinedLobby.Data.TryGetValue(_gameMapDataKey, out var mapData))
            {
                if (!IsLobbyHost) // 호스트가 아닌경우에만 맵 변경 보여주기
                {
                    eMapType selectedMap = (eMapType)System.Enum.Parse(typeof(eMapType), mapData.Value);
                    mainLobbyUI.ShowSelectedMap(selectedMap);
                }
            }
        }
        catch (LobbyServiceException ex)
        {
            if (ex.Message.Equals(_lobbyNotFoundError))
            {
                // 로비가 없는 경우
                CancelInvoke(nameof(RefreshPlayers));
                mainLobbyUI.ShowSessionEndedUI();
                _joinedLobby = null;
                Debug.Log(ex.Message);
                return;
            }
            else
            {
                Debug.LogError(ex.Message);
            }
        }

        // 추방된 경우
        if (_joinedLobby.Players[0].Data == null)
        {
            CancelInvoke(nameof(RefreshPlayers));
            mainLobbyUI.ShowKickedUI();
            _joinedLobby = null;
            Debug.Log("You are kicked from the lobby.");
            return;
        }

        // 플레이어 데이터 갱신
        LobbyPlayerDatas.Clear();
        int playerIndex = 0;
        bool gameReady = true;
        for (int i = 0; i < _joinedLobby.Players.Count; i++)
        {
            // 이름과 준비 상태 갱신
            bool playerReady = _joinedLobby.Players[i].Data[_playerReadyDataKey].Value.Equals("1");

            // 선택한 탱크 보여주기
            eTankType selectedTank = (eTankType)System.Enum.Parse(typeof(eTankType) ,_joinedLobby.Players[i].Data[_playerSelectTankDataKey].Value);
            LobbyPlayerDatas.Add(new PlayerData
            {
                name = _joinedLobby.Players[i].Data[_playerNameDataKey].Value,
                ready = playerReady,
                selectedTank = selectedTank,
            });

            // 모든 플레이어가 준비되었는지 확인
            if (!playerReady || _joinedLobby.Players.Count < 2)
            {
                gameReady = false;
            }

            if (IsPlayer(i))
            {
                playerIndex = i;
            }
        }

        // 호스트가 아닐 경우
        if (!IsLobbyHost)
        {
            //게임이 시작되었는지 확인
            string joinCode = _joinedLobby.Data[_gameStartDataKey].Value;
            if (!joinCode.Equals("0"))
            {
                StartGameAsClient(joinCode);
            }
        }

        mainLobbyUI.RefreshPlayersUI(playerIndex, gameReady);
        loadingUI.SetActive(_isGameStart);
    }

    async Task<string> CreateRelay()
    {
        // 릴레이 생성
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(NetworkPlayerData.GetMaxPlayer() - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            RelayServerData relayServerData = new(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();
            Debug.Log("Game started as host.");
            return joinCode;
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError(ex.Message);
            return null;
        }
    }

    async void JoinRelay(string joinCode)
    {
        // 릴레이 참가
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            RelayServerData relayServerData = new(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
            Debug.Log("Game started as client.");
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    void StartGameAsClient(string joinCode)
    {
        // 클라이언트로 게임 시작
        _isGameStart = true;
        loadingUI.SetActive(true);
        JoinRelay(joinCode);
        CancelInvoke(nameof(RefreshPlayers));
        NetworkPlayerData.SetGameInfo(selectedMapType, selectedTankType);
        _joinedLobby = null;
    }

    void LoadGameScene()
    {
        // 게임 씬 로드
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    Unity.Services.Lobbies.Models.Player GetPlayer(bool isHost)
    {
        return new Unity.Services.Lobbies.Models.Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                // 로비 멤버에게만 플레이어 이름 공개
                { _playerNameDataKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, _playerName) },

                // 로비 멤버에게만 준비 상태 공개
                { _playerReadyDataKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, isHost? "1" : "0") },

                // 선택한 탱크 보여주기
                {_playerSelectTankDataKey,  new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, eTankType.Random.ToString()) },
            },
        };
    }

    public class LobbyData
    {
        public string id;
        public string name;
        public EGameMode gameMode;

        // 맵선택
        public eMapType selectedMap;
    }

    public class PlayerData
    {
        public string name;
        public bool ready;

        // 탱크 선택
        public eTankType selectedTank;
    }
}
