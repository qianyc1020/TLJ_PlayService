using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class UserInfo_Game_Manager
{
    static List<UserInfo_Game> s_dataList = new List<UserInfo_Game>();

    public static void addOneData(UserInfo_Game userInfo_Game)
    {
        lock (s_dataList)
        {
            for (int i = 0; i < s_dataList.Count; i++)
            {
                if (s_dataList[i].uid.CompareTo(userInfo_Game.uid) == 0)
                {
                    s_dataList[i] = userInfo_Game;

                    return;
                }
            }

            s_dataList.Add(userInfo_Game);
        }
    }

    public static UserInfo_Game getDataByUid(string uid)
    {
        UserInfo_Game userInfo_Game = null;

        for (int i = 0; i < s_dataList.Count; i++)
        {
            if (s_dataList[i].uid.CompareTo(uid) == 0)
            {
                userInfo_Game = s_dataList[i];
                break;
            }
        }

        return userInfo_Game;
    }
}

public class UserInfo_Game
{
    public string tag { get; set; }
    public int connId { get; set; }
    public string uid { get; set; }
    public int isClientReq { get; set; }
    public int code { get; set; }
    public string name { get; set; }
    public int vipLevel { get; set; }
    public int gold { get; set; }
    public int head { get; set; }
    public Gamedata gameData { get; set; }
    public List<UserBuff> BuffData { get; set; }
}

public class Gamedata
{
    public int allGameCount { get; set; }
    public int winCount { get; set; }
    public int runCount { get; set; }
    public int meiliZhi { get; set; }
}

public class UserBuff
{
    public int prop_id { get; set; }
    public int buff_num { get; set; }
}