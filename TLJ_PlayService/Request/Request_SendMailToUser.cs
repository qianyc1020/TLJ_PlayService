using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;

class Request_SendMailToUser
{
    public static void doRequest(string uid, string title, string content,string reward)
    {
        try
        {
            JObject respondJO = new JObject();

            respondJO.Add("tag", TLJCommon.Consts.Tag_SendMailToUser);
            
            respondJO.Add("account", "admin");
            respondJO.Add("password", "jinyou123");

            respondJO.Add("uid", uid);
            respondJO.Add("title", title);
            respondJO.Add("content", content);
            respondJO.Add("reward", reward);

            LogUtil.getInstance().addDebugLog("Request_SendMailToUser----给玩家发邮件：" + uid +  "  "+ title + "  "+ content + "  " + reward);

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
            LogUtil.getInstance().addErrorLog("Request_SendMailToUser----" + ex.Message);
        }
    }
}