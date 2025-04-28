using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using Newtonsoft;
using Newtonsoft.Json;
using UnityEngine.Events;
using System.Linq;



public enum ERROR_State
{
    NONE,
    DonSearch_ID,
    DonSearch_PW
}


public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager _instance;
    public UserData userVO;
    private DatabaseReference token;

    [Space(5)]
    public ERROR_State eState;

    [Space(10)]
    public UnityEvent loginCallback;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    async void Start()
    {
        await InitializeFirebase();
    }

    async Task InitializeFirebase()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;

            // 🔥 Database URL을 여기에 추가해야 함!
            string databaseUrl = "https://canon-wars-41ce5-default-rtdb.firebaseio.com/";
            token = FirebaseDatabase.GetInstance(app, databaseUrl).RootReference;

            Debug.Log("Firebase Initialized Successfully!");
        }
        else
        {
            Debug.LogError($"Firebase Initialization Failed: {dependencyStatus}");
        }
    }

    //계정 생성 기능
    public void Create_UserAccount(string a_UserID, string a_UserPW, string a_UserNickName)
    {

        UserData userData = new UserData();
        userData.UserID = a_UserID;
        userData.UserPW = a_UserPW;
        userData.NickName = a_UserNickName;
        userData.UID = DateTime.Now.ToString("yyyyMMddHHmmss");

        ///---- User 테이블에 user 정보 저장.
        {
            string userTable_path = "UserDataSeat/" + userData.UserID;  // 데이터베이스에서 저장할 위치 설정

            // Firebase에 데이터 업로드 (UserID, UserPW, UID)
            token.Child(userTable_path).SetRawJsonValueAsync(JsonConvert.SerializeObject(userData)).ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("User data uploaded successfully.");
                }
                else
                {
                    Debug.LogError("Error uploading user data.");
                }
            });
        }
        ///-----------------------------------------

        ///---- User의 보유 Cannon Table에 User의 정보 생성.
        {
            string cannonTable_path = "UserCannonSeat/" + userData.UID;

            ///firebase에는 null 값으로 data를 저장할 수 없음. 따라서 임시 0번째의 값을 빈 값으로 객체를 직렬화하여 seat를 초기화함.
            UserCannon userCannon = new UserCannon();
            userCannon.CannonKeys = new List<string>();
            userCannon.CannonKeys.Add("");

            token.Child(cannonTable_path).SetRawJsonValueAsync(JsonConvert.SerializeObject(userCannon)).ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("User data uploaded successfully.");
                }
                else
                {
                    Debug.LogError("Error uploading user data.");
                }
            });
        }
        ///-------------------------------------------

        ///---- User의 전적 관리 Table에 User 정보 생성.
        {
            string battleInfoTable_path = "UserBattleInfoSeat/" + userData.UID;

            ///firebase에는 null 값으로 data를 저장할 수 없음. 따라서 임시 0번째의 값을 빈 값으로 객체를 직렬화하여 seat를 초기화함.
            Wrapper info = new Wrapper();
            info.BattleDatas = new List<UserBattleInfo>
            {
                new UserBattleInfo()
                {
                    date = "",
                    result = ""
                }
            };

            string json = JsonConvert.SerializeObject(info);

            token.Child(battleInfoTable_path).SetRawJsonValueAsync(json).ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("User data uploaded successfully.");
                }
                else
                {
                    Debug.LogError("Error uploading user data.");
                }
            });
        }

        ///------------------------------------------

    }

    //로그인 기능
    public async void Login(string a_UserID, string a_UserPW, Action error_callback, Action success_callback)
    {
        //USER SEAT에서 유저 정보 찾기.
        {
            UserData tempUserData = new UserData();
            string path = "UserDataSeat";

            await token.Child(path).OrderByKey().EqualTo(a_UserID).GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;

                    if (snapshot.Exists)
                    {
                        string value = snapshot.Child(a_UserID).GetRawJsonValue();

                        if (value == null)
                        {
                            eState = ERROR_State.DonSearch_ID; //ID 없음.
                            error_callback.Invoke();
                            return;
                        }

                        tempUserData = JsonConvert.DeserializeObject<UserData>(value);
                    }
                }
            });



            if (tempUserData.UserPW != a_UserPW)
            {
                eState = ERROR_State.DonSearch_PW; //PW 불일치.
                error_callback.Invoke();
                return;
            }

            //voSet
            userVO.UID = tempUserData.UID;
            userVO.UserID = tempUserData.UserID;
            userVO.UserPW = tempUserData.UserPW;
            userVO.NickName = tempUserData.NickName;
        }

        //user battleinfoseat에서 유저 정보 찾기.
        {
            string path = "UserBattleInfoSeat";
            // 특정 uid가 있는 데이터를 찾음
            var query = token.Child(path).OrderByKey().EqualTo(userVO.UID);

            await query.GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;

                    if (snapshot.Exists)
                    {
                        // uid의 BattleDatas를 가져옴
                        foreach (var child in snapshot.Children)
                        {
                            string battleDataJson = child.Child("BattleDatas").GetRawJsonValue();
                            userVO.BattleInfos = JsonConvert.DeserializeObject<List<UserBattleInfo>>(battleDataJson);
                        }
                    }
                }
            });
        }

        //user CannonSeat에서 유저 정보 찾기.
        {
            string path = "UserCannonSeat";
            // 특정 uid가 있는 데이터를 찾음
            var query = token.Child(path).OrderByKey().EqualTo(userVO.UID);

            await query.GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;

                    if (snapshot.Exists)
                    {
                        // uid의 BattleDatas를 가져옴
                        foreach (var child in snapshot.Children)
                        {
                            string CannonDataJson = child.Child("CannonKeys").GetRawJsonValue();
                            userVO.CannonInfos.CannonKeys = JsonConvert.DeserializeObject<List<string>>(CannonDataJson);
                        }
                    }
                }
            });
        }

        eState = ERROR_State.NONE;
        if (success_callback != null)
            success_callback.Invoke();
    }


    //비밀번호 찾기 기능
    public async void Get_UserPW(string a_UserID, Action callback)
    {
        //USER SEAT에서 유저 정보 찾기.
        {
            UserData tempUserData = new UserData();
            string path = "UserDataSeat";

            await token.Child(path).OrderByKey().EqualTo(a_UserID).GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;

                    if (snapshot.Exists)
                    {
                        string value = snapshot.Child(a_UserID).GetRawJsonValue();

                        if (value == null)
                        {
                            return;
                        }

                        tempUserData = JsonConvert.DeserializeObject<UserData>(value);
                    }
                }
            });

            userVO.UID = tempUserData.UID;
            userVO.UserID = tempUserData.UserID;
            userVO.UserPW = tempUserData.UserPW;

        }

        if (callback != null)
            callback.Invoke();
    }


    //비밀번호 변경 기능
    async void Update_UserPW(string a_newUserPW, Action callback)
    {
        if (userVO == null)
            return;

        if (userVO.UserID == string.Empty)
            return;

        //USER SEAT에서 유저 정보 찾기.
        {
            UserData tempUserData = new UserData();
            string path = "UserDataSeat";

            await token.Child(path).OrderByKey().EqualTo(userVO.UserID).GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;

                    if (snapshot.Exists)
                    {
                        string value = snapshot.Child(userVO.UserID).GetRawJsonValue();

                        if (value == null)
                        {
                            eState = ERROR_State.DonSearch_ID; //ID 없음.
                            return;
                        }

                        tempUserData = JsonConvert.DeserializeObject<UserData>(value);
                    }
                }
            });

            userVO.UID = tempUserData.UID;
            userVO.UserID = tempUserData.UserID;
            userVO.UserPW = a_newUserPW;

        }


        {
            string userTable_path = "UserDataSeat/" + userVO.UserID;  // 데이터베이스에서 저장할 위치 설정

            // Firebase에 데이터 업로드 (UserID, UserPW, UID)
            await token.Child(userTable_path).SetRawJsonValueAsync(JsonConvert.SerializeObject(userVO)).ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("User data uploaded successfully.");
                }
                else
                {
                    Debug.LogError("Error uploading user data.");
                }
            });
        }

        if (callback != null)
            callback.Invoke();
    }


    //VO 내의 Cannon List 를 Database에 저장.
    public void Update_UserCannon()
    {
        {
            string cannonTable_path = "UserCannonSeat/" + userVO.UID;

            UserCannon userCannon = new UserCannon();

            userCannon.CannonKeys = userVO.CannonInfos.CannonKeys.ToList();

            for (int i = 0; i < userCannon.CannonKeys.Count;)
            {
                if (userCannon.CannonKeys[i] == string.Empty)
                {
                    userCannon.CannonKeys.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            token.Child(cannonTable_path).SetRawJsonValueAsync(JsonConvert.SerializeObject(userCannon)).ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("User data uploaded successfully.");
                }
                else
                {
                    Debug.LogError("Error uploading user data.");
                }
            });
        }
    }

    //VO 내의 Battle List 를 Database에 저장.
    public void Update_UserBattleInfo()
    {
        string battleInfoTable_path = "UserBattleInfoSeat/" + userVO.UID;

        ///firebase에는 null 값으로 data를 저장할 수 없음. 따라서 임시 0번째의 값을 빈 값으로 객체를 직렬화하여 seat를 초기화함.
        Wrapper info = new Wrapper();
        info.BattleDatas = userVO.BattleInfos.ToList();

        for (int i = 0; i < info.BattleDatas.Count;)
        {
            if (info.BattleDatas[i].date == string.Empty)
            {
                info.BattleDatas.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }


        string json = JsonConvert.SerializeObject(info);

        token.Child(battleInfoTable_path).SetRawJsonValueAsync(json).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("User data uploaded successfully.");
            }
            else
            {
                Debug.LogError("Error uploading user data.");
            }
        });
    }



    /// <summary>
    /// VO 내의 Cannon Key List에 value를 add 함.
    /// </summary>
    /// <param name="value">Cannon Key</param>
    public void addCannonnKeys(string value)
    {
        userVO.CannonInfos.CannonKeys.Add(value);
    }

    /// <summary>
    /// VO 내의 BattleInfo List에 value를 add함.
    /// date는 add 할 때 Now DateTime으로 데이터를 자동으로 넣어줌.
    /// </summary>
    /// <param name="value">경기 결과 여부</param>
    public void addBattleInnfo(string value)
    {
        if (userVO.BattleInfos == null)
        {
            userVO.BattleInfos = new List<UserBattleInfo>();
        }

        userVO.BattleInfos.Add(new UserBattleInfo
        {
            date = DateTime.Now.ToString("yyyy-MM-dd"),
            result = value
        });
    }
}


// UserBattleInfo 배열을 래핑할 클래스
// DB Seat 내에 데이터가 null 상태로 초기화할 수 없기 때문에 객체를 하나 추가하여서 초기화용으로 사용되는 class.
[System.Serializable]
public class Wrapper
{
    public List<UserBattleInfo> BattleDatas;
}
