using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TLJ_PlayService;

class PlayLogic_Relax
{
    static PlayLogic_Relax s_playLogic_Normal = null;

    List<RoomData> m_roomList = new List<RoomData>();

    int m_tuoguanOutPokerDur = 100;        // 托管出牌时间

    public static PlayLogic_Relax getInstance()
    {
        if (s_playLogic_Normal == null)
        {
            s_playLogic_Normal = new PlayLogic_Relax();
        }

        return s_playLogic_Normal;
    }

    public int getPlayerCount()
    {
        int count = 0;
        for (int i = 0; i < m_roomList.Count; i++)
        {
            List<PlayerData> playerDataList = m_roomList[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (!playerDataList[j].m_isOffLine)
                {
                    ++count;
                }
            }
        }

        return count;
    }

    public int getRoomCount()
    {
        return m_roomList.Count;
    }

    public void OnReceive(IntPtr connId, string data)
    {
        JObject respondJO = new JObject();

        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            respondJO.Add("tag", tag);

            int playAction = Convert.ToInt32(jo.GetValue("playAction"));

            switch (playAction)
            {
                case (int)TLJCommon.Consts.PlayAction.PlayAction_JoinGame:
                    {
                        doTask_JoinGame(connId, data);
                    }
                    break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_ExitGame:
                    {
                        doTask_ExitGame(connId, data);
                    }
                    break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_QiangZhu:
                    {
                        doTask_QiangZhu(connId, data);
                    }
                    break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_MaiDi:
                    {
                        doTask_MaiDi(connId, data);
                    }
                    break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_PlayerOutPoker:
                    {
                        doTask_ReceivePlayerOutPoker(connId, data);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().writeLogToLocalNow("PlayLogic_Relax.OnReceive()异常：" + ex.Message);
            // 客户端参数错误
            respondJO.Add("code", Convert.ToInt32(TLJCommon.Consts.Code.Code_ParamError));

            // 发送给客户端
            PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
        }
    }

    void doTask_JoinGame(IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            int playAction = Convert.ToInt32(jo.GetValue("playAction"));

            RoomData room = null;

            // 检测该玩家是否已经加入房间
            for (int i = 0; i < m_roomList.Count; i++)
            {
                List<PlayerData> playerDataList = m_roomList[i].getPlayerDataList();

                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                    {
                        // 给客户端回复
                        {
                            JObject respondJO = new JObject();
                            respondJO.Add("tag", tag);
                            respondJO.Add("playAction", playAction);
                            respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_CommonFail);

                            // 发送给客户端
                            PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
                        }

                        return;
                    }
                }
            }

            // 在已有的房间寻找可以加入的房间
            for (int i = 0; i < m_roomList.Count; i++)
            {
                if (m_roomList[i].joinPlayer(new PlayerData(connId, uid)))
                {
                    room = m_roomList[i];
                }
            }

            // 当前没有房间可加入的话则创建一个新的房间
            if (room == null)
            {
                room = new RoomData(m_roomList.Count + 1);
                room.joinPlayer(new PlayerData(connId, uid));

                m_roomList.Add(room);
            }

            // 加入房间成功，给客户端回复
            {
                JObject respondJO = new JObject();
                respondJO.Add("tag", tag);
                respondJO.Add("playAction", playAction);
                respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_OK);
                respondJO.Add("roomId", room.getRoomId());

                // 发送给客户端
                PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
            }

            // 检测房间人数是否可以开赛
            if (room.getPlayerDataList().Count == 4)
            {
                room.m_isStartGame = true;

                // 设置级牌
                room.m_levelPokerNum = 2;

                JObject respondJO = new JObject();
                respondJO.Add("tag", tag);
                respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_StartGame);
                respondJO.Add("levelPokerNum", room.m_levelPokerNum);

                // 生成每个人的牌
                {
                    // 随机分配牌
                    //List<List<TLJCommon.PokerInfo>> pokerInfoList = AllotPoker.AllotPokerToPlayer();
                    // 用配置文件的牌
                    List<List<TLJCommon.PokerInfo>> pokerInfoList = AllotPoker.AllotPokerToPlayerByDebug();
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
                        if (respondJO.GetValue("teammateUID") != null)
                        {
                            respondJO.Remove("teammateUID");
                        }

                        // 分配各自队友
                        if (i == 0)
                        {
                            respondJO.Add("teammateUID", room.getPlayerDataList()[2].m_uid);
                            room.getPlayerDataList()[i].m_teammateUID = room.getPlayerDataList()[2].m_uid;
                        }
                        else if (i == 1)
                        {
                            respondJO.Add("teammateUID", room.getPlayerDataList()[3].m_uid);
                            room.getPlayerDataList()[i].m_teammateUID = room.getPlayerDataList()[3].m_uid;
                        }
                        else if (i == 2)
                        {
                            respondJO.Add("teammateUID", room.getPlayerDataList()[0].m_uid);
                            room.getPlayerDataList()[i].m_teammateUID = room.getPlayerDataList()[0].m_uid;
                        }
                        else if (i == 3)
                        {
                            respondJO.Add("teammateUID", room.getPlayerDataList()[1].m_uid);
                            room.getPlayerDataList()[i].m_teammateUID = room.getPlayerDataList()[1].m_uid;
                        }

                        JObject temp = new JObject();
                        temp.Add("name", "no name");
                        temp.Add("uid", room.getPlayerDataList()[i].m_uid);

                        userList.Add(temp);
                    }
                    respondJO.Add("userList", userList);
                }

                // 通知房间内的人开始比赛
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    if (respondJO.GetValue("pokerList") != null)
                    {
                        respondJO.Remove("pokerList");
                    }

                    // 自己的牌
                    {
                        JArray pokerList = new JArray();
                        for (int j = 0; j < room.getPlayerDataList()[i].getPokerList().Count; j++)
                        {
                            JObject temp = new JObject();
                            temp.Add("num", room.getPlayerDataList()[i].getPokerList()[j].m_num);
                            temp.Add("pokerType", (int)room.getPlayerDataList()[i].getPokerList()[j].m_pokerType);

                            pokerList.Add(temp);
                        }

                        respondJO.Add("pokerList", pokerList);
                    }

                    // 人数已满,可以开赛，发送给客户端
                    PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, respondJO.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("doTask_JoinGame异常：" + ex.Message);
        }
    }

    void doTask_ExitGame(IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            int playAction = Convert.ToInt32(jo.GetValue("playAction"));

            // 检测该玩家是否已经加入房间
            for (int i = 0; i < m_roomList.Count; i++)
            {
                List<PlayerData> playerDataList = m_roomList[i].getPlayerDataList();

                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                    {
                        // 给客户端回复
                        {
                            JObject respondJO = new JObject();
                            respondJO.Add("tag", tag);
                            respondJO.Add("playAction", playAction);
                            respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_OK);
                            respondJO.Add("roomId", m_roomList[i].getRoomId());

                            // 发送给客户端
                            PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
                        }

                        // 如果此人所在的房间已经开赛，则进入托管模式
                        if (m_roomList[i].m_isStartGame)
                        {
                            playerDataList[j].m_isOffLine = true;
                        }
                        // 如果此人所在的房间还没有开赛，则从房间删除此人
                        else
                        {
                            playerDataList.RemoveAt(j);
                            if (playerDataList.Count == 0)
                            {
                                m_roomList.RemoveAt(i);
                            }
                        }

                        return;
                    }
                }
            }

            // 玩家不在房间内，则退出房间失败，给客户端回复
            {
                JObject respondJO = new JObject();
                respondJO.Add("tag", tag);
                respondJO.Add("playAction", playAction);
                respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_CommonFail);

                // 发送给客户端
                PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("doTask_ExitGame异常：" + ex.Message);
        }
    }

    // 玩家抢主
    public void doTask_QiangZhu(IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            int playAction = Convert.ToInt32(jo.GetValue("playAction"));
            int pokerType = Convert.ToInt32(jo.GetValue("pokerType"));

            for (int i = 0; i < m_roomList.Count; i++)
            {
                RoomData room = m_roomList[i];
                List<PlayerData> playerDataList = room.getPlayerDataList();

                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                    {
                        // 只有一个人可以抢主，先来先得
                        if (room.m_zhuangjiaPlayerData == null)
                        {
                            // 此人是来抢主的
                            if (pokerType != -1)
                            {
                                JObject respondJO = new JObject();
                                respondJO.Add("tag", tag);
                                respondJO.Add("playAction", playAction);
                                respondJO.Add("uid", uid);
                                respondJO.Add("pokerType", pokerType);

                                // 设置主牌花色
                                room.m_masterPokerType = pokerType;

                                room.m_zhuangjiaPlayerData = playerDataList[j];

                                // 发送给客户端
                                for (int k = 0; k < playerDataList.Count; k++)
                                {
                                    if (respondJO.GetValue("isBanker") != null)
                                    {
                                        respondJO.Remove("isBanker");
                                    }

                                    if ((playerDataList[k].m_uid.CompareTo(room.m_zhuangjiaPlayerData.m_uid) == 0) || (playerDataList[k].m_uid.CompareTo(room.m_zhuangjiaPlayerData.m_teammateUID) == 0))
                                    {
                                        playerDataList[k].m_isBanker = 1;
                                        respondJO.Add("isBanker", 1);
                                    }
                                    else
                                    {
                                        playerDataList[k].m_isBanker = 0;
                                        respondJO.Add("isBanker", 0);
                                    }

                                    PlayService.m_serverUtil.sendMessage(playerDataList[k].m_connId, respondJO.ToString());
                                }

                                //// 开始本房间的比赛
                                //doTask_CallPlayerOutPoker(room, data, true);

                                // 让该房间庄家埋底
                                callPlayerMaiDi(room);
                            }
                            // 此人是来通知服务端，抢主倒计时结束，谁先来通知，就默认把这个人设为庄家
                            else
                            {
                                JObject respondJO = new JObject();
                                respondJO.Add("tag", tag);
                                respondJO.Add("playAction", playAction);
                                respondJO.Add("uid", uid);
                                respondJO.Add("pokerType", pokerType);

                                room.m_zhuangjiaPlayerData = playerDataList[j];

                                // 发送给客户端
                                for (int k = 0; k < playerDataList.Count; k++)
                                {
                                    if (respondJO.GetValue("isBanker") != null)
                                    {
                                        respondJO.Remove("isBanker");
                                    }

                                    if ((playerDataList[k].m_uid.CompareTo(room.m_zhuangjiaPlayerData.m_uid) == 0) || (playerDataList[k].m_uid.CompareTo(room.m_zhuangjiaPlayerData.m_teammateUID) == 0))
                                    {
                                        playerDataList[k].m_isBanker = 1;
                                        respondJO.Add("isBanker", 1);
                                    }
                                    else
                                    {
                                        playerDataList[k].m_isBanker = 0;
                                        respondJO.Add("isBanker", 0);
                                    }

                                    PlayService.m_serverUtil.sendMessage(playerDataList[k].m_connId, respondJO.ToString());
                                }

                                //// 开始本房间的比赛
                                //doTask_CallPlayerOutPoker(room, data, true);

                                // 让该房间庄家埋底
                                callPlayerMaiDi(room);
                            }
                        }

                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("doTask_QiangZhu异常：" + ex.Message);
        }
    }

    // 玩家埋底
    public void doTask_MaiDi(IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            for (int i = 0; i < m_roomList.Count; i++)
            {
                RoomData room = m_roomList[i];
                List<PlayerData> playerDataList = room.getPlayerDataList();

                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                    {
                        // 删除此人出的牌、替换底牌
                        {
                            JArray ja = (JArray)JsonConvert.DeserializeObject(jo.GetValue("diPokerList").ToString());
                            for (int m = 0; m < ja.Count; m++)
                            {
                                int num = Convert.ToInt32(ja[m]["num"]);
                                int pokerType = Convert.ToInt32(ja[m]["pokerType"]);

                                for (int n = playerDataList[j].getPokerList().Count - 1; n >= 0; n--)
                                {
                                    if ((playerDataList[j].getPokerList()[n].m_num == num) && ((int)playerDataList[j].getPokerList()[n].m_pokerType == pokerType))
                                    {
                                        // 加到底牌里面
                                        room.getDiPokerList().Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));

                                        // 出的牌从自己的牌堆里删除
                                        {
                                            playerDataList[j].getPokerList().RemoveAt(n);
                                        }

                                        break;
                                    }
                                }
                            }
                        }

                        // 开始本房间的比赛
                        doTask_CallPlayerOutPoker(room, data, true);

                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("doTask_MaiDi异常：" + ex.Message);
        }
    }

    // 推送上一个玩家出的牌,并让下一个玩家出牌
    public void doTask_CallPlayerOutPoker(RoomData room , string data,bool isFirst)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            int isCurRoundFirstPlayer = 0;
            if (room.m_curOutPokerPlayer != null)
            {
                if (uid.CompareTo(room.m_curRoundFirstPlayer.m_uid) == 0)
                {
                    isCurRoundFirstPlayer = 1;
                }
            }

            if (room.m_curOutPokerPlayer == null)
            {
                room.m_curOutPokerPlayer = room.m_zhuangjiaPlayerData;
                room.m_curRoundFirstPlayer = room.m_zhuangjiaPlayerData;
            }
            else if (room.getPlayerDataList().IndexOf(room.m_curOutPokerPlayer) == 3)
            {
                room.m_curOutPokerPlayer = room.getPlayerDataList()[0];
            }
            else
            {
                room.m_curOutPokerPlayer = room.getPlayerDataList()[room.getPlayerDataList().IndexOf(room.m_curOutPokerPlayer) + 1];
            }

            int isFreeOutPoker = 0;
            if (isFirst)
            {
                isFreeOutPoker = 1;
            }

            // 庄家对家抓到的分数
            int getScore = 0;

            if (!isFirst)
            {
                // 一轮出牌结束
                if (room.m_curRoundFirstPlayer.m_uid.CompareTo(room.m_curOutPokerPlayer.m_uid) == 0)
                {
                    // 选出这一轮出的牌最大的人，作为下一轮先出牌的人
                    //PlayerData playerData = compareWhoMax(room.getPlayerDataList(), room.m_curRoundFirstPlayer);
                    PlayerData playerData = CompareWhoMax.compareWhoMax(room);
                    room.m_curOutPokerPlayer = playerData;
                    room.m_curRoundFirstPlayer = playerData;
                    
                    // 如果是庄家对家这一轮出牌最大，则把这一轮出的牌中5、10、k加到他们的所得分数上
                    if (playerData.m_isBanker == 0)
                    {
                        for (int i = 0; i < room.getPlayerDataList().Count; i++)
                        {
                            for (int j = 0; j < room.getPlayerDataList()[i].m_curOutPokerList.Count; j++)
                            {
                                if (room.getPlayerDataList()[i].m_curOutPokerList[j].m_num == 5)
                                {
                                    getScore += 5;
                                }
                                else if (room.getPlayerDataList()[i].m_curOutPokerList[j].m_num == 10)
                                {
                                    getScore += 10;
                                }
                                // 13代表“K”
                                else if (room.getPlayerDataList()[i].m_curOutPokerList[j].m_num == 13)
                                {
                                    getScore += 10;
                                }
                            }
                        }
                    }

                    // 清空上一个周期出的牌
                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        room.getPlayerDataList()[i].m_curOutPokerList.Clear();
                    }

                    // 自由出牌
                    isFreeOutPoker = 1;

                    room.m_getAllScore += getScore;
                }
            }

            // 检测是否所有人的牌都出完，是的话就解散该房间
            {
                bool isEnd = true;

                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    if (room.getPlayerDataList()[i].getPokerList().Count > 0)
                    {
                        isEnd = false;
                        break;
                    }
                }

                if (isEnd)
                {
                    gameOver(room,data);

                    return;
                }
            }

            // 通知
            {
                JObject respondJO;
                {
                    respondJO = new JObject();

                    respondJO.Add("tag", TLJCommon.Consts.Tag_XiuXianChang);
                    respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_CallPlayerOutPoker);
                    respondJO.Add("cur_uid", room.m_curOutPokerPlayer.m_uid);
                    respondJO.Add("isFreeOutPoker", isFreeOutPoker);
                    respondJO.Add("getScore", getScore);

                    if (isFirst)
                    {
                        respondJO.Add("hasPlayerOutPoker", 0);
                    }
                    else
                    {
                        respondJO.Add("hasPlayerOutPoker", 1);
                        respondJO.Add("isCurRoundFirstPlayer", isCurRoundFirstPlayer);
                        respondJO.Add("pre_uid", jo.GetValue("uid"));
                        respondJO.Add("pre_outPokerList", jo.GetValue("pokerList"));
                    }
                }

                // 给在线的人推送
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    // 推送给客户端
                    if (!room.getPlayerDataList()[i].m_isOffLine)
                    {
                        PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, respondJO.ToString());
                    }
                }

                // 如果当前出牌的人离线了，单独处理
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    if (room.getPlayerDataList()[i].m_isOffLine)
                    {
                        if (room.getPlayerDataList()[i].m_uid.CompareTo(room.m_curOutPokerPlayer.m_uid) == 0)
                        {
                            trusteeshipLogic(respondJO.ToString(), room.getPlayerDataList()[i]);

                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("doTask_CallPlayerOutPoker异常：" + ex.Message);
        }
    }

    // 收到玩家出的牌
    public void doTask_ReceivePlayerOutPoker(IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            RoomData room = null;

            // 找到玩家所在的房间
            for (int i = 0; i < m_roomList.Count; i++)
            {
                List<PlayerData> playerDataList = m_roomList[i].getPlayerDataList();

                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                    {
                        room = m_roomList[i];

                        {
                            JArray ja = (JArray)JsonConvert.DeserializeObject(jo.GetValue("pokerList").ToString());
                            List<TLJCommon.PokerInfo> outPokerList = new List<TLJCommon.PokerInfo>();
                            for (int m = 0; m < ja.Count; m++)
                            {
                                int num = Convert.ToInt32(ja[m]["num"]);
                                int pokerType = Convert.ToInt32(ja[m]["pokerType"]);

                                outPokerList.Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));
                            }

                            // 此人出的牌不是单牌、对子、拖拉机，如果是此回合第一个人出牌则当做甩牌处理
                            if(CheckOutPoker.checkOutPokerType(outPokerList) == CheckOutPoker.OutPokerType.OutPokerType_Error)
                            {
                                if (uid.CompareTo(room.m_curRoundFirstPlayer.m_uid) == 0)
                                {
                                    bool isSuccess = false;

                                    List<TLJCommon.PokerInfo> firstOutPokerList_single = GameUtil.choiceSinglePoker(outPokerList, outPokerList[0].m_pokerType);
                                    List<TLJCommon.PokerInfo> firstOutPokerList_double = GameUtil.choiceDoublePoker(outPokerList, outPokerList[0].m_pokerType);

                                    bool isSingleBigger = true;
                                    bool isDoubleBigger = true;

                                    // 检测是否成功
                                    {
                                        for (int k = 0; k < room.getPlayerDataList().Count; k++)
                                        {
                                            List<TLJCommon.PokerInfo> pokerList_single = GameUtil.choiceSinglePoker(room.getPlayerDataList()[k].getPokerList(), outPokerList[0].m_pokerType);
                                            List<TLJCommon.PokerInfo> pokerList_double = GameUtil.choiceDoublePoker(room.getPlayerDataList()[k].getPokerList(), outPokerList[0].m_pokerType);

                                            for (int m = 0; m < pokerList_single.Count; m++)
                                            {
                                                if (pokerList_single[m].m_num > firstOutPokerList_single[firstOutPokerList_single.Count - 1].m_num)
                                                {
                                                    isSingleBigger = false;
                                                    isSuccess = false;

                                                    break;
                                                }
                                            }

                                            for (int m = 0; m < pokerList_double.Count; m++)
                                            {
                                                if (pokerList_double[m].m_num > firstOutPokerList_double[firstOutPokerList_double.Count - 1].m_num)
                                                {
                                                    isDoubleBigger = false;
                                                    isSuccess = false;

                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    // 甩牌成功
                                    if (isSuccess)
                                    {
                                        // 从此人牌堆里删除他出的牌
                                        {
                                            //JArray ja = (JArray)JsonConvert.DeserializeObject(jo.GetValue("pokerList").ToString());
                                            for (int m = 0; m < ja.Count; m++)
                                            {
                                                int num = Convert.ToInt32(ja[m]["num"]);
                                                int pokerType = Convert.ToInt32(ja[m]["pokerType"]);

                                                for (int n = playerDataList[j].getPokerList().Count - 1; n >= 0; n--)
                                                {
                                                    if ((playerDataList[j].getPokerList()[n].m_num == num) && ((int)playerDataList[j].getPokerList()[n].m_pokerType == pokerType))
                                                    {
                                                        // 加到当前这一轮出牌的牌堆里面
                                                        playerDataList[j].m_curOutPokerList.Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));

                                                        // 出的牌从自己的牌堆里删除
                                                        {
                                                            playerDataList[j].getPokerList().RemoveAt(n);
                                                        }

                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    // 甩牌失败
                                    else
                                    {
                                        JObject respondJO;
                                        {
                                            respondJO = new JObject();

                                            respondJO.Add("tag", TLJCommon.Consts.Tag_XiuXianChang);
                                            respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_ShuaiPai);
                                            respondJO.Add("uid", room.m_curOutPokerPlayer.m_uid);
                                            respondJO.Add("pokerList", jo.GetValue("pokerList"));
                                        }

                                        // 给在线的人推送
                                        for (int n = 0; n < room.getPlayerDataList().Count; n++)
                                        {
                                            // 推送给客户端
                                            if (!room.getPlayerDataList()[n].m_isOffLine)
                                            {
                                                PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[n].m_connId, respondJO.ToString());
                                            }
                                        }

                                        Thread.Sleep(5000);

                                        {
                                            // 单牌和对子都比别人小、单牌比别人小对子比别人大，则优先出单牌
                                            if (((!isSingleBigger) && (!isDoubleBigger)) || ((!isSingleBigger) && isDoubleBigger))
                                            {
                                                {
                                                    JArray ja_outPoker = new JArray();
                                                    JObject jo_outPoker = new JObject();

                                                    int num = firstOutPokerList_single[firstOutPokerList_single.Count - 1].m_num;
                                                    int pokerType = (int)firstOutPokerList_single[firstOutPokerList_single.Count - 1].m_pokerType;

                                                    jo_outPoker.Add("num", num);
                                                    jo_outPoker.Add("pokerType", pokerType);

                                                    ja_outPoker.Add(jo_outPoker);

                                                    for (int n = playerDataList[j].getPokerList().Count - 1; n >= 0; n--)
                                                    {
                                                        if ((playerDataList[j].getPokerList()[n].m_num == num) && ((int)playerDataList[j].getPokerList()[n].m_pokerType == pokerType))
                                                        {
                                                            // 加到当前这一轮出牌的牌堆里面
                                                            playerDataList[j].m_curOutPokerList.Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));

                                                            // 出的牌从自己的牌堆里删除
                                                            {
                                                                playerDataList[j].getPokerList().RemoveAt(n);
                                                            }

                                                            break;
                                                        }
                                                    }

                                                    jo["pokerList"] = ja_outPoker;
                                                }
                                            }
                                            // 单牌比别人大，对子比别人小，则优先出对子
                                            else if (isSingleBigger && (!isDoubleBigger))
                                            {
                                                {
                                                    JArray ja_outPoker = new JArray();

                                                    int num = firstOutPokerList_double[firstOutPokerList_double.Count - 1].m_num;
                                                    int pokerType = (int)firstOutPokerList_double[firstOutPokerList_double.Count - 1].m_pokerType;

                                                    {
                                                        JObject jo_outPoker = new JObject();
                                                        jo_outPoker.Add("num", num);
                                                        jo_outPoker.Add("pokerType", pokerType);

                                                        ja_outPoker.Add(jo_outPoker);
                                                    }

                                                    {
                                                        JObject jo_outPoker = new JObject();
                                                        jo_outPoker.Add("num", num);
                                                        jo_outPoker.Add("pokerType", pokerType);

                                                        ja_outPoker.Add(jo_outPoker);
                                                    }

                                                    int findNum = 0;
                                                    for (int n = playerDataList[j].getPokerList().Count - 1; n >= 0; n--)
                                                    {
                                                        if ((playerDataList[j].getPokerList()[n].m_num == num) && ((int)playerDataList[j].getPokerList()[n].m_pokerType == pokerType))
                                                        {
                                                            // 加到当前这一轮出牌的牌堆里面
                                                            playerDataList[j].m_curOutPokerList.Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));

                                                            // 出的牌从自己的牌堆里删除
                                                            {
                                                                playerDataList[j].getPokerList().RemoveAt(n);
                                                            }

                                                            if ((++findNum) == 2)
                                                            {
                                                                break;
                                                            }
                                                        }
                                                    }

                                                    jo["pokerList"] = ja_outPoker;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            // 此人出的牌是单牌、对子、拖拉机,类型没问题，从此人牌堆里删除他出的牌
                            {
                                for (int m = 0; m < ja.Count; m++)
                                {
                                    int num = Convert.ToInt32(ja[m]["num"]);
                                    int pokerType = Convert.ToInt32(ja[m]["pokerType"]);

                                    for (int n = playerDataList[j].getPokerList().Count - 1; n >= 0; n--)
                                    {
                                        if ((playerDataList[j].getPokerList()[n].m_num == num) && ((int)playerDataList[j].getPokerList()[n].m_pokerType == pokerType))
                                        {
                                            // 加到当前这一轮出牌的牌堆里面
                                            playerDataList[j].m_curOutPokerList.Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));

                                            // 出的牌从自己的牌堆里删除
                                            {
                                                playerDataList[j].getPokerList().RemoveAt(n);
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        break;
                    }
                }

                if (room != null)
                {
                    break;
                }
            }

            //doTask_CallPlayerOutPoker(room, data, false);
            doTask_CallPlayerOutPoker(room, jo.ToString(), false);
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("doTask_ReceivePlayerOutPoker异常：" + ex.Message);
        }
    }

    public bool doTaskPlayerCloseConn(IntPtr connId)
    {
        try
        {
            for (int i = 0; i < m_roomList.Count; i++)
            {
                List<PlayerData> playerDataList = m_roomList[i].getPlayerDataList();

                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_connId == connId)
                    {
                        // 如果此人所在的房间已经开赛，则进入托管模式
                        if (m_roomList[i].m_isStartGame)
                        {
                            playerDataList[j].m_isOffLine = true;

                            // 如果当前房间正好轮到此人出牌
                            if (m_roomList[i].m_curOutPokerPlayer.m_uid.CompareTo(playerDataList[j].m_uid) == 0)
                            {
                                trusteeshipLogic(playerDataList[j]);
                            }
                        }
                        // 如果此人所在的房间还没有开赛，则从房间删除此人
                        else
                        {
                            playerDataList.RemoveAt(j);
                            if (playerDataList.Count == 0)
                            {
                                m_roomList.RemoveAt(i);
                            }
                        }

                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("doTaskPlayerCloseConn异常：" + ex.Message);
        }

        return false;
    }

    // 通知玩家开始埋底
    public void callPlayerMaiDi(RoomData room)
    {
        try
        {
            JObject respondJO = new JObject();
            respondJO.Add("tag", TLJCommon.Consts.Tag_XiuXianChang);
            respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_MaiDi);
            respondJO.Add("uid", room.m_zhuangjiaPlayerData.m_uid);

            // 底牌
            {
                JArray pokerList = new JArray();
                for (int i = 0; i < room.getDiPokerList().Count; i++)
                {
                    JObject temp = new JObject();

                    int num = room.getDiPokerList()[i].m_num;
                    int pokerType = (int)room.getDiPokerList()[i].m_pokerType;

                    temp.Add("num", num);
                    temp.Add("pokerType", pokerType);

                    pokerList.Add(temp);

                    // 把底牌加到庄家牌里面去
                    room.m_zhuangjiaPlayerData.getPokerList().Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));
                }

                respondJO.Add("diPokerList", pokerList);
            }

            // 通知房间内的人
            for (int i = 0; i < room.getPlayerDataList().Count; i++)
            {
                if (!room.getPlayerDataList()[i].m_isOffLine)
                {
                    PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, respondJO.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("callPlayerMaiDi异常：" + ex.Message);
        }
    }

    // 游戏结束
    void gameOver(RoomData room , string data)
    {
        try
        {
            LogUtil.getInstance().addDebugLog("比赛结束，解散该房间：" + room.getRoomId());

            JObject jo = JObject.Parse(data);

            // 通知
            {
                JObject respondJO;
                {
                    respondJO = new JObject();

                    respondJO.Add("tag", TLJCommon.Consts.Tag_XiuXianChang);
                    respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_GameOver);
                    respondJO.Add("getAllScore", room.m_getAllScore);
                    respondJO.Add("isBankerWin", room.m_getAllScore >= 80 ? 0 : 1);
                    respondJO.Add("pre_uid", jo.GetValue("uid"));
                    respondJO.Add("pre_outPokerList", jo.GetValue("pokerList"));
                }

                // 给在线的人推送
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    // 推送给客户端
                    if (!room.getPlayerDataList()[i].m_isOffLine)
                    {
                        PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, respondJO.ToString());
                    }
                }
            }

            m_roomList.Remove(room);
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("gameOver异常：" + ex.Message);
        }
    }

    // 比较这一轮出牌的大小
    PlayerData compareWhoMax(List<PlayerData> listPlayerData, PlayerData firstOutPokerPlayer)
    {
        /*
         * listPlayerData是按玩家进入房间顺序排的
         * 这里要改成按这一轮出牌顺序排
         */

        List<PlayerData> tempList = new List<PlayerData>();

        // 重新排序
        {
            int index = listPlayerData.IndexOf(firstOutPokerPlayer);

            for (int i = index; i < listPlayerData.Count; i++)
            {
                tempList.Add(listPlayerData[i]);
            }

            for (int i = 0; i < index; i++)
            {
                tempList.Add(listPlayerData[i]);
            }
        }

        PlayerData max = tempList[0];

        if (CheckOutPoker.checkOutPokerType(max.m_curOutPokerList) != CheckOutPoker.OutPokerType.OutPokerType_Error)
        {
            for (int i = 1; i < tempList.Count; i++)
            {
                // 出牌类型必须一样才可以继续比较大小，否则视为小
                if (CheckOutPoker.checkOutPokerType(max.m_curOutPokerList) == CheckOutPoker.checkOutPokerType(tempList[i].m_curOutPokerList))
                {
                    if (tempList[i].m_curOutPokerList[0].m_num > max.m_curOutPokerList[0].m_num)
                    {
                        max = tempList[i];
                    }
                }
            }
        }

        return max;
    }

    // 托管逻辑
    void trusteeshipLogic(string jsonData, PlayerData playerData)
    {
        try
        {
            Thread.Sleep(m_tuoguanOutPokerDur);

            JObject jo = JObject.Parse(jsonData);
            // 轮到自己出牌
            {
                if (playerData.getPokerList().Count > 0)
                {
                    JObject backData = new JObject();

                    backData.Add("tag", TLJCommon.Consts.Tag_XiuXianChang);
                    backData.Add("uid", playerData.m_uid);
                    backData.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_PlayerOutPoker);

                    // 自己出的牌
                    {
                        int num = playerData.getPokerList()[playerData.getPokerList().Count - 1].m_num;
                        int pokerType = (int)playerData.getPokerList()[playerData.getPokerList().Count - 1].m_pokerType;

                        // 出的牌从自己的牌堆里删除
                        //playerData.getPokerList().RemoveAt(playerData.getPokerList().Count - 1);

                        JArray jarray = new JArray();
                        //for (int i = 0; i < m_myPokerObjList.Count; i++)
                        //{
                        //    PokerScript pokerScript = m_myPokerObjList[i].GetComponent<PokerScript>();
                        //    if (pokerScript.getIsSelect())
                        {
                            JObject temp = new JObject();
                            temp.Add("num" , num);
                            temp.Add("pokerType" , pokerType);
                            jarray.Add(temp);
                        }
                        //}
                        backData.Add("pokerList", jarray);
                    }

                    LogUtil.getInstance().addDebugLog("托管出牌：" + playerData.m_uid);
                    doTask_ReceivePlayerOutPoker(playerData.m_connId, backData.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("trusteeshipLogic异常1：" + ex.Message);
        }
    }

    // 托管逻辑
    void trusteeshipLogic(PlayerData playerData)
    {
        try
        {
            Thread.Sleep(m_tuoguanOutPokerDur);

            // 轮到自己出牌
            {
                if (playerData.getPokerList().Count > 0)
                {
                    JObject backData = new JObject();

                    backData.Add("tag", TLJCommon.Consts.Tag_XiuXianChang);
                    backData.Add("uid", playerData.m_uid);
                    backData.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_PlayerOutPoker);

                    // 自己出的牌
                    {
                        //List<>
                        int num = playerData.getPokerList()[playerData.getPokerList().Count - 1].m_num;
                        int pokerType = (int)playerData.getPokerList()[playerData.getPokerList().Count - 1].m_pokerType;

                        // 出的牌从自己的牌堆里删除
                        //playerData.getPokerList().RemoveAt(playerData.getPokerList().Count - 1);

                        JArray jarray = new JArray();
                        //for (int i = 0; i < m_myPokerObjList.Count; i++)
                        //{
                        //    PokerScript pokerScript = m_myPokerObjList[i].GetComponent<PokerScript>();
                        //    if (pokerScript.getIsSelect())
                        {
                            JObject temp = new JObject();
                            temp.Add("num", num);
                            temp.Add("pokerType", pokerType);
                            jarray.Add(temp);
                        }
                        //}
                        backData.Add("pokerList", jarray);
                    }

                    LogUtil.getInstance().addDebugLog("托管出牌：" + playerData.m_uid);
                    doTask_ReceivePlayerOutPoker(playerData.m_connId, backData.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("trusteeshipLogic异常2：" + ex.Message);
        }
    }
}

class RoomData
{
    int m_roomId;
    public bool m_isStartGame = false;

    public PlayerData m_curOutPokerPlayer = null;
    public PlayerData m_curRoundFirstPlayer = null;
    
    // 本房间玩家信息
    List<PlayerData> m_playerDataList = new List<PlayerData>();

    // 底牌
    List<TLJCommon.PokerInfo> m_DiPokerList = new List<TLJCommon.PokerInfo>();

    // 默认为-1，代表没有被赋值过
    public int m_levelPokerNum = -1;            // 级牌
    public int m_masterPokerType = -1;          // 主牌花色

    public int m_getAllScore = 0;                  // 庄家对家抓到的分数

    public PlayerData m_zhuangjiaPlayerData = null;

    public RoomData(int roomId)
    {
        m_roomId = roomId;
    }

    public int getRoomId()
    {
        return m_roomId;
    }

    public List<PlayerData> getPlayerDataList()
    {
        return m_playerDataList;
    }

    public bool joinPlayer(PlayerData playerData)
    {
        if (m_playerDataList.Count < 4)
        {
            m_playerDataList.Add(playerData);

            return true;
        }

        return false;
    }

    public void setDiPokerList(List<TLJCommon.PokerInfo> diPokerList)
    {
        m_DiPokerList = diPokerList;
    }

    public List<TLJCommon.PokerInfo> getDiPokerList()
    {
        return m_DiPokerList;
    }
}

class PlayerData
{
    public IntPtr m_connId;
    public string m_uid;
    public string m_teammateUID;        // 队友uid
    public int m_isBanker = 0;          // 是否是庄家
    public bool m_isOffLine = false;

    List<TLJCommon.PokerInfo> m_pokerList = new List<TLJCommon.PokerInfo>();
    public List<TLJCommon.PokerInfo> m_curOutPokerList = new List<TLJCommon.PokerInfo>();

    public PlayerData(IntPtr connId, string uid)
    {
        m_connId = connId;
        m_uid = uid;
    }

    public void setPokerList(List<TLJCommon.PokerInfo> pokerList)
    {
        m_pokerList = pokerList;
    }

    public List<TLJCommon.PokerInfo> getPokerList()
    {
        return m_pokerList;
    }
}