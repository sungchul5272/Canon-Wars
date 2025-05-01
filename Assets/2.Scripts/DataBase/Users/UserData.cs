using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class UserData 
{
    public string UserID;
    public string UserPW;
    public string UID;
    public string NickName;
    
    [NonSerialized] public string NowTank;

    public List<UserBattleInfo> BattleInfos;
    public UserCannon CannonInfos;
}
