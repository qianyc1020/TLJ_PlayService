using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TLJ_PlayService;

class NetRespond_Login
{
    public static string doAskCilentReq_Login(IntPtr connId, string reqData)
    {
        JObject respondJO = new JObject();

        try
        {
            JObject jo = JObject.Parse(reqData);
            string tag = jo.GetValue("tag").ToString();
            respondJO.Add("tag", tag);

            int platform = Convert.ToInt32(jo.GetValue("platform"));
            switch (platform)
            {
                // 官方包
                case (int)TLJCommon.Consts.Platform.Platform_Official:
                    {
                        string account = jo.GetValue("account").ToString();
                        string password = jo.GetValue("password").ToString();

                        // 传给数据库服务器
                        {
                            JObject temp = new JObject();
                            temp.Add("tag", "Login");
                            temp.Add("connId", connId.ToInt32());

                            temp.Add("account", account);
                            temp.Add("password", password);

                            //for (int k = 0; k < 1000; k++)
                            {
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
                    }
                    break;

                // 未知渠道包
                default:
                    {

                    }
                    break;
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
        JObject jo = JObject.Parse(respondData);

        string tag = jo.GetValue("tag").ToString();
        int code = Convert.ToInt32(jo.GetValue("code"));
        
        JObject respondJO = new JObject();
        respondJO.Add("tag", tag);
        respondJO.Add("code", code);

        //string account = jo.GetValue("account").ToString();
        //string password = jo.GetValue("password").ToString();



        //JObject userInfo = new JObject();
        //{
        //    userInfo.Add("uid", "uid1234");
        //    userInfo.Add("name", "default name");
        //    userInfo.Add("goldNum", 101);
        //}

        //respondJO.Add("userInfo", userInfo);

        // 发送给客户端
        PlayService.m_serverUtil.sendMessage((IntPtr)connId, respondJO.ToString());
    }
}
