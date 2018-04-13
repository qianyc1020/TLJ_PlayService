using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;

class Request_UserInfo_Game
{
    public static void doRequest(string uid)
    {
        try
        {
            JObject respondJO = new JObject();

            respondJO.Add("tag", TLJCommon.Consts.Tag_UserInfo_Game);
            respondJO.Add("uid", uid);
            respondJO.Add("isClientReq", 0);

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
            TLJ_PlayService.PlayService.log.Error("Request_UserInfo_Game.doRequest----" + ex.Message);
        }
    }

    public static void onMySqlRespond(string respondData)
    {
        try
        {
            UserInfo_Game userInfo_Game = Newtonsoft.Json.JsonConvert.DeserializeObject<UserInfo_Game>(respondData);
            UserInfo_Game_Manager.addOneData(userInfo_Game);

            // 获取到玩家信息之后找到该玩家所在的房间，给同桌的玩家推送此人的信息
            {
                // 去升级找
                RoomData room = GameUtil.getRoomByUid(userInfo_Game.uid);
                if (room != null)
                {
                    string data = Newtonsoft.Json.JsonConvert.SerializeObject(userInfo_Game);

                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        if ((!room.getPlayerDataList()[i].isOffLine()) && (room.getPlayerDataList()[i].m_uid.CompareTo(userInfo_Game.uid) != 0))
                        {
                            // 发送给客户端
                            PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, data);
                        }
                    }
                }
                else
                {
                    // 去斗地主找
                    DDZ_RoomData room_ddz = DDZ_GameUtil.getRoomByUid(userInfo_Game.uid);
                    if (room_ddz != null)
                    {
                        string data = Newtonsoft.Json.JsonConvert.SerializeObject(userInfo_Game);

                        for (int i = 0; i < room_ddz.getPlayerDataList().Count; i++)
                        {
                            if ((!room_ddz.getPlayerDataList()[i].isOffLine()) && (room_ddz.getPlayerDataList()[i].m_uid.CompareTo(userInfo_Game.uid) != 0))
                            {
                                // 发送给客户端
                                PlayService.m_serverUtil.sendMessage(room_ddz.getPlayerDataList()[i].m_connId, data);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error("Request_UserInfo_Game.onMySqlRespond----" + ex.Message + "," + respondData);

            // 客户端参数错误
            //respondJO.Add("code", Convert.ToInt32(TLJCommon.Consts.Code.Code_ParamError));

            // 发送给客户端
            //LogicService.m_serverUtil.sendMessage(connId, respondJO.ToString());
        }
    }
}