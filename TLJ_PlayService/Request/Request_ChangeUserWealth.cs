using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;

class Request_ChangeUserWealth
{
    public static void doRequest(string uid,int reward_id,int reward_num,string reason)
    {
        try
        {
            JObject respondJO = new JObject();

            respondJO.Add("tag", TLJCommon.Consts.Tag_ChangeUserWealth);
            
            respondJO.Add("account", "admin");
            respondJO.Add("password", "jinyou123");

            respondJO.Add("uid", uid);
            respondJO.Add("reward_id", reward_id);
            respondJO.Add("reward_num", reward_num);
            respondJO.Add("reason", reason);

            LogUtil.getInstance().addDebugLog("Request_ChangeUserWealth----改变玩家财富：" + uid +  "  "+ reward_id+"  "+ reward_num + "  " + reason);

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
            TLJ_PlayService.PlayService.log.Error("Request_ChangeUserWealth----" + ex.Message);
        }
    }
}