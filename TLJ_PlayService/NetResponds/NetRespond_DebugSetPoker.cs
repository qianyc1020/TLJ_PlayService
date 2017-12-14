using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;

class NetRespond_DebugSetPoker
{
    public static void doAskCilentReq_DebugSetPoker(IntPtr connId, string reqData)
    {
        try
        {
            JObject jo = JObject.Parse(reqData);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            // 逻辑处理
            {
                JArray ja = (JArray)JsonConvert.DeserializeObject(jo.GetValue("pokerList").ToString());
                List<TLJCommon.PokerInfo> pokerLst = new List<TLJCommon.PokerInfo>();
                for (int m = 0; m < ja.Count; m++)
                {
                    int num = Convert.ToInt32(ja[m]["num"]);
                    int pokerType = Convert.ToInt32(ja[m]["pokerType"]);

                    pokerLst.Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));
                }

                CustomPoker.setPlayerCustomPokerInfo(uid, pokerLst);
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("NetRespond_DebugSetPoker.doAskCilentReq_DebugSetPoker----" + ex.Message);

            //// 发送给客户端
            //PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
        }
    }
}