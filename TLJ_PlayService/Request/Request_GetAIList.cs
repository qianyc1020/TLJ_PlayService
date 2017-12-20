using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;

class Request_GetAIList
{
    public static void doRequest()
    {
        try
        {
            JObject respondJO = new JObject();

            respondJO.Add("tag", TLJCommon.Consts.Tag_GetAIList);
            respondJO.Add("account", "admin");
            respondJO.Add("password", "jinyou123");

            LogUtil.getInstance().addDebugLog("Request_GetAIList----拉取机器人列表");

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
            TLJ_PlayService.PlayService.log.Error("Request_GetAIList.doRequest----" + ex.Message);
        }
    }

    public static void onMySqlRespond(string respondData)
    {
        try
        {
            AIDataScript.getInstance().initJson(respondData);
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error("Request_GetAIList.onMySqlRespond----" + ex.Message);

            // 客户端参数错误
            //respondJO.Add("code", Convert.ToInt32(TLJCommon.Consts.Code.Code_ParamError));

            // 发送给客户端
            //LogicService.m_serverUtil.sendMessage(connId, respondJO.ToString());
        }
    }
}