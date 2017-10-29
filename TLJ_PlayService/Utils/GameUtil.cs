using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TLJ_PlayService;

class GameUtil
{
    // 找出一组牌中某种花色的单牌
    public static List<TLJCommon.PokerInfo> choiceSinglePoker(List<TLJCommon.PokerInfo> myPokerList, TLJCommon.Consts.PokerType pokerType)
    {
        // 先筛选出同花色的牌
        List<TLJCommon.PokerInfo> pokerList = new List<TLJCommon.PokerInfo>();
        for (int i = myPokerList.Count - 1; i >= 0; i--)
        {
            if (myPokerList[i].m_pokerType == pokerType)
            {
                pokerList.Add(myPokerList[i]);
            }
        }

        List<TLJCommon.PokerInfo> singleList = new List<TLJCommon.PokerInfo>();
        List<TLJCommon.PokerInfo> doubleList = new List<TLJCommon.PokerInfo>();

        if (pokerList.Count > 1)
        {
            for (int i = pokerList.Count - 1; i >= 1; i--)
            {
                if (pokerList[i].m_num == pokerList[i - 1].m_num)
                {
                    doubleList.Add(pokerList[i]);
                    --i;

                    if (i == 1)
                    {
                        singleList.Add(pokerList[i - 1]);
                    }
                }
                else
                {
                    singleList.Add(pokerList[i]);

                    if (i == 1)
                    {
                        singleList.Add(pokerList[i - 1]);
                    }
                }
            }
        }
        else if (pokerList.Count == 1)
        {
            singleList.Add(pokerList[0]);
        }

        return singleList;
    }

    // 找出一组牌中某种花色的对子
    public static List<TLJCommon.PokerInfo> choiceDoublePoker(List<TLJCommon.PokerInfo> myPokerList, TLJCommon.Consts.PokerType pokerType)
    {
        // 先筛选出同花色的牌
        List<TLJCommon.PokerInfo> pokerList = new List<TLJCommon.PokerInfo>();
        for (int i = myPokerList.Count - 1; i >= 0; i--)
        {
            if (myPokerList[i].m_pokerType == pokerType)
            {
                pokerList.Add(myPokerList[i]);
            }
        }

        List<TLJCommon.PokerInfo> singleList = new List<TLJCommon.PokerInfo>();
        List<TLJCommon.PokerInfo> doubleList = new List<TLJCommon.PokerInfo>();

        if (pokerList.Count > 1)
        {
            for (int i = pokerList.Count - 1; i >= 1; i--)
            {
                if (pokerList[i].m_num == pokerList[i - 1].m_num)
                {
                    doubleList.Add(pokerList[i]);
                    --i;

                    if (i == 1)
                    {
                        singleList.Add(pokerList[i - 1]);
                    }
                }
                else
                {
                    singleList.Add(pokerList[i]);

                    if (i == 1)
                    {
                        singleList.Add(pokerList[i - 1]);
                    }
                }
            }
        }
        else if (pokerList.Count == 1)
        {
            singleList.Add(pokerList[0]);
        }

        return doubleList;
    }

    public static bool checkRoomNonePlayer(RoomData room)
    {
        bool isRemove = true;
        for (int i = 0; i < room.getPlayerDataList().Count; i++)
        {
            // 推送给客户端
            if (!room.getPlayerDataList()[i].m_isOffLine)
            {
                isRemove = false;
                break;
            }
        }

        return isRemove;
    }

    public static int getGameRoomPlayerCount(string gameRoomType)
    {
        int count = 0;
        for (int i = 0; i < PlayLogic_PVP.getInstance().getRoomList().Count; i++)
        {
            RoomData tempRoom = PlayLogic_PVP.getInstance().getRoomList()[i];
            if (tempRoom.m_gameRoomType.CompareTo(gameRoomType) == 0)
            {
                for (int j = 0; j < tempRoom.getPlayerDataList().Count; j++)
                {
                    ++count;
                }
            }
        }

        return count;
    }

    /*
     * 检测该房间是否可以开始打牌
     * 如果可以的话就通知房间内的玩家
     * 然后每隔500毫秒给玩家发一张牌
     */
    public static void checkRoomStartGame(RoomData room,string tag,bool initLevelPokerNum)
    {
        try
        {
            if (room.getPlayerDataList().Count == 4)
            {
                // 提交任务
                {
                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        if ((!room.getPlayerDataList()[i].m_isOffLine) && (!room.getPlayerDataList()[i].m_isAI))
                        {
                            List<string> tempList = new List<string>();
                            CommonUtil.splitStr(room.m_gameRoomType, tempList, '_');

                            switch (tempList[1])
                            {
                                case "JingDian":
                                    {
                                        Request_ProgressTask.doRequest(room.getPlayerDataList()[i].m_uid, 201);
                                        Request_ProgressTask.doRequest(room.getPlayerDataList()[i].m_uid, 205);
                                    }
                                    break;

                                case "ChaoDi":
                                    {
                                        Request_ProgressTask.doRequest(room.getPlayerDataList()[i].m_uid, 202);
                                        Request_ProgressTask.doRequest(room.getPlayerDataList()[i].m_uid, 207);
                                    }
                                    break;

                                case "JinBi":
                                    {
                                        Request_ProgressTask.doRequest(room.getPlayerDataList()[i].m_uid, 206);
                                    }
                                    break;

                                case "HuaFei":
                                    {
                                        Request_ProgressTask.doRequest(room.getPlayerDataList()[i].m_uid, 206);
                                    }
                                    break;
                            }
                        }
                    }
                }

                room.m_isStartGame = true;
                room.m_roomState = RoomData.RoomState.RoomState_qiangzhu;

                // 设置级牌
                if (initLevelPokerNum)
                {
                    room.m_levelPokerNum = 2;

                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        room.getPlayerDataList()[i].m_myLevelPoker = room.m_levelPokerNum;
                    }
                }

                JObject respondJO = new JObject();
                respondJO.Add("tag", tag);
                respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_StartGame);
                respondJO.Add("levelPokerNum", room.m_levelPokerNum);

                // 生成每个人的牌
                {
                    // 随机分配牌
                    List<List<TLJCommon.PokerInfo>> pokerInfoList = AllotPoker.AllotPokerToPlayer();
                    // 用配置文件的牌
                    //List<List<TLJCommon.PokerInfo>> pokerInfoList = AllotPoker.AllotPokerToPlayerByDebug();
                    room.getPlayerDataList()[0].setPokerList(pokerInfoList[0]);
                    room.getPlayerDataList()[1].setPokerList(pokerInfoList[1]);
                    room.getPlayerDataList()[2].setPokerList(pokerInfoList[2]);
                    room.getPlayerDataList()[3].setPokerList(pokerInfoList[3]);
                    room.setDiPokerList(pokerInfoList[4]);
                }

                // 本房间的所有玩家
                {
                    JArray userList = new JArray();
                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        JObject temp = new JObject();
                        temp.Add("name", "no name");
                        temp.Add("uid", room.getPlayerDataList()[i].m_uid);

                        userList.Add(temp);
                    }
                    respondJO.Add("userList", userList);
                }

                // 通知房间内的人开始比赛
                for (int i = 0; i < 4; i++)
                {
                    {
                        if (respondJO.GetValue("teammateUID") != null)
                        {
                            respondJO.Remove("teammateUID");
                        }

                        if (respondJO.GetValue("myLevelPoker") != null)
                        {
                            respondJO.Remove("myLevelPoker");
                        }

                        if (respondJO.GetValue("otherLevelPoker") != null)
                        {
                            respondJO.Remove("otherLevelPoker");
                        }

                        // 分配各自队友:0->2    1->3
                        if (i == 0)
                        {
                            respondJO.Add("teammateUID", room.getPlayerDataList()[2].m_uid);
                            room.getPlayerDataList()[i].m_teammateUID = room.getPlayerDataList()[2].m_uid;

                            respondJO.Add("myLevelPoker", room.getPlayerDataList()[i].m_myLevelPoker);
                            respondJO.Add("otherLevelPoker", room.getPlayerDataList()[1].m_myLevelPoker);
                        }
                        else if (i == 1)
                        {
                            respondJO.Add("teammateUID", room.getPlayerDataList()[3].m_uid);
                            room.getPlayerDataList()[i].m_teammateUID = room.getPlayerDataList()[3].m_uid;

                            respondJO.Add("myLevelPoker", room.getPlayerDataList()[i].m_myLevelPoker);
                            respondJO.Add("otherLevelPoker", room.getPlayerDataList()[0].m_myLevelPoker);
                        }
                        else if (i == 2)
                        {
                            respondJO.Add("teammateUID", room.getPlayerDataList()[0].m_uid);
                            room.getPlayerDataList()[i].m_teammateUID = room.getPlayerDataList()[0].m_uid;

                            respondJO.Add("myLevelPoker", room.getPlayerDataList()[i].m_myLevelPoker);
                            respondJO.Add("otherLevelPoker", room.getPlayerDataList()[1].m_myLevelPoker);
                        }
                        else if (i == 3)
                        {
                            respondJO.Add("teammateUID", room.getPlayerDataList()[1].m_uid);
                            room.getPlayerDataList()[i].m_teammateUID = room.getPlayerDataList()[1].m_uid;

                            respondJO.Add("myLevelPoker", room.getPlayerDataList()[i].m_myLevelPoker);
                            respondJO.Add("otherLevelPoker", room.getPlayerDataList()[0].m_myLevelPoker);
                        }
                    }

                    // 人数已满,可以开赛，发送给客户端
                    PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, respondJO.ToString());
                }

                // 一张一张给每人发牌
                {
                    for (int i = 0; i < 25; i++)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            if (!room.getPlayerDataList()[j].m_isOffLine)
                            {
                                JObject jo2 = new JObject();
                                jo2.Add("tag", tag);
                                jo2.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_FaPai);
                                jo2.Add("num", room.getPlayerDataList()[j].getPokerList()[i].m_num);
                                jo2.Add("pokerType", (int)room.getPlayerDataList()[j].getPokerList()[i].m_pokerType);

                                if (i == 24)
                                {
                                    jo2.Add("isEnd", 1);
                                }
                                else
                                {
                                    jo2.Add("isEnd", 0);
                                }

                                PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[j].m_connId, jo2.ToString());
                            }
                        }

                        Thread.Sleep(500);
                    }
                }
            }
            else
            {
                LogUtil.getInstance().addDebugLog("GameUtils----" + ":人数不够无法开赛：count = " + room.getPlayerDataList().Count);
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("GameUtil.checkRoomStartGame()----" + ex.Message + "tag:" + tag + "  roomid:" + room.getRoomId() + "gameroomtype:" + room.m_gameRoomType);
        }
    }

    public static void setPlayerScore(RoomData room)
    {
        try
        {
            float jichufenshu = 1000;
            float changcixishu = 1;
            float defenxishu = 1;
            float xianjiadefen;

            // 计算场次系数
            {
                List<string> tempList = new List<string>();
                CommonUtil.splitStr(room.m_gameRoomType, tempList, '_');

                switch (tempList[0])
                {
                    case "XiuXian":
                        {
                            if (tempList[2].CompareTo("ChuJi") == 0)
                            {
                                changcixishu = 1;
                            }
                            else if (tempList[2].CompareTo("ZhongJi") == 0)
                            {
                                changcixishu = 2;
                            }
                            else if (tempList[2].CompareTo("GaoJi") == 0)
                            {
                                changcixishu = 3;
                            }
                        }
                        break;

                    case "PVP":
                        {
                            changcixishu = 1;
                        }
                        break;
                }
            }

            // 计算得分系数
            {
                if (room.m_getAllScore == 0)
                {
                    defenxishu = -3.8f;
                }
                else if ((room.m_getAllScore >= 5) && (room.m_getAllScore <= 40))
                {
                    defenxishu = -2.8f;
                }
                else if ((room.m_getAllScore >= 45) && (room.m_getAllScore <= 75))
                {
                    defenxishu = -1.8f;
                }
                else if (room.m_getAllScore == 80)
                {
                    defenxishu = 1.0f;
                }
                else if ((room.m_getAllScore >= 85) && (room.m_getAllScore <= 120))
                {
                    defenxishu = 1.8f;
                }
                else if ((room.m_getAllScore >= 125) && (room.m_getAllScore <= 195))
                {
                    defenxishu = 2.8f;
                }
                else if (room.m_getAllScore == 200)
                {
                    defenxishu = 3.8f;
                }
            }

            xianjiadefen = jichufenshu * changcixishu * defenxishu;

            for (int i = 0; i < room.getPlayerDataList().Count; i++)
            {
                if (room.getPlayerDataList()[i].m_isBanker == 1)
                {
                    room.getPlayerDataList()[i].m_score += (-(int)xianjiadefen);
                }
                else
                {
                    room.getPlayerDataList()[i].m_score += (int)xianjiadefen;
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("GameUtil.setPlayerScore()----" + ex.Message + "gameRoomType:" + room.m_gameRoomType);
        }
    }
}