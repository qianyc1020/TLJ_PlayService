using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;

class Request_UseBuff
{
    public static void doRequest(string uid,int prop_id)
    {
        try
        {
            JObject respondJO = new JObject();

            respondJO.Add("tag", TLJCommon.Consts.Tag_UseBuff);

            respondJO.Add("uid", uid);
            respondJO.Add("prop_id", prop_id);

            LogUtil.getInstance().addDebugLog("Request_UseBuff----玩家消耗buff：" + uid +  "  "+ prop_id);

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
            LogUtil.getInstance().addErrorLog("Request_ChangeUserWealth----" + ex.Message);
        }
    }
}