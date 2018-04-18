using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;

class NetRespond_RetryJoinGame
{
    public static void doAskCilentReq_RetryJoinGame(IntPtr connId, string reqData)
    {
        JObject respondJO = new JObject();

        try
        {
            JObject jo = JObject.Parse(reqData);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            // 逻辑处理
            {
                // 先去升级里面找
                if (GameUtil.checkPlayerIsInRoom(uid))
                {
                    GameLogic.doTask_RetryJoinGame(connId, reqData);
                }
                // 再去斗地主里面找
                else if (DDZ_GameUtil.checkPlayerIsInRoom(uid))
                {
                    DDZ_GameLogic.doTask_RetryJoinGame(connId, reqData);
                }
            }
        }
        catch (Exception ex)
        {
        }
    }
}