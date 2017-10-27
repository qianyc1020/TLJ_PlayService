using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;

class NetRespond_GetPVPGameRoom
{
    public static string doAskCilentReq_GetPVPGameRoom(IntPtr connId, string reqData)
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
            // 发送给客户端
            {
                //PlayService.m_serverUtil.sendMessage((IntPtr)connId, respondData);
                
                JObject jo = JObject.Parse(respondData);
                JArray room_list = (JArray)JsonConvert.DeserializeObject(jo.GetValue("room_list").ToString());

                {
                    JObject respondJO = new JObject();
                    respondJO.Add("tag", jo.GetValue("tag"));
                    respondJO.Add("code", jo.GetValue("code"));

                    {
                        JArray ja = new JArray();
                        respondJO.Add("room_list", ja);

                        for (int i = 0; i < room_list.Count; i++)
                        {
                            JObject temp = new JObject();

                            temp.Add("id", room_list[i]["id"]);
                            temp.Add("gameroomtype", room_list[i]["gameroomtype"]);
                            temp.Add("gameroomname", room_list[i]["gameroomname"]);
                            temp.Add("kaisairenshu", room_list[i]["kaisairenshu"]);
                            temp.Add("baomingfei", room_list[i]["baomingfei"]);
                            temp.Add("reward", room_list[i]["reward"]);

                            temp.Add("baomingrenshu", GameUtil.getGameRoomPlayerCount(room_list[i]["gameroomtype"].ToString()));

                            ja.Add(temp);
                        }
                    }

                    PlayService.m_serverUtil.sendMessage((IntPtr)connId, respondJO.ToString());
                }
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

public class PVPGameRoomData
{
    public int id;
    public string gameroomtype;
    public string gameroomname;
    public int kaisairenshu;
    public string baomingfei;
    public string reward;
}