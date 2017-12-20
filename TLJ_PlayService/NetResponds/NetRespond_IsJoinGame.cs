using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;

class NetRespond_IsJoinGame
{
    public static void doAskCilentReq_IsJoinGame(IntPtr connId, string reqData)
    {
        JObject respondJO = new JObject();

        try
        {
            JObject jo = JObject.Parse(reqData);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            // 逻辑处理
            {
                respondJO.Add("tag", tag);
                respondJO.Add("isJoinGame", GameUtil.checkPlayerIsInRoom(uid) ? 1 : 0);

                if (GameUtil.checkPlayerIsInRoom(uid))
                {
                    respondJO.Add("gameRoomType", GameUtil.getRoomByUid(uid).m_gameRoomType);
                }

                // 发送给客户端
                PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error("NetRespond_IsJoinGame----" + ex.Message);

            // 客户端参数错误
            respondJO.Add("code", Convert.ToInt32(TLJCommon.Consts.Code.Code_ParamError));

            //// 发送给客户端
            //PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
        }
    }
}