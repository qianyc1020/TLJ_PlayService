using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;

class Request_OnlineStatistics
{
    public enum OnlineStatisticsType
    {
        OnlineStatisticsType_Join = 1,
        OnlineStatisticsType_exit,
        OnlineStatisticsType_clear,
    }

    public static void doRequest(string uid,int room_id, string gameroomtype,bool isAI,int type)
    {
        try
        {
            JObject respondJO = new JObject();

            respondJO.Add("tag", TLJCommon.Consts.Tag_OnlineStatistics);
            respondJO.Add("uid", uid);
            respondJO.Add("room_id", room_id);
            respondJO.Add("gameroomtype", gameroomtype);
            respondJO.Add("isAI", isAI);
            respondJO.Add("type", type);

            // 传给数据库服务器
            {
                if (!PlayService.m_mySqlServerUtil.sendMseeage(respondJO.ToString()))
                {
                    // 连接不上数据库服务器
                }
            }
        }
        catch (Exception ex)
        {
            // 客户端参数错误
            TLJ_PlayService.PlayService.log.Error("Request_OnlineStatistics----" + ex.Message);
        }
    }
}