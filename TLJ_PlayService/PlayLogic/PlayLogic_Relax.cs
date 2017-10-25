using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TLJCommon;
using TLJ_PlayService;

class PlayLogic_Relax
{
    static PlayLogic_Relax s_playLogic_Normal = null;

    List<RoomData> m_roomList = new List<RoomData>();

    int m_tuoguanOutPokerDur = 100; // 托管出牌时间:毫秒

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

    public List<RoomData> getRoomList()
    {
        return m_roomList;
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
                case (int) TLJCommon.Consts.PlayAction.PlayAction_JoinGame:
                {
                    doTask_JoinGame(connId, data);
                }
                break;

                case (int) TLJCommon.Consts.PlayAction.PlayAction_ExitGame:
                {
                    doTask_ExitGame(connId, data);
                }
                break;

                case (int) TLJCommon.Consts.PlayAction.PlayAction_QiangZhu:
                {
                    doTask_QiangZhu(connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_QiangZhuEnd:
                {
                    doTask_QiangZhuEnd(connId, data);
                }
                break;

                case (int) TLJCommon.Consts.PlayAction.PlayAction_MaiDi:
                {
                    doTask_MaiDi(connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_PlayerChaoDi:
                {
                    doTask_PlayerChaoDi(connId, data);
                }
                break;

                case (int) TLJCommon.Consts.PlayAction.PlayAction_PlayerOutPoker:
                {
                    doTask_ReceivePlayerOutPoker(connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_ContinueGame:
                {
                    doTask_ContinueGame(connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_ChangeRoom:
                {
                    doTask_ChangeRoom(connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_Chat:
                {
                    doTask_Chat(connId, data);
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
            string gameroomtype = jo.GetValue("gameroomtype").ToString();
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
                            respondJO.Add("gameroomtype", gameroomtype);
                            respondJO.Add("code", (int) TLJCommon.Consts.Code.Code_CommonFail);

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
                if(gameroomtype.CompareTo(m_roomList[i].m_gameRoomType) == 0)
                {
                    if (m_roomList[i].joinPlayer(new PlayerData(connId, uid)))
                    {
                        room = m_roomList[i];
                        break;
                    }
                }
            }

            // 当前没有房间可加入的话则创建一个新的房间
            if (room == null)
            {
                room = new RoomData(m_roomList.Count + 1, gameroomtype);
                room.joinPlayer(new PlayerData(connId, uid));

                m_roomList.Add(room);
            }

            // 加入房间成功，给客户端回复
            {
                JObject respondJO = new JObject();
                respondJO.Add("tag", tag);
                respondJO.Add("playAction", playAction);
                respondJO.Add("gameroomtype", gameroomtype);
                respondJO.Add("code", (int) TLJCommon.Consts.Code.Code_OK);
                respondJO.Add("roomId", room.getRoomId());

                // 发送给客户端
                PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
            }

            // 检测房间人数是否可以开赛
            if (room.getPlayerDataList().Count == 4)
            {
                room.m_isStartGame = true;
                room.m_roomState = RoomData.RoomState.RoomState_qiangzhu;

                // 设置级牌
                {
                    room.m_levelPokerNum = 2;

                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        room.getPlayerDataList()[i].m_myLevelPoker = room.m_levelPokerNum;
                    }
                }

                JObject respondJO = new JObject();
                respondJO.Add("tag", tag);
                respondJO.Add("playAction", (int) TLJCommon.Consts.PlayAction.PlayAction_StartGame);
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

                            if (!room.getPlayerDataList()[j].m_isOffLine)
                            {
                                PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[j].m_connId, jo2.ToString());
                            }
                        }

                        Thread.Sleep(500);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:doTask_JoinGame异常：" + ex.Message);
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
                            respondJO.Add("code", (int) TLJCommon.Consts.Code.Code_OK);
                            respondJO.Add("roomId", m_roomList[i].getRoomId());

                            // 发送给客户端
                            PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
                        }

                        doTaskPlayerCloseConn(connId);

                        return;
                    }
                }
            }

            // 玩家不在房间内，则退出房间失败，给客户端回复
            {
                JObject respondJO = new JObject();
                respondJO.Add("tag", tag);
                respondJO.Add("playAction", playAction);
                respondJO.Add("code", (int) TLJCommon.Consts.Code.Code_CommonFail);

                // 发送给客户端
                PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:doTask_ExitGame异常：" + ex.Message);
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

            for (int i = 0; i < m_roomList.Count; i++)
            {
                RoomData room = m_roomList[i];
                List<PlayerData> playerDataList = room.getPlayerDataList();

                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                    {
                        bool isQiangZhuSuccess = false;
                        List<TLJCommon.PokerInfo> qiangzhuPokerList = new List<TLJCommon.PokerInfo>();
                        JArray ja = (JArray)JsonConvert.DeserializeObject(jo.GetValue("pokerList").ToString());
                        for (int k = 0; k < ja.Count; k++)
                        {
                            int num = Convert.ToInt32(ja[k]["num"]);
                            int pokerType = Convert.ToInt32(ja[k]["pokerType"]);

                            qiangzhuPokerList.Add(new PokerInfo(num, (Consts.PokerType)pokerType));
                        }

                        if (room.m_zhuangjiaPlayerData == null)
                        {
                            room.m_zhuangjiaPlayerData = playerDataList[j];
                            room.m_qiangzhuPokerList = qiangzhuPokerList;

                            // 设置主牌花色
                            room.m_masterPokerType = (int)room.m_qiangzhuPokerList[0].m_pokerType;

                            isQiangZhuSuccess = true;
                        }
                        else
                        {
                            if (qiangzhuPokerList.Count > room.m_qiangzhuPokerList.Count)
                            {
                                room.m_qiangzhuPokerList.Clear();
                                room.m_zhuangjiaPlayerData = playerDataList[j];
                                room.m_qiangzhuPokerList = qiangzhuPokerList;

                                // 设置主牌花色
                                room.m_masterPokerType = (int)room.m_qiangzhuPokerList[0].m_pokerType;

                                isQiangZhuSuccess = true;
                            }
                            else
                            {
                                if (qiangzhuPokerList[0].m_pokerType > room.m_qiangzhuPokerList[0].m_pokerType)
                                {
                                    room.m_qiangzhuPokerList.Clear();
                                    room.m_zhuangjiaPlayerData = playerDataList[j];
                                    room.m_qiangzhuPokerList = qiangzhuPokerList;

                                    // 设置主牌花色
                                    room.m_masterPokerType = (int)room.m_qiangzhuPokerList[0].m_pokerType;

                                    isQiangZhuSuccess = true;
                                }
                            }
                        }

                        // 通知客户端
                        if(isQiangZhuSuccess)
                        {
                            JObject respondJO = new JObject();
                            respondJO.Add("tag", tag);
                            respondJO.Add("playAction", playAction);
                            respondJO.Add("uid", uid);
                            respondJO.Add("pokerList", jo.GetValue("pokerList"));

                            // 发送给客户端
                            for (int k = 0; k < playerDataList.Count; k++)
                            {
                                PlayService.m_serverUtil.sendMessage(playerDataList[k].m_connId,respondJO.ToString());
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:doTask_QiangZhu异常：" + ex.Message);
        }
    }

    // 玩家抢主结束
    public void doTask_QiangZhuEnd(IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            int playAction = Convert.ToInt32(jo.GetValue("playAction"));

            for (int i = 0; i < m_roomList.Count; i++)
            {
                RoomData room = m_roomList[i];
                List<PlayerData> playerDataList = room.getPlayerDataList();

                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                    {
                        /*
                         * 客户端抢主倒计时结束会一起通知服务端，只处理最先发过来的数据
                         * 如果此前没人抢主，则此人默认为庄家
                         */
                        if (room.m_roomState == RoomData.RoomState.RoomState_qiangzhu)
                        {
                            room.m_roomState = RoomData.RoomState.RoomState_zhuangjiamaidi;

                            if (room.m_zhuangjiaPlayerData == null)
                            {
                                room.m_zhuangjiaPlayerData = playerDataList[j];
                            }

                            {
                                JObject respondJO = new JObject();
                                respondJO.Add("tag", tag);
                                respondJO.Add("playAction", playAction);
                                respondJO.Add("uid", room.m_zhuangjiaPlayerData.m_uid);
                                respondJO.Add("masterPokerType", room.m_masterPokerType);

                                // 发送给客户端
                                for (int k = 0; k < playerDataList.Count; k++)
                                {
                                    if (respondJO.GetValue("isBanker") != null)
                                    {
                                        respondJO.Remove("isBanker");
                                    }

                                    if ((playerDataList[k].m_uid.CompareTo(room.m_zhuangjiaPlayerData.m_uid) == 0) ||
                                        (playerDataList[k].m_uid.CompareTo(room.m_zhuangjiaPlayerData.m_teammateUID) ==
                                         0))
                                    {
                                        playerDataList[k].m_isBanker = 1;
                                        respondJO.Add("isBanker", 1);
                                    }
                                    else
                                    {
                                        playerDataList[k].m_isBanker = 0;
                                        respondJO.Add("isBanker", 0);
                                    }

                                    PlayService.m_serverUtil.sendMessage(playerDataList[k].m_connId,
                                        respondJO.ToString());
                                }

                                // 让该房间庄家埋底
                                callPlayerMaiDi(room);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:doTask_QiangZhu异常：" + ex.Message);
        }
    }

    // 庄家埋底
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
                            room.getDiPokerList().Clear();

                            JArray ja = (JArray) JsonConvert.DeserializeObject(jo.GetValue("diPokerList").ToString());
                            for (int m = 0; m < ja.Count; m++)
                            {
                                int num = Convert.ToInt32(ja[m]["num"]);
                                int pokerType = Convert.ToInt32(ja[m]["pokerType"]);

                                for (int n = playerDataList[j].getPokerList().Count - 1; n >= 0; n--)
                                {
                                    if ((playerDataList[j].getPokerList()[n].m_num == num) &&
                                        ((int) playerDataList[j].getPokerList()[n].m_pokerType == pokerType))
                                    {
                                        // 加到底牌里面
                                        room.getDiPokerList().Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType) pokerType));

                                        // 出的牌从自己的牌堆里删除
                                        {
                                            playerDataList[j].getPokerList().RemoveAt(n);
                                        }

                                        break;
                                    }
                                }
                            }
                        }

                        // 给108所有牌设置权重
                        {
                            PlayRuleUtil.SetAllPokerWeight(room);
                        }

                        // 本房间不可以炒底则直接开始游戏
                        if (!room.m_canChaoDi)
                        {
                            // 开始本房间的比赛
                            doTask_CallPlayerOutPoker(room, data, true);

                            room.m_roomState = RoomData.RoomState.RoomState_gaming;
                        }
                        // 本房间可以炒底则通知玩家炒底
                        else
                        {
                            PlayerData playerData = null;
                            if (room.getPlayerDataList().IndexOf(playerDataList[j]) == 3)
                            {
                                playerData = room.getPlayerDataList()[0];
                            }
                            else
                            {
                                playerData = room.getPlayerDataList()[room.getPlayerDataList().IndexOf(playerDataList[j]) + 1];
                            }

                            if (playerData.m_uid.CompareTo(room.m_zhuangjiaPlayerData.m_uid) == 0)
                            {
                                // 开始本房间的比赛
                                doTask_CallPlayerOutPoker(room, data, true);

                                room.m_roomState = RoomData.RoomState.RoomState_gaming;
                            }
                            else
                            {
                                callPlayerChaoDi(room, playerData.m_uid);
                            }
                            //callPlayerChaoDi(room, cur_playerData.m_uid);
                        }

                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:doTask_MaiDi异常：" + ex.Message);
        }
    }

    // 通知玩家出炒底
    public void callPlayerChaoDi(RoomData room , string uid)
    {
        try
        {
            // 通知
            {
                JObject respondJO;
                {
                    respondJO = new JObject();

                    respondJO.Add("tag", TLJCommon.Consts.Tag_XiuXianChang);
                    respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_CallPlayerChaoDi);
                    respondJO.Add("uid", uid);
                }

                // 给在线的人推送
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    // 推送给客户端
                    if (!room.getPlayerDataList()[i].m_isOffLine)
                    {
                        PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId,respondJO.ToString());
                    }
                }

                // 如果当前出牌的人离线了，单独处理
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    if (room.getPlayerDataList()[i].m_isOffLine)
                    {
                        if (room.getPlayerDataList()[i].m_uid.CompareTo(room.m_curOutPokerPlayer.m_uid) == 0)
                        {
                            trusteeshipLogic(room, respondJO.ToString(), room.getPlayerDataList()[i]);

                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:callPlayerChaoDi异常：" + ex.Message);
        }
    }

    // 玩家炒底
    public void doTask_PlayerChaoDi(IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            int playAction = Convert.ToInt32(jo.GetValue("playAction"));
            int hasPoker = Convert.ToInt32(jo.GetValue("hasPoker"));

            for (int i = 0; i < m_roomList.Count; i++)
            {
                RoomData room = m_roomList[i];
                List<PlayerData> playerDataList = room.getPlayerDataList();

                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                    {
                        // 通知客户端
                        {
                            JObject respondJO = new JObject();
                            respondJO.Add("tag", tag);
                            respondJO.Add("playAction", playAction);
                            respondJO.Add("uid", uid);
                            respondJO.Add("hasPoker", hasPoker);

                            if (hasPoker == 1)
                            {
                                respondJO.Add("pokerList", jo.GetValue("pokerList"));
                            }

                            // 底牌
                            if (hasPoker == 1)
                            {
                                JArray pokerList = new JArray();
                                for (int k = 0; k < room.getDiPokerList().Count; k++)
                                {
                                    JObject temp = new JObject();

                                    int num = room.getDiPokerList()[k].m_num;
                                    int pokerType = (int)room.getDiPokerList()[k].m_pokerType;

                                    temp.Add("num", num);
                                    temp.Add("pokerType", pokerType);

                                    pokerList.Add(temp);

                                    // 把底牌加到庄家牌里面去
                                    playerDataList[j].getPokerList().Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));
                                }

                                respondJO.Add("diPokerList", pokerList);
                            }

                            // 发送给客户端
                            for (int k = 0; k < playerDataList.Count; k++)
                            {
                                PlayService.m_serverUtil.sendMessage(playerDataList[k].m_connId, respondJO.ToString());
                            }
                        }

                        // 此玩家没有炒底，通知下一个人炒底
                        if (hasPoker == 0)
                        {
                            PlayerData playerData = null;
                            if (room.getPlayerDataList().IndexOf(playerDataList[j]) == 3)
                            {
                                playerData = room.getPlayerDataList()[0];
                            }
                            else
                            {
                                playerData = room.getPlayerDataList()[room.getPlayerDataList().IndexOf(playerDataList[j]) + 1];
                            }

                            if (playerData.m_uid.CompareTo(room.m_zhuangjiaPlayerData.m_uid) == 0)
                            {
                                // 开始本房间的比赛
                                doTask_CallPlayerOutPoker(room, data, true);

                                room.m_roomState = RoomData.RoomState.RoomState_gaming;
                            }
                            else
                            {
                                callPlayerChaoDi(room, playerData.m_uid);
                            }
                        }

                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:doTask_PlayerChaoDi异常：" + ex.Message);
        }
    }

    // 推送上一个玩家出的牌,并让下一个玩家出牌
    public void doTask_CallPlayerOutPoker(RoomData room, string data, bool isFirst)
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
                    gameOver(room, data);

                    return;
                }
            }

            // 通知
            {
                JObject respondJO;
                {
                    respondJO = new JObject();

                    respondJO.Add("tag", TLJCommon.Consts.Tag_XiuXianChang);
                    respondJO.Add("playAction", (int) TLJCommon.Consts.PlayAction.PlayAction_CallPlayerOutPoker);
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
                        PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId,
                            respondJO.ToString());
                    }
                }

                // 如果当前出牌的人离线了，单独处理
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    if (room.getPlayerDataList()[i].m_isOffLine)
                    {
                        if (room.getPlayerDataList()[i].m_uid.CompareTo(room.m_curOutPokerPlayer.m_uid) == 0)
                        {
                            trusteeshipLogic(room,respondJO.ToString(), room.getPlayerDataList()[i]);

                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:doTask_CallPlayerOutPoker异常：" + ex.Message);
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
                            JArray ja = (JArray) JsonConvert.DeserializeObject(jo.GetValue("pokerList").ToString());
                            List<TLJCommon.PokerInfo> outPokerList = new List<TLJCommon.PokerInfo>();
                            for (int m = 0; m < ja.Count; m++)
                            {
                                int num = Convert.ToInt32(ja[m]["num"]);
                                int pokerType = Convert.ToInt32(ja[m]["pokerType"]);

                                outPokerList.Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType) pokerType));
                            }

                            // 此人出的牌不是单牌、对子、拖拉机，如果是此回合第一个人出牌则当做甩牌处理
                            LogUtil.getInstance().addDebugLog("出牌类型:"+CheckOutPoker.checkOutPokerType(outPokerList, room.m_levelPokerNum, room.m_masterPokerType).ToString());
                            if(CheckOutPoker.checkOutPokerType(outPokerList, room.m_levelPokerNum,room.m_masterPokerType) == CheckOutPoker.OutPokerType.OutPokerType_ShuaiPai)
                            {
                                if (uid.CompareTo(room.m_curRoundFirstPlayer.m_uid) == 0)
                                {
                                    //检测是否甩牌成功
                                    List<PokerInfo> shuaiPaiPoker = PlayRuleUtil.GetShuaiPaiPoker(room, outPokerList);
                                    bool isSuccess = (shuaiPaiPoker.Count == 0 ? true : false);
                                    PlayService.log.Info("甩牌结果:" + isSuccess);
                                    //   甩牌成功
                                    if (isSuccess)
                                    {
                                        // 从此人牌堆里删除他出的牌
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
                                                        playerDataList[j].m_curOutPokerList.Add(new TLJCommon.PokerInfo(num,(TLJCommon.Consts.PokerType)pokerType));

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
                                    //  甩牌失败
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

                                        // 从此人牌堆里删除他出的牌
                                        {
                                            for (int m = 0; m < shuaiPaiPoker.Count; m++)
                                            {
                                                int num = shuaiPaiPoker[m].m_num;
                                                int pokerType = (int)shuaiPaiPoker[m].m_pokerType;

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

                                        {
                                            JArray ja_outPoker = new JArray();
                                            for (int n = 0; n < shuaiPaiPoker.Count; n++)
                                            {
                                                JObject jo_outPoker = new JObject();
                                                jo_outPoker.Add("num", shuaiPaiPoker[n].m_num);
                                                jo_outPoker.Add("pokerType", (int)shuaiPaiPoker[n].m_pokerType);

                                                ja_outPoker.Add(jo_outPoker);
                                            }

                                            jo["pokerList"] = ja_outPoker;
                                        }
                                    }
                                }
                                else
                                {
                                    for (int m = 0; m < ja.Count; m++)
                                    {
                                        int num = Convert.ToInt32(ja[m]["num"]);
                                        int pokerType = Convert.ToInt32(ja[m]["pokerType"]);

                                        for (int n = playerDataList[j].getPokerList().Count - 1; n >= 0; n--)
                                        {
                                            if ((playerDataList[j].getPokerList()[n].m_num == num) &&
                                                ((int)playerDataList[j].getPokerList()[n].m_pokerType == pokerType))
                                            {
                                                // 加到当前这一轮出牌的牌堆里面
                                                playerDataList[j].m_curOutPokerList.Add(new TLJCommon.PokerInfo(num,(TLJCommon.Consts.PokerType)pokerType));

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
                            // 此人出的牌是单牌、对子、拖拉机,类型没问题，从此人牌堆里删除他出的牌
                            else
                            {
                                for (int m = 0; m < ja.Count; m++)
                                {
                                    int num = Convert.ToInt32(ja[m]["num"]);
                                    int pokerType = Convert.ToInt32(ja[m]["pokerType"]);

                                    for (int n = playerDataList[j].getPokerList().Count - 1; n >= 0; n--)
                                    {
                                        if ((playerDataList[j].getPokerList()[n].m_num == num) &&
                                            ((int) playerDataList[j].getPokerList()[n].m_pokerType == pokerType))
                                        {
                                            // 加到当前这一轮出牌的牌堆里面
                                            playerDataList[j].m_curOutPokerList.Add(new TLJCommon.PokerInfo(num,(TLJCommon.Consts.PokerType) pokerType));

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
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:doTask_ReceivePlayerOutPoker异常：" + ex.Message);
        }
    }
    
    public void doTask_ContinueGame(IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            RoomData room = getRoomByPlayerUid(uid);

            if (room != null)
            {
                bool isOK = true;

                {
                    JObject respondJO = new JObject();
                    respondJO.Add("tag", TLJCommon.Consts.Tag_XiuXianChang);
                    respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_ContinueGame);
                    
                    if (room.getPlayerDataList().Count == 4)
                    {
                        for (int i = 0; i < room.getPlayerDataList().Count; i++)
                        {
                            if (room.getPlayerDataList()[i].m_isOffLine)
                            {
                                isOK = false;
                                break;
                            }
                        }

                        if (isOK)
                        {
                            room.getPlayerDataByUid(uid).m_isContinueGame = true;
                            respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_OK);
                        }
                        else
                        {
                            isOK = false;
                            respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_CommonFail);
                        }
                    }
                    else
                    {
                        isOK = false;
                        respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_CommonFail);
                    }

                    // 发送给客户端
                    PlayService.m_serverUtil.sendMessage(room.getPlayerDataByUid(uid).m_connId, respondJO.ToString());

                    if (!isOK)
                    {
                        // 继续游戏失败，则把此玩家从该房间强制删除，如果此房间没人了，则删除该房间
                        room.deletePlayer(uid);
                        if (room.getPlayerDataList().Count == 0)
                        {
                            m_roomList.Remove(room);
                        }
                    }
                }

                // 检查是否4个人都愿意继续游戏
                if(isOK)
                {
                    bool canStartGame = true;
                    if (room.getPlayerDataList().Count == 4)
                    {
                        for (int i = 0; i < room.getPlayerDataList().Count; i++)
                        {
                            if (!room.getPlayerDataList()[i].m_isContinueGame)
                            {
                                canStartGame = false;
                                break;
                            }
                        }
                    }

                    if (canStartGame)
                    {
                        int levelPokerNum = room.m_levelPokerNum;

                        room.clearData();

                        {
                            room.m_isStartGame = true;
                            room.m_roomState = RoomData.RoomState.RoomState_qiangzhu;

                            // 设置级牌:room.m_levelPokerNum
                            // 这里不用设置了，游戏结束时会自动重新设置

                            JObject respondJO = new JObject();
                            respondJO.Add("tag", TLJCommon.Consts.Tag_XiuXianChang);
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
                            for (int i = 0; i < room.getPlayerDataList().Count; i++)
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

                                    // 分配各自队友
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

                                        if (!room.getPlayerDataList()[j].m_isOffLine)
                                        {
                                            PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[j].m_connId, jo2.ToString());
                                        }
                                    }

                                    Thread.Sleep(500);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                LogUtil.getInstance().addDebugLog("PlayLogic_Relax.doTask_ContinueGame:未找到此人所在房间：" + uid);
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:doTask_ContinueGame异常：" + ex.Message);
        }
    }

    public void doTask_ChangeRoom(IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            RoomData cur_room = getRoomByPlayerUid(uid);
            string gameroomtype = cur_room.m_gameRoomType;

            if (cur_room != null)
            {
                // 检查是否有人想继续游戏，有的话则告诉他失败
                {
                    for (int i = cur_room.getPlayerDataList().Count - 1; i >=0  ; i--)
                    {
                        if (cur_room.getPlayerDataList()[i].m_isContinueGame)
                        {
                            JObject respondJO = new JObject();
                            respondJO.Add("tag", TLJCommon.Consts.Tag_XiuXianChang);
                            respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_ContinueGameFail);
                            respondJO.Add("uid", cur_room.getPlayerDataList()[i].m_uid);

                            // 发送给客户端
                            PlayService.m_serverUtil.sendMessage(cur_room.getPlayerDataList()[i].m_connId, respondJO.ToString());

                            cur_room.getPlayerDataList().RemoveAt(i);
                        }
                    }
                }

                // 从当前房间删除，如果房间人数为空，则删除此房间
                {
                    cur_room.deletePlayer(uid);
                    if (cur_room.getPlayerDataList().Count == 0)
                    {
                        m_roomList.Remove(cur_room);
                    }
                }

                RoomData room = null;
                
                // 在已有的房间寻找可以加入的房间
                for (int i = 0; i < m_roomList.Count; i++)
                {
                    if (m_roomList[i].m_roomState == RoomData.RoomState.RoomState_waiting)
                    {
                        if (m_roomList[i].joinPlayer(new PlayerData(connId, uid)))
                        {
                            room = m_roomList[i];
                            break;
                        }
                    }
                }

                // 当前没有房间可加入的话则创建一个新的房间
                if (room == null)
                {
                    room = new RoomData(m_roomList.Count + 1, gameroomtype);
                    room.joinPlayer(new PlayerData(connId, uid));

                    m_roomList.Add(room);
                }

                // 加入房间成功，给客户端回复
                {
                    JObject respondJO = new JObject();
                    respondJO.Add("tag", tag);
                    respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_ChangeRoom);
                    respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_OK);
                    respondJO.Add("roomId", room.getRoomId());

                    // 发送给客户端
                    PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
                }

                // 检测房间人数是否可以开赛
                if (room.getPlayerDataList().Count == 4)
                {
                    room.m_isStartGame = true;
                    room.m_roomState = RoomData.RoomState.RoomState_qiangzhu;

                    // 设置级牌
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

                                if (!room.getPlayerDataList()[j].m_isOffLine)
                                {
                                    PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[j].m_connId, jo2.ToString());
                                }
                            }

                            Thread.Sleep(500);
                        }
                    }
                }
            }
            else
            {
                LogUtil.getInstance().addDebugLog("PlayLogic_Relax.doTask_ChangeRoom:未找到此人所在房间：" + uid);
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:ChangeRoom异常：" + ex.Message);
        }
    }

    public void doTask_Chat(IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            int content_id = Convert.ToInt32(jo.GetValue("content_id"));

            RoomData room = getRoomByPlayerUid(uid);

            if (room != null)
            {
                // 给在线的人推送
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    // 推送给客户端
                    if (!room.getPlayerDataList()[i].m_isOffLine)
                    {
                        PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, data);
                    }
                }
            }
            else
            {
                LogUtil.getInstance().addDebugLog("PlayLogic_Relax.doTask_Chat:未找到此人所在房间：" + uid);
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:doTask_Chat异常：" + ex.Message);
        }
    }

    public bool doTaskPlayerCloseConn(IntPtr connId)
    {
        try
        {
            for (int i = 0; i < m_roomList.Count; i++)
            {
                RoomData room = m_roomList[i];
                List<PlayerData> playerDataList = room.getPlayerDataList();

                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_connId == connId)
                    {
                        switch (m_roomList[i].m_roomState)
                        {
                            case RoomData.RoomState.RoomState_waiting:
                                {
                                    LogUtil.getInstance().addDebugLog("玩家在本桌满人之前退出：" + playerDataList[j].m_uid);

                                    playerDataList.RemoveAt(j);
                                    if (playerDataList.Count == 0)
                                    {
                                        LogUtil.getInstance().addDebugLog("此房间人数为0，解散房间：" + m_roomList[i].getRoomId());
                                        m_roomList.RemoveAt(i);
                                    }
                                }
                                break;

                            case RoomData.RoomState.RoomState_qiangzhu:
                                {
                                    LogUtil.getInstance().addDebugLog("玩家在抢主阶段退出：" + playerDataList[j].m_uid);

                                    playerDataList[j].m_isOffLine = true;
                                }
                                break;

                            case RoomData.RoomState.RoomState_zhuangjiamaidi:
                                {
                                    LogUtil.getInstance().addDebugLog("玩家在庄家埋底阶段退出：" + playerDataList[j].m_uid);

                                    playerDataList[j].m_isOffLine = true;

                                    if (m_roomList[i].m_zhuangjiaPlayerData.m_uid.CompareTo(playerDataList[j].m_uid) == 0)
                                    {
                                        JObject data = new JObject();
                                        data.Add("tag", TLJCommon.Consts.Tag_XiuXianChang);
                                        data.Add("uid", m_roomList[i].m_zhuangjiaPlayerData.m_uid);
                                        data.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_MaiDi);
                                        
                                        List<TLJCommon.PokerInfo> myOutPokerList = new List<TLJCommon.PokerInfo>();

                                        // 自己出的牌
                                        {
                                            JArray ja = new JArray();
                                            for (int k = 1; k <= 8; k++)
                                            {
                                                JObject jo = new JObject();
                                                jo.Add("num", playerDataList[j].getPokerList()[playerDataList[j].getPokerList().Count - k].m_num);
                                                jo.Add("pokerType", (int)playerDataList[j].getPokerList()[playerDataList[j].getPokerList().Count - k].m_pokerType);

                                                ja.Add(jo);
                                            }

                                            data.Add("diPokerList", ja);
                                        }

                                        LogUtil.getInstance().addDebugLog("托管：帮" + playerDataList[j].m_uid + "埋底:" + data.ToString());
                                        doTask_MaiDi(playerDataList[j].m_connId, data.ToString());
                                    }
                                }
                                break;

                            case RoomData.RoomState.RoomState_fanzhu:
                                {
                                    LogUtil.getInstance().addDebugLog("玩家在反主阶段退出：" + playerDataList[j].m_uid);

                                    playerDataList[j].m_isOffLine = true;
                                }
                                break;

                            case RoomData.RoomState.RoomState_chaodi:
                                {
                                    LogUtil.getInstance().addDebugLog("玩家在抄底阶段退出：" + playerDataList[j].m_uid);

                                    playerDataList[j].m_isOffLine = true;
                                }
                                break;

                            case RoomData.RoomState.RoomState_gaming:
                                {
                                    LogUtil.getInstance().addDebugLog("玩家在游戏中退出：" + playerDataList[j].m_uid);
                                    playerDataList[j].m_isOffLine = true;

                                    // 如果当前房间正好轮到此人出牌
                                    if (m_roomList[i].m_curOutPokerPlayer.m_uid.CompareTo(playerDataList[j].m_uid) == 0)
                                    {
                                        trusteeshipLogic(m_roomList[i], playerDataList[j]);
                                    }
                                }
                                break;

                            case RoomData.RoomState.RoomState_end:
                                {
                                    LogUtil.getInstance().addDebugLog("玩家在本桌打完后退出：" + playerDataList[j].m_uid);

                                    playerDataList.RemoveAt(j);
                                    if (playerDataList.Count == 0)
                                    {
                                        LogUtil.getInstance().addDebugLog("此房间人数为0，解散房间：" + m_roomList[i].getRoomId());
                                        m_roomList.Remove(room);
                                    }
                                    else
                                    {
                                        // 检查此房间内是否有人想继续游戏，有的话则告诉他失败
                                        {
                                            for (int k = playerDataList.Count - 1; k >= 0; k--)
                                            {
                                                if (playerDataList[k].m_isContinueGame)
                                                {
                                                    {
                                                        JObject respondJO = new JObject();
                                                        respondJO.Add("tag", TLJCommon.Consts.Tag_XiuXianChang);
                                                        respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_ContinueGameFail);
                                                        respondJO.Add("uid", playerDataList[k].m_uid);

                                                        // 发送给客户端
                                                        PlayService.m_serverUtil.sendMessage(playerDataList[k].m_connId, respondJO.ToString());
                                                    }

                                                    playerDataList.RemoveAt(k);
                                                }
                                            }
                                        }

                                        // 如果房间人数为空，则删除此房间
                                        {
                                            if (playerDataList.Count == 0)
                                            {
                                                m_roomList.Remove(room);
                                            }
                                        }
                                    }
                                }
                                break;

                            default:
                                {
                                    LogUtil.getInstance().addDebugLog("玩家在未知阶段退出：" + playerDataList[j].m_uid);

                                    playerDataList.RemoveAt(j);
                                    if (playerDataList.Count == 0)
                                    {
                                        LogUtil.getInstance().addDebugLog("PlayLogic_Relax:此房间人数为0，解散房间：" + m_roomList[i].getRoomId());
                                        m_roomList.RemoveAt(i);
                                    }
                                }
                                break;
                        }
                        
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:doTaskPlayerCloseConn异常：" + ex.Message);
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
            respondJO.Add("playAction", (int) TLJCommon.Consts.PlayAction.PlayAction_MaiDi);
            respondJO.Add("uid", room.m_zhuangjiaPlayerData.m_uid);

            // 底牌
            {
                JArray pokerList = new JArray();
                for (int i = 0; i < room.getDiPokerList().Count; i++)
                {
                    JObject temp = new JObject();

                    int num = room.getDiPokerList()[i].m_num;
                    int pokerType = (int) room.getDiPokerList()[i].m_pokerType;

                    temp.Add("num", num);
                    temp.Add("pokerType", pokerType);

                    pokerList.Add(temp);

                    // 把底牌加到庄家牌里面去
                    room.m_zhuangjiaPlayerData.getPokerList()
                        .Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType) pokerType));
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
                else
                {
                    // 如果此时庄家离线
                    if (room.m_zhuangjiaPlayerData.m_uid.CompareTo(room.getPlayerDataList()[i].m_uid) == 0)
                    {

                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:callPlayerMaiDi异常：" + ex.Message);
        }
    }

    // 游戏结束
    void gameOver(RoomData room, string data)
    {
        try
        {
            //LogUtil.getInstance().addDebugLog("比赛结束，解散该房间：" + room.getRoomId());
            LogUtil.getInstance().addDebugLog("PlayLogic_Relax:比赛结束,roomid = :" + room.getRoomId());

            // 逻辑处理
            {
                // 闲家赢
                if (room.m_getAllScore >= 80)
                {
                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        if (room.getPlayerDataList()[i].m_isBanker == 0)
                        {
                            ++room.getPlayerDataList()[i].m_myLevelPoker;
                            if (room.getPlayerDataList()[i].m_myLevelPoker == 15)
                            {
                                room.getPlayerDataList()[i].m_myLevelPoker = 2;
                            }

                            room.m_levelPokerNum = room.getPlayerDataList()[i].m_myLevelPoker;
                        }
                    }
                }
                // 庄家赢
                else
                {
                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        if (room.getPlayerDataList()[i].m_isBanker == 1)
                        {
                            ++room.getPlayerDataList()[i].m_myLevelPoker;
                            if (room.getPlayerDataList()[i].m_myLevelPoker == 15)
                            {
                                room.getPlayerDataList()[i].m_myLevelPoker = 2;
                            }

                            room.m_levelPokerNum = room.getPlayerDataList()[i].m_myLevelPoker;
                        }
                    }
                }
            }

            JObject jo = JObject.Parse(data);

            // 通知
            {
                JObject respondJO;
                {
                    respondJO = new JObject();

                    respondJO.Add("tag", TLJCommon.Consts.Tag_XiuXianChang);
                    respondJO.Add("playAction", (int) TLJCommon.Consts.PlayAction.PlayAction_GameOver);
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
                        PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId,
                            respondJO.ToString());
                    }
                }
            }

            room.m_roomState = RoomData.RoomState.RoomState_end;

            // 检查是否删除该房间
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

                if (isRemove)
                {
                    m_roomList.Remove(room);
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:gameOver异常：" + ex.Message);
        }
    }

    RoomData getRoomByPlayerUid(string uid)
    {
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

                    return room;
                }
            }
        }

        return room;
    }

    // 托管逻辑
    void trusteeshipLogic(RoomData room, string jsonData, PlayerData playerData)
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
                    backData.Add("playAction", (int) TLJCommon.Consts.PlayAction.PlayAction_PlayerOutPoker);

                    // 自己出的牌
                    {
                        // 任意出
                        if (playerData.m_uid.CompareTo(room.m_curRoundFirstPlayer.m_uid) == 0)
                        {
                            int num = playerData.getPokerList()[playerData.getPokerList().Count - 1].m_num;
                            int pokerType = (int)playerData.getPokerList()[playerData.getPokerList().Count - 1].m_pokerType;

                            JArray jarray = new JArray();
                            {
                                JObject temp = new JObject();
                                temp.Add("num", num);
                                temp.Add("pokerType", pokerType);
                                jarray.Add(temp);
                            }
                            backData.Add("pokerList", jarray);
                        }
                        // 跟牌
                        else
                        {
                            List<TLJCommon.PokerInfo> listPoker = PlayRuleUtil.GetPokerWhenTuoGuan(room.m_curRoundFirstPlayer.m_curOutPokerList, playerData.getPokerList(), room.m_levelPokerNum, room.m_masterPokerType);
                            JArray jarray = new JArray();
                            for (int i = 0; i < listPoker.Count; i++)
                            {
                                int num = listPoker[i].m_num;
                                int pokerType = (int)listPoker[i].m_pokerType;
                                
                                {
                                    JObject temp = new JObject();
                                    temp.Add("num", num);
                                    temp.Add("pokerType", pokerType);
                                    jarray.Add(temp);
                                }
                            }
                            backData.Add("pokerList", jarray);
                        }
                    }

                    LogUtil.getInstance().addDebugLog("托管出牌：" + playerData.m_uid);
                    doTask_ReceivePlayerOutPoker(playerData.m_connId, backData.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:trusteeshipLogic异常1：" + ex.Message);
        }
    }

    // 托管逻辑
    void trusteeshipLogic(RoomData room, PlayerData playerData)
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
                    backData.Add("playAction", (int) TLJCommon.Consts.PlayAction.PlayAction_PlayerOutPoker);

                    // 自己出的牌
                    {
                        // 任意出
                        if (playerData.m_uid.CompareTo(room.m_curRoundFirstPlayer.m_uid) == 0)
                        {
                            int num = playerData.getPokerList()[playerData.getPokerList().Count - 1].m_num;
                            int pokerType = (int)playerData.getPokerList()[playerData.getPokerList().Count - 1].m_pokerType;

                            JArray jarray = new JArray();
                            {
                                JObject temp = new JObject();
                                temp.Add("num", num);
                                temp.Add("pokerType", pokerType);
                                jarray.Add(temp);
                            }
                            backData.Add("pokerList", jarray);
                        }
                        // 跟牌
                        else
                        {
                            List<TLJCommon.PokerInfo> listPoker = PlayRuleUtil.GetPokerWhenTuoGuan(room.m_curRoundFirstPlayer.m_curOutPokerList, playerData.getPokerList(), room.m_levelPokerNum, room.m_masterPokerType);
                            JArray jarray = new JArray();
                            for (int i = 0; i < listPoker.Count; i++)
                            {
                                int num = listPoker[i].m_num;
                                int pokerType = (int)listPoker[i].m_pokerType;

                                {
                                    JObject temp = new JObject();
                                    temp.Add("num", num);
                                    temp.Add("pokerType", pokerType);
                                    jarray.Add(temp);
                                }
                            }
                            backData.Add("pokerList", jarray);
                        }
                    }

                    LogUtil.getInstance().addDebugLog("托管出牌：" + playerData.m_uid);
                    doTask_ReceivePlayerOutPoker(playerData.m_connId, backData.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("PlayLogic_Relax:trusteeshipLogic异常2：" + ex.Message);
        }
    }
}