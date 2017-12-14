using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;

class NetRespond_UseBuff
{
    public static string doAskCilentReq_UseBuff(IntPtr connId, string reqData)
    {
        JObject respondJO = new JObject();

        try
        {
            JObject jo = JObject.Parse(reqData);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            respondJO.Add("tag", tag);

            // 传给数据库服务器
            {
                JObject temp = new JObject();
                temp.Add("tag", tag);
                temp.Add("connId", connId.ToInt32());

                temp.Add("uid", uid);
                temp.Add("prop_id", (int)jo.GetValue("prop_id"));

                if (!PlayService.m_mySqlServerUtil.sendMseeage(temp.ToString()))
                {
                    // 连接不上数据库服务器，通知客户端
                    {
                        respondJO.Add("code", Convert.ToInt32(TLJCommon.Consts.Code.Code_MySqlError));

                        // 发送给客户端
                        PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // 客户端参数错误
            respondJO.Add("code", Convert.ToInt32(TLJCommon.Consts.Code.Code_ParamError));

            // 发送给客户端
            PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
        }

        //return respondJO.ToString();
        return "";
    }

    public static void onMySqlRespond(int connId, string respondData)
    {
        try
        {
            {
                JObject jo = JObject.Parse(respondData);
                int code = (int)jo.GetValue("code");

                if (code == (int)TLJCommon.Consts.Code.Code_OK)
                {
                    string uid = jo.GetValue("uid").ToString();
                    int prop_id = (int)jo.GetValue("prop_id");

                    // 记牌器
                    if (prop_id == (int)TLJCommon.Consts.Prop.Prop_jipaiqi)
                    {
                        PlayerData playerData = GameUtil.getPlayerDataByUid(uid);
                        if (playerData != null)
                        {
                            playerData.m_isUseJiPaiQi = true;
                        }
                    }
                }
            }

            // 发送给客户端
            {
                PlayService.m_serverUtil.sendMessage((IntPtr)connId, respondData);
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addDebugLog(ex.Message);

            // 客户端参数错误
            //respondJO.Add("code", Convert.ToInt32(TLJCommon.Consts.Code.Code_ParamError));

            // 发送给客户端
            //LogicService.m_serverUtil.sendMessage(connId, respondJO.ToString());
        }
    }
}