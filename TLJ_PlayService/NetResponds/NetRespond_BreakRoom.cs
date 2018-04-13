using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;

class NetRespond_BreakRoom
{
    public static string doAskCilentReq_BreakRoom(IntPtr connId, string reqData)
    {
        try
        {
            JObject jo = JObject.Parse(reqData);
            string tag = jo.GetValue("tag").ToString();
            int roomID = (int)(jo.GetValue("roomID"));
            
            // 回复客户端
            {
                JObject respondJO = new JObject();
                respondJO.Add("tag", tag);

                if (GameLogic.breakRoomByRoomID(roomID))
                {
                    respondJO.Add("code", Convert.ToInt32(TLJCommon.Consts.Code.Code_OK));
                }
                else
                {
                    respondJO.Add("code", Convert.ToInt32(TLJCommon.Consts.Code.Code_CommonFail));
                }

                // 发送给客户端
                PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
            }
        }
        catch (Exception ex)
        {
        }

        //return respondJO.ToString();
        return "";
    }
}