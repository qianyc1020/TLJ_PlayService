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
                RoomData room = GameUtil.getRoomByUid(uid);
                PlayerData playerData = room.getPlayerDataByUid(uid);

                if (room != null)
                {
                    respondJO.Add("tag", TLJCommon.Consts.Tag_ResumeGame);
                    respondJO.Add("gameroomtype", room.m_gameRoomType);
                    respondJO.Add("roomState", (int)room.m_roomState);
                    respondJO.Add("isUseJiPaiQi", playerData.m_isUseJiPaiQi);
                    respondJO.Add("levelPokerNum", room.m_levelPokerNum);
                    respondJO.Add("myLevelPoker", playerData.m_myLevelPoker);

                    if (room.getPlayerDataList().IndexOf(playerData) == 3)
                    {
                        respondJO.Add("otherLevelPoker", room.getPlayerDataList()[0].m_myLevelPoker);
                    }
                    else
                    {
                        respondJO.Add("otherLevelPoker", room.getPlayerDataList()[room.getPlayerDataList().IndexOf(playerData) + 1].m_myLevelPoker);
                    }

                    respondJO.Add("masterPokerType", room.m_masterPokerType);
                    respondJO.Add("teammateUID", playerData.m_teammateUID);
                    respondJO.Add("getAllScore", room.m_getAllScore);

                    // 庄家uid
                    if (room.m_zhuangjiaPlayerData != null)
                    {
                        respondJO.Add("zhuangjiaUID", room.m_zhuangjiaPlayerData.m_uid);
                    }
                    else
                    {
                        respondJO.Add("zhuangjiaUID", "");
                    }

                    // 当前回合第一个出牌的人
                    if (room.m_curRoundFirstPlayer != null)
                    {
                        respondJO.Add("curRoundFirstPlayer", room.m_curRoundFirstPlayer.m_uid);
                    }
                    else
                    {
                        respondJO.Add("curRoundFirstPlayer", "");
                    }

                    // 当前出牌的人
                    if (room.m_curOutPokerPlayer != null)
                    {
                        respondJO.Add("curOutPokerPlayer", room.m_curOutPokerPlayer.m_uid);
                    }
                    else
                    {
                        respondJO.Add("curOutPokerPlayer", "");
                    }

                    // 当前出牌的人是否是自由出牌
                    if (room.m_curRoundFirstPlayer != null && room.m_curOutPokerPlayer != null)
                    {
                        if (room.m_curRoundFirstPlayer.m_uid.CompareTo(room.m_curOutPokerPlayer.m_uid) == 0)
                        {
                            respondJO.Add("isFreeOutPoker", 1);
                        }
                        else
                        {
                            respondJO.Add("isFreeOutPoker", 0);
                        }
                    }
                    else
                    {
                        respondJO.Add("isFreeOutPoker", 0);
                    }

                    // 当前埋底的人
                    if (room.m_curMaiDiPlayer != null)
                    {
                        respondJO.Add("curMaiDiPlayer", room.m_curMaiDiPlayer.m_uid);
                    }
                    else
                    {
                        respondJO.Add("curMaiDiPlayer", "");
                    }

                    // 当前炒底的人
                    if (room.m_curChaoDiPlayer != null)
                    {
                        respondJO.Add("curChaoDiPlayer", room.m_curChaoDiPlayer.m_uid);
                    }
                    else
                    {
                        respondJO.Add("curChaoDiPlayer", "");
                    }

                    // 当前回合出的牌
                    {
                        for (int i = 0; i < room.getPlayerDataList().Count; i++)
                        {
                            JArray ja_curRoundPlayerOutPokerList = new JArray();
                            
                            for (int j = 0; j < room.getPlayerDataList()[i].m_curOutPokerList.Count; j++)
                            {
                                JObject temp = new JObject();

                                int num = room.getPlayerDataList()[i].m_curOutPokerList[j].m_num;
                                int pokerType = (int)room.getPlayerDataList()[i].m_curOutPokerList[j].m_pokerType;

                                temp.Add("num", num);
                                temp.Add("pokerType", pokerType);

                                ja_curRoundPlayerOutPokerList.Add(temp);
                            }

                            respondJO.Add("player" + i + "OutPokerList", ja_curRoundPlayerOutPokerList);
                        }
                    }

                    // 当局所有已经出掉的牌
                    {
                        JArray ja_allOutPokerList = new JArray();

                        for (int i = 0; i < room.m_allOutPokerList.Count; i++)
                        {
                            JObject temp = new JObject();

                            int num = room.m_allOutPokerList[i].m_num;
                            int pokerType = (int)room.m_allOutPokerList[i].m_pokerType;

                            temp.Add("num", num);
                            temp.Add("pokerType", pokerType);

                            ja_allOutPokerList.Add(temp);
                        }

                        respondJO.Add("allOutPokerList", ja_allOutPokerList);
                    }

                    // userList
                    {
                        JArray ja = new JArray();
                        for (int i = 0; i < room.getPlayerDataList().Count; i++)
                        {
                            JObject temp = new JObject();
                            temp.Add("uid", room.getPlayerDataList()[i].m_uid);
                            temp.Add("score", room.getPlayerDataList()[i].m_score);

                            ja.Add(temp);
                        }
                        respondJO.Add("userList", ja);
                    }

                    // myPokerList
                    {
                        JArray ja = new JArray();
                        for (int i = 0; i < room.getPlayerDataList().Count; i++)
                        {
                            if (room.getPlayerDataList()[i].m_uid.CompareTo(uid) == 0)
                            {
                                for (int j = 0; j < room.getPlayerDataList()[i].getPokerList().Count; j++)
                                {
                                    JObject temp = new JObject();
                                    temp.Add("num", room.getPlayerDataList()[i].getPokerList()[j].m_num);
                                    temp.Add("pokerType", (int)room.getPlayerDataList()[i].getPokerList()[j].m_pokerType);

                                    ja.Add(temp);
                                }
                                
                                break;
                            }
                        }
                        respondJO.Add("myPokerList", ja);
                    }

                    // allotPokerList
                    {
                        JArray ja = new JArray();
                        for (int i = 0; i < room.getPlayerDataList().Count; i++)
                        {
                            if (room.getPlayerDataList()[i].m_uid.CompareTo(uid) == 0)
                            {
                                for (int j = 0; j < room.getPlayerDataList()[i].m_allotPokerList.Count; j++)
                                {
                                    JObject temp = new JObject();
                                    temp.Add("num", room.getPlayerDataList()[i].m_allotPokerList[j].m_num);
                                    temp.Add("pokerType", (int)room.getPlayerDataList()[i].m_allotPokerList[j].m_pokerType);

                                    ja.Add(temp);
                                }

                                break;
                            }
                        }
                        respondJO.Add("allotPokerList", ja);
                    }

                    // 把玩家设为在线
                    {
                        playerData.m_isOffLine = false;
                        playerData.m_connId = connId;
                    }

                    // 发送给客户端
                    PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("NetRespond_RetryJoinGame----" + ex.Message);

            // 客户端参数错误
            respondJO.Add("code", Convert.ToInt32(TLJCommon.Consts.Code.Code_ParamError));

            //// 发送给客户端
            //PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
        }
    }
}