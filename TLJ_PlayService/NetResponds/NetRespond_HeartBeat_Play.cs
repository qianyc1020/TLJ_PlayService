using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;

class NetRespond_HeartBeat_Play
{
    public static string doAskCilentReq_HeartBeat_Play(IntPtr connId, string reqData)
    {
        JObject respondJO = new JObject();

        try
        {
            JObject jo = JObject.Parse(reqData);

            respondJO.Add("tag", jo.GetValue("tag").ToString());

            // 发送给客户端
            PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
        }
        catch (Exception ex)
        {
        }

        //return respondJO.ToString();
        return "";
    }
}