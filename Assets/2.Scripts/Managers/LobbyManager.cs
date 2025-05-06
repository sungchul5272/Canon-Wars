using System.Collections;
using System.Collections.Generic;
using System.Text;
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

    public static LobbyManager Instance { get; private set; }

    public List<LobbyData> PublicLobbyDatas { get; private set; } = new();
    public List<PlayerData> LobbyPlayerDatas { get; private set; } = new();

    public bool IsLobbyHost => _joinedLobby != null && (_joinedLobby.HostId == AuthenticationService.Instance.PlayerId);

    public eMapType SelectedMapType { get; set; } = eMapType.Random;
    public eTankType SelectedTankType { get; set; } = eTankType.Random;

    Lobby _joinedLobby;
    Coroutine _autoNetworkShutdown;
    System.Random _random = new();
    QueryFilter.FieldOptions _privateLobbyFilter = QueryFilter.FieldOptions.S1;
    QueryFilter.FieldOptions _internalCodeFilter = QueryFilter.FieldOptions.S2;
    QueryFilter.FieldOptions _gameModeFilter = QueryFilter.FieldOptions.S3;

    readonly string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"; // 대문자 알파벳과 숫자

    string _playerName;
    string _playerNickName;
    string _gameSceneName = "3.IngameScene";
    string _playerNameDataKey = "PlayerName";
    string _playerReadyDataKey = "PlayerReady";
    string _playerSelectTankDataKey = "PlayerSelectTank";
    string _gameModeDataKey = "GameMode";
    string _gameStartDataKey = "GameStart";
    string _gameMapDataKey = "SelectMap";
    string _internalLobbyCodeDataKey = "InternalLobbyCode";
    string _privateLobbyDataKey = "PrivateLobby";
    string _lobbyNotFoundError = "lobby not found";
    float _maintainLobbyTime = 20;
    float _rateLimitTime = 1.1f;
    float _gameStartTimeout = 10;
    float _backToLobbyTimeout = 5;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 초기화
        startLobbyUI.Init();
        createLobbyUI.Init();
        sortLobbyUI.Init();
        mainLobbyUI.Init();
        _joinedLobby = null;

        SoundManager.Instance.PlayLobbySceneBGM();

        // 플레이어 아이디 부여
        _playerName = FirebaseManager._instance.userVO.UserID;
        _playerNickName = FirebaseManager._instance.userVO.NickName;

        // 유니티 서비스 로그인
        LogInUnityService();

        // 시작하기
        startButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlayButtonClick();
            startPanel.SetActive(true);
            infoPanel.SetActive(false);
            optionPanel.SetActive(false);
        });

        // 내 정보
        infoButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlayButtonClick();
            startPanel.SetActive(false);
            infoPanel.SetActive(true);
            optionPanel.SetActive(false);
        });

        // 옵션
        optionButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlayButtonClick();
            startPanel.SetActive(false);
            infoPanel.SetActive(false);
            optionPanel.SetActive(true);
        });

        // 종료
        exitButton.onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            SoundManager.Instance.PlayButtonClick();
            UnityEditor.EditorApplication.isPlaying = false;
#else
            SoundManager.Instance.PlayButtonClick();
            Application.Quit();
#endif
        });
    }

    async void LogInUnityService()
    {
        // 익명으로 유니티 로그인
        try
        {
            await UnityServices.InitializeAsync();

            // 이미 로그인 된 경우 경기가 끝나고 돌아온 것으로 간주
            if (AuthenticationService.Instance.IsSignedIn)
            {
                if (NetworkPlayerData.InternalLobbyCode != string.Empty)//NetworkManager.Singleton.IsListening)
                {
                    Debug.Log("로비로 복귀합니다.");
                    SetSceneToMainLobby();
                }
                else
                {
                    // 연결이 끊긴 경우
                    Debug.Log("연결이 끊겼으므로 시작 메뉴로 이동합니다.");
                    if (NetworkPlayerData.IsGameAborted)
                    {
                        mainLobbyUI.ShowConnectionFailedUI();
                    }
                }

                return;
            }

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log($"익명 로그인: {AuthenticationService.Instance.PlayerId}.");
            };

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    async void SetSceneToMainLobby()
    {
        // 게임 시작 실패 후 복귀한 경우
        if (NetworkPlayerData.IsGameAborted)
        {
            mainLobbyUI.ShowConnectionFailedUI();
        }

        // 메인 로비로 복귀
        startPanel.SetActive(true);
        startLobbyUI.gameObject.SetActive(false);
        mainLobbyUI.gameObject.SetActive(true);
        loadingUI.SetActive(true);

        if (NetworkPlayerData.IsHost)
        {
            Debug.Log("복귀할 로비 재생성.");

            // 호스트는 로비 생성
            CreateLobby(NetworkPlayerData.GameMode, NetworkPlayerData.LobbyName, NetworkPlayerData.IsPrivateLobby, NetworkPlayerData.InternalLobbyCode);
        }
        else
        {
            Debug.Log("복귀할 로비 탐색.");

            // 이전 로비 탐색 방지를 위해 잠시 대기
            await Task.Delay((int)(_rateLimitTime * 1000));

            Lobby rejoinLobby = null;
            System.Diagnostics.Stopwatch stopwatch = new();
            stopwatch.Start();
            while (true)
            {
                try
                {
                    QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
                    {
                        Filters = new List<QueryFilter>
                        {
                            // 동일한(EQ) 내부 코드만 표시
                            new QueryFilter(_internalCodeFilter, NetworkPlayerData.InternalLobbyCode, QueryFilter.OpOptions.EQ)
                        },
                    });

                    int lobbyCount = queryResponse.Results.Count;
                    if (lobbyCount == 1)
                    {
                        Debug.Log("복귀할 로비 찾기 성공.");
                        rejoinLobby = queryResponse.Results[0];
                        break;
                    }
                    else if (lobbyCount > 1)
                    {
                        Debug.LogWarning("이전 로비가 남아있습니다.");
                    }

                    // 타임아웃 검사
                    if (stopwatch.Elapsed.TotalSeconds > _backToLobbyTimeout)
                    {
                        Debug.Log("로비 복귀 타임아웃");
                        ShowJoinFailed();
                        break;
                    }

                    Debug.Log("로비 탐색 재요청 대기.");
                    await Task.Delay((int)(_rateLimitTime * 1000)); // 로비 탐색 재요청 대기
                }
                catch (LobbyServiceException ex)
                {
                    Debug.LogError(ex.Message);
                }
            }

            stopwatch.Stop();
            if (rejoinLobby != null)
            {
                Debug.Log($"복귀할 로비 참가: {rejoinLobby.Name}, 내부 코드: {rejoinLobby.Data[_internalLobbyCodeDataKey].Value}");
                JoinLobby(string.Empty, -1, rejoinLobby.Id);
            }
        }
    }

    void ShowJoinFailed()
    {
        // 로비 탐색 화면으로 변경
        mainLobbyUI.gameObject.SetActive(false);
        sortLobbyUI.gameObject.SetActive(true);

        // 참가 실패 UI 활성화
        sortLobbyUI.ShowJoinFailedUI();
        _joinedLobby = null;
    }

    public async void CreateLobby(EGameMode gameMode, string lobbyName, bool isPrivate, string internalCode)
    {
        // 내부 코드가 없으면 새로 생성
        if (internalCode == string.Empty)
        {
            while (true)
            {
                string randomCode = GetRandomCode();
                try
                {
                    // 중복된 코드를 가진 로비가 있는지 확인
                    QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
                    {
                        Filters = new List<QueryFilter>
                        {
                            // 동일한(EQ) 내부 코드만 표시
                            new QueryFilter(_internalCodeFilter, randomCode, QueryFilter.OpOptions.EQ)
                        },
                    });

                    // 중복된 코드의 로비가 없으면 해당 코드로 결정
                    if (queryResponse.Results.Count <= 0)
                    {
                        Debug.Log("내부 코드 생성 완료.");
                        internalCode = randomCode;
                        break;
                    }
                    else
                    {
                        Debug.Log("중복된 내부 코드 로비 발견, 코드 재생성 시작.");
                    }

                    await Task.Delay((int)(_rateLimitTime * 1000)); // 요청 대기
                }
                catch (LobbyServiceException ex)
                {
                    Debug.LogError(ex.Message);
                }
            }
        }
        else // 내부 코드를 이용해 복귀하는 경우
        {
            try
            {
                // 이전 로비가 남아있는지 확인
                QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
                {
                    Filters = new List<QueryFilter>
                    {
                        // 동일한(EQ) 내부 코드만 표시
                        new QueryFilter(_internalCodeFilter, internalCode, QueryFilter.OpOptions.EQ)
                    },
                });

                // 이전 로비가 남아있으면 삭제
                if (queryResponse.Results.Count > 0)
                {
                    Debug.Log("이전 로비를 발견하여 삭제합니다.");
                    for (int i = 0; i < queryResponse.Results.Count; i++)
                    {
                        try
                        {
                            // 로비 삭제
                            await LobbyService.Instance.DeleteLobbyAsync(queryResponse.Results[i].Id);
                            Debug.Log("이전 로비 삭제됨.");
                        }
                        catch (LobbyServiceException ex)
                        {
                            Debug.LogError(ex.Message);
                        }
                    }
                }
            }
            catch (LobbyServiceException ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        // 로비 생성
        try
        {
            loadingUI.SetActive(true);
            string privateMode = isPrivate ? "1" : "0";
            _joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, NetworkPlayerData.SetMaxPlayer(gameMode), new CreateLobbyOptions
            {
                Player = GetPlayer(true),
                Data = new Dictionary<string, DataObject>
                {
                    // 로비 공개 여부
                    {_privateLobbyDataKey, new DataObject(DataObject.VisibilityOptions.Member, privateMode, DataObject.IndexOptions.S1) },

                    // 내부 커스텀 로비 코드 (로비 밖에서도 알 수 있도록 공개)
                    {_internalLobbyCodeDataKey, new DataObject(DataObject.VisibilityOptions.Public, internalCode, DataObject.IndexOptions.S2) },

                    // 게임 모드 (로비 밖에서도 알 수 있도록 공개)
                    { _gameModeDataKey, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString(), DataObject.IndexOptions.S3) },

                    // 맵 (기본 랜덤으로 설정)
                    {_gameMapDataKey,  new DataObject(DataObject.VisibilityOptions.Member, eMapType.Random.ToString()) },

                    // 게임 시작 여부
                    { _gameStartDataKey, new DataObject(DataObject.VisibilityOptions.Member, "0") }
                }
            });

            loadingUI.SetActive(false);
            PublicLobbyDatas.Clear();
            mainLobbyUI.EnterMainLobbyUI(lobbyName, _joinedLobby.LobbyCode);
            ReadyPlayer(true);
            InvokeRepeating(nameof(MaintainLobby), _maintainLobbyTime, _maintainLobbyTime);
            InvokeRepeating(nameof(RefreshPlayers), _rateLimitTime, _rateLimitTime);
            Debug.Log($"생성된 로비: {_joinedLobby.Name}, 공개 여부: {privateMode}, 코드: {_joinedLobby.LobbyCode}, 내부 코드: {_joinedLobby.Data[_internalLobbyCodeDataKey].Value}.");

            // 이전 로비 정보는 삭제
            NetworkPlayerData.RemoveGameInfo();
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    string GetRandomCode(int length = 6)
    {
        // 무작위 코드 생성
        StringBuilder stringBuilder = new(length);
        for (int i = 0; i < length; i++)
        {
            stringBuilder.Append(chars[_random.Next(chars.Length)]);
        }

        return stringBuilder.ToString();
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
            // 이름과 준비 상태 갱신 (호스트는 자동 레디)
            bool playerReady = (i == 0) || _joinedLobby.Players[i].Data[_playerReadyDataKey].Value.Equals("1");

            // 선택한 탱크 보여주기
            eTankType selectedTank = (eTankType)System.Enum.Parse(typeof(eTankType), _joinedLobby.Players[i].Data[_playerSelectTankDataKey].Value);
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
        bool isGameStarted = false;
        if (!IsLobbyHost)
        {
            //게임이 시작되었는지 확인
            string joinCode = _joinedLobby.Data[_gameStartDataKey].Value;
            if (!joinCode.Equals("0"))
            {
                StartGameAsClient(joinCode);
                isGameStarted = true;
            }
        }

        mainLobbyUI.RefreshPlayersUI(playerIndex, gameReady);
        if (!isGameStarted && !IsLobbyHost)
        {
            loadingUI.SetActive(false);
        }
    }

    public async void JoinLobby(string lobbyCode, int index, string rejoinId)
    {
        // 로비 참가
        try
        {
            // 복귀 로비가 아닌 경우
            if (rejoinId == string.Empty)
            {
                // 인터셉트 방지를 위해 입장 요청 대기
                await Task.Delay((int)(_rateLimitTime * 1000));
            }

            loadingUI.SetActive(true);
            if (lobbyCode == string.Empty)
            {
                // 공개 로비 참가
                string lobbyId = (rejoinId != string.Empty) ? rejoinId : ((index >= 0) ? PublicLobbyDatas[index].id : "0");
                _joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId, new JoinLobbyByIdOptions
                {
                    Player = GetPlayer(false),
                });
            }
            else
            {
                // 비공개 로비는 코드로 참가
                _joinedLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions
                {
                    Player = GetPlayer(false),
                });
            }

            PublicLobbyDatas.Clear();
            mainLobbyUI.EnterMainLobbyUI(_joinedLobby.Name, _joinedLobby.LobbyCode);
            InvokeRepeating(nameof(RefreshPlayers), _rateLimitTime, _rateLimitTime);
            Debug.Log($"참가한 로비: {_joinedLobby.Name}, 코드: {_joinedLobby.LobbyCode}, 내부 코드: {_joinedLobby.Data[_internalLobbyCodeDataKey].Value}.");

            // 이전 로비 정보는 삭제
            NetworkPlayerData.RemoveGameInfo();
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

            ShowJoinFailed();
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
        {
            return;
        }

        try
        {
            _joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { _gameMapDataKey, new DataObject(DataObject.VisibilityOptions.Member, mapType.ToString()) }
                }
            });

            SelectedMapType = mapType;
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
            CancelInvoke(nameof(RefreshPlayers));
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
            EGameMode gameMode = (EGameMode)System.Enum.Parse(typeof(EGameMode), _joinedLobby.Data[_gameModeDataKey].Value);
            string internalCode = _joinedLobby.Data[_internalLobbyCodeDataKey].Value;
            bool isPrivate = _joinedLobby.Data[_privateLobbyDataKey].Value.Equals("1");
            NetworkPlayerData.SetGameInfo(gameMode, SelectedMapType, SelectedTankType, _joinedLobby.Name, internalCode, isPrivate, true);
            _joinedLobby = null;

            // 게임 씬 로드
            StartCoroutine(LoadGameSceneAsync());
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    IEnumerator LoadGameSceneAsync()
    {
        Debug.Log("게임 씬을 로드합니다.");
        yield return new WaitForSeconds(_rateLimitTime);
        NetworkManager.Singleton.SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single);
    }

    async Task<string> CreateRelay()
    {
        // 릴레이 생성
        try
        {
            NetworkManager singleton = NetworkManager.Singleton;
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(NetworkPlayerData.GetMaxPlayer() - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            RelayServerData relayServerData = new(allocation, "dtls");
            singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            singleton.StartHost();
            Debug.Log("Game started as host.");
            return joinCode;
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError(ex.Message);
            ShowGameStartFailed();
            return "0";
        }
    }

    void ShowGameStartFailed()
    {
        mainLobbyUI.ShowGameStartFailedUI();
        loadingUI.SetActive(false);
    }

    public bool IsPlayer(int index)
    {
        // 해당 플레이어가 본인인지 확인
        return _joinedLobby.Players[index].Id == AuthenticationService.Instance.PlayerId;
    }

    void StartGameAsClient(string joinCode)
    {
        // 클라이언트로 게임 시작
        CancelInvoke(nameof(RefreshPlayers));
        loadingUI.SetActive(true);
        JoinRelay(joinCode);
        EGameMode gameMode = (EGameMode)System.Enum.Parse(typeof(EGameMode), _joinedLobby.Data[_gameModeDataKey].Value);
        string internalCode = _joinedLobby.Data[_internalLobbyCodeDataKey].Value;
        bool isPrivate = _joinedLobby.Data[_privateLobbyDataKey].Value.Equals("1");
        NetworkPlayerData.SetGameInfo(gameMode, SelectedMapType, SelectedTankType, _joinedLobby.Name, internalCode, isPrivate, false);
        _joinedLobby = null;
    }

    async void JoinRelay(string joinCode)
    {
        // 릴레이 참가
        try
        {
            NetworkManager singleton = NetworkManager.Singleton;
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            RelayServerData relayServerData = new(joinAllocation, "dtls");
            singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            singleton.StartClient();
            StartCoroutine(CheckConnectionAsync());
            Debug.Log("Game started as client.");
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError(ex.Message);
            ShowGameStartFailed();
        }
    }

    IEnumerator CheckConnectionAsync()
    {
        float timer = 0;
        while (true)
        {
            if (timer > _gameStartTimeout)
            {
                // 연결이 끊어진 경우
                Debug.Log("게임 씬으로 넘어가기 전 타임아웃 되었습니다.");
                NetworkManager.Singleton.Shutdown();
                mainLobbyUI.ShowConnectionFailedUI();
                mainLobbyUI.LeaveMainLobbyUI();
                loadingUI.SetActive(false);
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }
    }

    Unity.Services.Lobbies.Models.Player GetPlayer(bool isHost)
    {
        return new Unity.Services.Lobbies.Models.Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                // 로비 멤버에게만 플레이어 이름 공개
                { _playerNameDataKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, _playerNickName) },

                // 로비 멤버에게만 준비 상태 공개
                { _playerReadyDataKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, isHost? "1" : "0") },

                // 선택한 탱크 보여주기
                {_playerSelectTankDataKey,  new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, eTankType.Random.ToString()) },
            },
        };
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
                    // 공개 로비만 표시
                    new QueryFilter(_privateLobbyFilter, "0", QueryFilter.OpOptions.EQ),

                    // 동일한(EQ) 게임 모드만 표시
                    new QueryFilter(_gameModeFilter, gameMode.ToString(), QueryFilter.OpOptions.EQ),

                    // 빈 자리가 0 보다 큰(GT) 로비만 표시
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
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

                Debug.Log($"찾은 로비:{lobby.Name}, 게임 모드: {lobby.Data[_gameModeDataKey].Value}.");
            }

            sortLobbyUI.RefreshLobbiesUI();
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public class LobbyData
    {
        public string id; // 로비 고유 아이디
        public string name; // 로비 제목

        public EGameMode gameMode;
        public eMapType selectedMap;
    }

    public class PlayerData
    {
        public string name; // 닉네임
        public bool ready; // 준비 상태

        public eTankType selectedTank;
    }
}
