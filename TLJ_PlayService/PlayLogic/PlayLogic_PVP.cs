using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TLJCommon;
using TLJ_PlayService;
using static TLJCommon.Consts;

class PlayLogic_PVP: GameBase
{
    static PlayLogic_PVP s_playLogic_PVP = null;

    List<RoomData> m_roomList = new List<RoomData>();

    string m_tag = TLJCommon.Consts.Tag_JingJiChang;
    string m_logFlag = "PlayLogic_PVP";

    public static PlayLogic_PVP getInstance()
    {
        if (s_playLogic_PVP == null)
        {
            s_playLogic_PVP = new PlayLogic_PVP();
        }

        return s_playLogic_PVP;
    }

    public int getPlayerCount()
    {
        int count = 0;
        for (int i = 0; i < m_roomList.Count; i++)
        {
            List<PlayerData> playerDataList = m_roomList[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (!playerDataList[j].isOffLine())
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
                case (int) TLJCommon.Consts.PlayAction.PlayAction_JoinGame:
                {
                    doTask_JoinGame(connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_WaitMatchTimeOut:
                {
                    GameLogic.doTask_WaitMatchTimeOut(this,connId, data);
                }
                break;

                case (int) TLJCommon.Consts.PlayAction.PlayAction_ExitPVP:
                {
                    doTask_ExitPVP(connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_ExitGame:
                {
                    GameLogic.doTask_ExitGame(this, connId, data);
                }
                break;

                case (int) TLJCommon.Consts.PlayAction.PlayAction_QiangZhu:
                {
                    GameLogic.doTask_QiangZhu(this, connId, data);
                }
                break;

                case (int) TLJCommon.Consts.PlayAction.PlayAction_MaiDi:
                {
                    GameLogic.doTask_MaiDi(this, connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_PlayerChaoDi:
                {
                    GameLogic.doTask_PlayerChaoDi(this, connId, data);
                }
                break;

                case (int) TLJCommon.Consts.PlayAction.PlayAction_PlayerOutPoker:
                {
                    GameLogic.doTask_ReceivePlayerOutPoker(this, connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_ContinueGame:
                {
                    GameLogic.doTask_ContinueGame(this,connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_ChangeRoom:
                {
                    GameLogic.doTask_ChangeRoom(this,connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_Chat:
                {
                    GameLogic.doTask_Chat(this, connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_SetTuoGuanState:
                {
                    GameLogic.doTask_SetTuoGuanState(this, connId, data);
                }
                break;
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().writeLogToLocalNow(m_logFlag + "----" + ".OnReceive()异常：" + ex.Message);
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
            if (GameUtil.checkPlayerIsInRoom(uid))
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

            lock (m_roomList)
            {
                // 在已有的房间寻找可以加入的房间
                for (int i = 0; i < m_roomList.Count; i++)
                {
                    //if (gameroomtype.CompareTo(m_roomList[i].m_gameRoomType) == 0)
                    if ((gameroomtype.CompareTo(m_roomList[i].m_gameRoomType) == 0) && (1 == m_roomList[i].m_rounds_pvp) && (m_roomList[i].m_roomState == RoomState.RoomState_waiting))
                    {
                        if (m_roomList[i].joinPlayer(new PlayerData(connId, uid, false, gameroomtype)))
                        {
                            room = m_roomList[i];
                            break;
                        }
                    }
                }

                // 当前没有房间可加入的话则创建一个新的房间
                if (room == null)
                {
                    room = new RoomData(this, m_roomList.Count + 1, gameroomtype);
                    room.joinPlayer(new PlayerData(connId, uid, false, gameroomtype));

                    m_roomList.Add(room);

                    LogUtil.getInstance().addDebugLog("新建比赛场房间：" + room.getRoomId());
                }
            }

            // 扣除报名费
            {
                string baomingfei = PVPGameRoomDataScript.getInstance().getDataByRoomType(gameroomtype).baomingfei;
                if (baomingfei.CompareTo("0") != 0)
                {
                    List<string> tempList = new List<string>();
                    CommonUtil.splitStr(baomingfei, tempList,':');
                    Request_ChangeUserWealth.doRequest(uid, int.Parse(tempList[0]), -int.Parse(tempList[1]));
                }
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
            GameLogic.checkRoomStartGame(room, m_tag, true);
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_JoinGame异常：" + ex.Message);
        }
    }

    void doTask_ExitPVP(IntPtr connId, string data)
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
                        //// 还回报名费
                        //if (m_roomList[i].m_roomState == RoomData.RoomState.RoomState_waiting)
                        //{
                        //    PVPGameRoomData pvpGameRoomData = PVPGameRoomDataScript.getInstance().getDataByRoomType(m_roomList[i].m_gameRoomType);
                        //    string baomingfei = pvpGameRoomData.baomingfei;

                        //    if (baomingfei.CompareTo("0") != 0)
                        //    {
                        //        List<string> tempList = new List<string>();
                        //        CommonUtil.splitStr(baomingfei, tempList, ':');

                        //        int id = int.Parse(tempList[0]);
                        //        int num = int.Parse(tempList[1]);

                        //        string content = pvpGameRoomData.gameroomname + "报名费返还：";
                        //        if (id == 1)
                        //        {
                        //            content += ("金币x" + num);
                        //        }
                        //        else
                        //        {
                        //            content += ("蓝钻石x" + num);
                        //        }

                        //        Request_SendMailToUser.doRequest(uid, "报名费返还", content, baomingfei);
                        //    }
                        //}

                        // 给客户端回复
                        {
                            JObject respondJO = new JObject();
                            respondJO.Add("tag", tag);
                            respondJO.Add("playAction", playAction);
                            respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_OK);
                            respondJO.Add("gameroomtype", m_roomList[i].m_gameRoomType);
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
                respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_CommonFail);

                // 发送给客户端
                PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_ExitPVP异常：" + ex.Message);
        }
    }

    //------------------------------------------------------------------以上内容休闲场和PVP逻辑一样--------------------------------------------------------------

    public override List<RoomData> getRoomList()
    {
        return m_roomList;
    }

    public override string getTag()
    {
        return m_tag;
    }

    public override bool doTaskPlayerCloseConn(IntPtr connId)
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
                        //// 记录逃跑数据
                        //if ((m_roomList[i].m_roomState != RoomState.RoomState_waiting) &&
                        //    (m_roomList[i].m_roomState != RoomState.RoomState_end))
                        //{
                        //    Request_RecordUserGameData.doRequest(room.getPlayerDataList()[i].m_uid, room.m_gameRoomType, (int)TLJCommon.Consts.GameAction.GameAction_Run);
                        //}

                        switch (m_roomList[i].m_roomState)
                        {
                            case RoomState.RoomState_waiting:
                                {
                                    if (!playerDataList[j].isOffLine())
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在本桌满人之前退出：" + playerDataList[j].m_uid);

                                        // 还回报名费
                                        {
                                            PVPGameRoomData pvpGameRoomData = PVPGameRoomDataScript.getInstance().getDataByRoomType(room.m_gameRoomType);
                                            string baomingfei = pvpGameRoomData.baomingfei;

                                            if (baomingfei.CompareTo("0") != 0)
                                            {
                                                List<string> tempList = new List<string>();
                                                CommonUtil.splitStr(baomingfei, tempList, ':');

                                                int id = int.Parse(tempList[0]);
                                                int num = int.Parse(tempList[1]);

                                                string content = pvpGameRoomData.gameroomname + "报名费返还：";
                                                if (id == 1)
                                                {
                                                    content += ("金币x" + num);
                                                }
                                                else
                                                {
                                                    content += ("蓝钻石x" + num);
                                                }

                                                Request_SendMailToUser.doRequest(playerDataList[j].m_uid, "报名费返还", content, baomingfei);
                                            }
                                        }

                                        playerDataList.RemoveAt(j);

                                        GameUtil.checkAllOffLine(room);

                                        if (GameUtil.checkRoomNonePlayer(room))
                                        {
                                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + room);
                                            GameLogic.removeRoom(this, room);
                                        }
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            case RoomState.RoomState_qiangzhu:
                                {
                                    if (!playerDataList[j].isOffLine())
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在抢主阶段退出：" + playerDataList[j].m_uid);

                                        playerDataList[j].setIsOffLine(true);

                                        GameUtil.checkAllOffLine(room);
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            case RoomState.RoomState_zhuangjiamaidi:
                                {
                                    if (!playerDataList[j].isOffLine())
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在庄家埋底阶段退出：" + playerDataList[j].m_uid);

                                        playerDataList[j].setIsOffLine(true);

                                        GameUtil.checkAllOffLine(room);

                                        //TrusteeshipLogic.trusteeshipLogic_MaiDi(this, room, playerDataList[j]);
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            //case RoomData.RoomState.RoomState_fanzhu:
                            //    {
                            //        LogUtil.getInstance().addDebugLog("玩家在反主阶段退出：" + playerDataList[j].m_uid);

                            //        playerDataList[j].m_isOffLine = true;
                            //    }
                            //    break;

                            case RoomState.RoomState_chaodi:
                                {
                                    if (!playerDataList[j].isOffLine())
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在抄底阶段退出：" + playerDataList[j].m_uid);

                                        playerDataList[j].setIsOffLine(true);

                                        GameUtil.checkAllOffLine(room);

                                        //TrusteeshipLogic.trusteeshipLogic_ChaoDi(this, room, playerDataList[j]);
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            case RoomState.RoomState_othermaidi:
                                {
                                    if (!playerDataList[j].isOffLine())
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在Other埋底阶段退出：" + playerDataList[j].m_uid);

                                        playerDataList[j].setIsOffLine(true);

                                        GameUtil.checkAllOffLine(room);

                                        //TrusteeshipLogic.trusteeshipLogic_MaiDi(this, room, playerDataList[j]);
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            case RoomState.RoomState_gaming:
                                {
                                    if (!playerDataList[j].isOffLine())
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在游戏中退出：" + playerDataList[j].m_uid);
                                        playerDataList[j].setIsOffLine(true);

                                        GameUtil.checkAllOffLine(room);

                                        // 如果当前房间正好轮到此人出牌
                                        if (m_roomList[i].m_curOutPokerPlayer.m_uid.CompareTo(playerDataList[j].m_uid) == 0)
                                        {
                                            //TrusteeshipLogic.trusteeshipLogic_OutPoker(this, m_roomList[i], playerDataList[j]);
                                        }
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            case RoomState.RoomState_end:
                                {
                                    if (!playerDataList[j].isOffLine())
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在本桌打完后退出：" + playerDataList[j].m_uid);

                                        playerDataList.RemoveAt(j);
                                        if (playerDataList.Count == 0)
                                        {
                                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + m_roomList[i].getRoomId());
                                            GameLogic.removeRoom(this, room);
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
                                                            respondJO.Add("tag", m_tag);
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
                                                if (GameUtil.checkRoomNonePlayer(room))
                                                {
                                                    LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + room.getRoomId());
                                                    GameLogic.removeRoom(this, room);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            default:
                                {
                                    if (!playerDataList[j].isOffLine())
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在未知阶段退出：" + playerDataList[j].m_uid);

                                        playerDataList.RemoveAt(j);

                                        if (GameUtil.checkRoomNonePlayer(room))
                                        {
                                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + room.getRoomId());
                                            GameLogic.removeRoom(this, room);
                                        }
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
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
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTaskPlayerCloseConn异常：" + ex.Message);
        }

        return false;
    }

    // 游戏结束
    public override void gameOver(RoomData now_room)
    {
        try
        {
            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":比赛结束,roomid = :" + now_room.getRoomId());

            now_room.m_roomState = RoomState.RoomState_end;

            // 计算每个玩家的金币（积分）
            GameUtil.setPlayerScore(now_room,true);

            List<PlayerData> winPlayerList = new List<PlayerData>();

            string gameRoomType = now_room.m_gameRoomType;
            int rounds_pvp = now_room.m_rounds_pvp;

            
            bool isContiune = true;         // 是否打到最后一轮            
            bool isJueShengJu = false;      // 下一局是否是决胜局

            {
                List<string> listTemp = new List<string>();
                CommonUtil.splitStr(now_room.m_gameRoomType, listTemp, '_');
                int jirenchang = int.Parse(listTemp[2]);
                switch (jirenchang)
                {
                    case 8:
                        {
                            if (rounds_pvp == 3)
                            {
                                isContiune = false;
                            }
                            // 决胜局
                            else if (rounds_pvp == 2)
                            {
                                isJueShengJu = true;
                            }
                        }
                        break;

                    case 16:
                        {
                            if (rounds_pvp == 4)
                            {
                                isContiune = false;
                            }
                            // 决胜局
                            else if (rounds_pvp == 3)
                            {
                                isJueShengJu = true;
                            }
                        }
                        break;

                    case 32:
                        {
                            if (rounds_pvp == 5)
                            {
                                isContiune = false;
                            }
                            // 决胜局
                            else if (rounds_pvp == 4)
                            {
                                isJueShengJu = true;
                            }
                        }
                        break;
                }
            }

            // 逻辑处理
            {
                // 闲家赢
                if (now_room.m_getAllScore >= 80)
                {
                    for (int i = 0; i < now_room.getPlayerDataList().Count; i++)
                    {
                        if (now_room.getPlayerDataList()[i].m_isBanker == 0)
                        {
                            ++now_room.getPlayerDataList()[i].m_myLevelPoker;
                            if (now_room.getPlayerDataList()[i].m_myLevelPoker == 15)
                            {
                                now_room.getPlayerDataList()[i].m_myLevelPoker = 2;
                            }

                            now_room.m_levelPokerNum = now_room.getPlayerDataList()[i].m_myLevelPoker;

                            winPlayerList.Add(now_room.getPlayerDataList()[i]);

                            // 提交任务
                            if (!now_room.getPlayerDataList()[i].m_isAI)
                            {
                                Request_ProgressTask.doRequest(now_room.getPlayerDataList()[i].m_uid, 203);
                                Request_ProgressTask.doRequest(now_room.getPlayerDataList()[i].m_uid, 212);
                            }

                            // 记录胜利次数数据
                            {
                                Request_RecordUserGameData.doRequest(now_room.getPlayerDataList()[i].m_uid, now_room.m_gameRoomType,(int)TLJCommon.Consts.GameAction.GameAction_Win);
                            }

                            // 分数在原来的基础上减半，防止玩家之间分数差距太大
                            {
                                now_room.getPlayerDataList()[i].m_score /= 2;
                            }
                        }
                        // 加入淘汰人员列表里
                        else
                        {
                            PVPChangCiUtil.getInstance().addPlayerToThere(now_room.m_gameRoomType, now_room.getPlayerDataList()[i]);

                            //// 分数在原来的基础上减10000，防止比晋级的人分数高，影响排名
                            //if (!isJueShengJu && isContiune)
                            //{
                            //    now_room.getPlayerDataList()[i].m_score -= 10000;
                            //}
                        }
                    }
                }
                // 庄家赢
                else
                {
                    for (int i = 0; i < now_room.getPlayerDataList().Count; i++)
                    {
                        if (now_room.getPlayerDataList()[i].m_isBanker == 1)
                        {
                            ++now_room.getPlayerDataList()[i].m_myLevelPoker;
                            if (now_room.getPlayerDataList()[i].m_myLevelPoker == 15)
                            {
                                now_room.getPlayerDataList()[i].m_myLevelPoker = 2;
                            }

                            now_room.m_levelPokerNum = now_room.getPlayerDataList()[i].m_myLevelPoker;

                            winPlayerList.Add(now_room.getPlayerDataList()[i]);

                            // 提交任务
                            if (!now_room.getPlayerDataList()[i].m_isAI)
                            {
                                Request_ProgressTask.doRequest(now_room.getPlayerDataList()[i].m_uid, 212);
                            }

                            // 记录胜利次数数据
                            {
                                Request_RecordUserGameData.doRequest(now_room.getPlayerDataList()[i].m_uid, now_room.m_gameRoomType,(int)TLJCommon.Consts.GameAction.GameAction_Win);
                            }

                            // 分数在原来的基础上减半，防止玩家之间分数差距太大
                            {
                                now_room.getPlayerDataList()[i].m_score /= 2;
                            }
                        }
                        // 加入淘汰人员列表里
                        else
                        {
                            PVPChangCiUtil.getInstance().addPlayerToThere(now_room.m_gameRoomType, now_room.getPlayerDataList()[i]);

                            //// 分数在原来的基础上减10000，防止比晋级的人分数高，影响排名
                            //if (!isJueShengJu && isContiune)
                            //{
                            //    now_room.getPlayerDataList()[i].m_score -= 10000;
                            //}
                        }
                    }
                }
            }

            for (int i = 0; i < winPlayerList.Count; i++)
            {
                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":胜利的人：" + winPlayerList[i].m_uid + "  isBanker:" + winPlayerList[i].m_isBanker);
            }

            // 通知
            {
                JObject respondJO;
                {
                    respondJO = new JObject();

                    respondJO.Add("tag", m_tag);
                    respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_GameOver);
                    respondJO.Add("getAllScore", now_room.m_getAllScore);
                    respondJO.Add("isBankerWin", now_room.m_getAllScore >= 80 ? 0 : 1);
                }

                // 给在线的人推送
                for (int i = 0; i < now_room.getPlayerDataList().Count; i++)
                {
                    // 推送给客户端
                    if (!now_room.getPlayerDataList()[i].isOffLine())
                    {
                        if (!(now_room.getPlayerDataList()[i].m_isAI))
                        {
                            if (respondJO.GetValue("score") != null)
                            {
                                respondJO.Remove("score");
                            }

                            respondJO.Add("score", now_room.getPlayerDataList()[i].m_score);

                            PlayService.m_serverUtil.sendMessage(now_room.getPlayerDataList()[i].m_connId, respondJO.ToString());
                        }
                    }
                    else
                    {
                        if (!(now_room.getPlayerDataList()[i].m_isAI))
                        {
                            // 记录逃跑数据
                            Request_RecordUserGameData.doRequest(now_room.getPlayerDataList()[i].m_uid, now_room.m_gameRoomType, (int)TLJCommon.Consts.GameAction.GameAction_Run);
                        }
                    }

                    if (now_room.getPlayerDataList()[i].m_isAI)
                    {
                        AIDataScript.getInstance().backOneAI(now_room.getPlayerDataList()[i].m_uid);
                    }

                    // 告诉数据库服务器该玩家打完一局
                    {
                        Request_GameOver.doRequest(now_room.getPlayerDataList()[i].m_uid, now_room.m_gameRoomType);
                    }
                }
            }

            if (isJueShengJu)
            {
                jueshengju(now_room);

                return;
            }

            // 进入新房间，准备开始下一局
            if (isContiune)
            {
                // 删除该房间
                {
                    LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":本局打完，强制解散该房间：" + now_room.getRoomId());
                    GameLogic.removeRoom(this, now_room);
                }

                for (int i = 0; i < winPlayerList.Count; i++)
                {
                    if (!winPlayerList[i].isOffLine())
                    {
                        {
                            RoomData room = null;

                            // 玩家数据清理
                            {
                                winPlayerList[i].m_isBanker = 0;
                            }

                            lock (m_roomList)
                            {
                                // 在已有的房间寻找可以加入的房间
                                for (int j = 0; j < m_roomList.Count; j++)
                                {
                                    if ((gameRoomType.CompareTo(m_roomList[j].m_gameRoomType) == 0) && ((rounds_pvp + 1) == m_roomList[j].m_rounds_pvp) && (m_roomList[j].m_roomState == RoomState.RoomState_waiting))
                                    {
                                        if (m_roomList[j].joinPlayer(winPlayerList[i]))
                                        {
                                            room = m_roomList[j];
                                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":找到新房间：" + room.getRoomId());

                                            break;
                                        }
                                    }
                                }

                                // 当前没有房间可加入的话则创建一个新的房间
                                if (room == null)
                                {
                                    room = new RoomData(this, m_roomList.Count + 1, gameRoomType);
                                    room.joinPlayer(winPlayerList[i]);
                                    room.m_rounds_pvp = rounds_pvp + 1;

                                    m_roomList.Add(room);

                                    LogUtil.getInstance().addDebugLog("新建比赛场房间：" + room.getRoomId());

                                    LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":创建新房间：" + room.getRoomId());
                                }
                            }

                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":该新房间人数：" + room.getPlayerDataList().Count);

                            if (room.getPlayerDataList().Count == 4)
                            {
                                // 延迟3秒开赛
                                Thread.Sleep(3000);

                                // 检测房间人数是否可以开赛
                                GameLogic.checkRoomStartGame(room, m_tag, true);
                            }
                        }
                    }
                }
            }
            // 所有局数已经打完
            else
            {
                {
                    for (int i = 0; i < winPlayerList.Count; i++)
                    {
                        // 加入淘汰人员列表里
                        PVPChangCiUtil.getInstance().addPlayerToThere(gameRoomType, winPlayerList[i]);
                    }
                }

                PVPRoomPlayerList curPVPRoomPlayerList = PVPChangCiUtil.getInstance().getPVPRoomPlayerListByUid(winPlayerList[0].m_uid);
                if (curPVPRoomPlayerList != null)
                {
                    PVPChangCiUtil.getInstance().sortPVPRoomPlayerList(curPVPRoomPlayerList);
                    PVPChangCiUtil.getInstance().deletePVPRoomPlayerList(curPVPRoomPlayerList);

                    GameUtil.setPVPReward(curPVPRoomPlayerList);

                    string gameroomname = PVPGameRoomDataScript.getInstance().getDataByRoomType(curPVPRoomPlayerList.m_gameRoomType).gameroomname;

                    // 用邮件给他们发奖励
                    for (int i = 0; i < curPVPRoomPlayerList.m_playerList.Count; i++)
                    {
                        LogUtil.getInstance().addDebugLog(m_tag + "----名次" + (i + 1) + "  id = " + curPVPRoomPlayerList.m_playerList[i].m_uid + " 分数为" + curPVPRoomPlayerList.m_playerList[i].m_score + "  奖励为:" + curPVPRoomPlayerList.m_playerList[i].m_pvpReward);

                        if (curPVPRoomPlayerList.m_playerList[i].m_pvpReward.CompareTo("") != 0)
                        {
                            string title = gameroomname + "奖励";
                            string content = "恭喜您在" + gameroomname + "获得第" + curPVPRoomPlayerList.m_playerList[i].m_rank + "名，为您送上专属奖励";
                            Request_SendMailToUser.doRequest(curPVPRoomPlayerList.m_playerList[i].m_uid, title, content, curPVPRoomPlayerList.m_playerList[i].m_pvpReward);
                        }

                        // 提交任务
                        if (i == 0)
                        {
                            Request_ProgressTask.doRequest(curPVPRoomPlayerList.m_playerList[i].m_uid, 214);
                        }
                    }
                }
                else
                {
                    LogUtil.getInstance().addDebugLog(m_tag + "----curPVPRoomPlayerList == null");
                }

                {
                    JObject respondJO;
                    {
                        respondJO = new JObject();

                        respondJO.Add("tag", m_tag);
                        respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_PVPGameOver);
                    }

                    // 给在线的人推送
                    for (int i = 0; i < now_room.getPlayerDataList().Count; i++)
                    {
                        if (!now_room.getPlayerDataList()[i].isOffLine())
                        {
                            if (respondJO.GetValue("mingci") != null)
                            {
                                respondJO.Remove("mingci");
                            }

                            if (respondJO.GetValue("pvpreward") != null)
                            {
                                respondJO.Remove("pvpreward");
                            }

                            respondJO.Add("mingci", now_room.getPlayerDataList()[i].m_rank);
                            respondJO.Add("pvpreward", now_room.getPlayerDataList()[i].m_pvpReward);

                            PlayService.m_serverUtil.sendMessage(now_room.getPlayerDataList()[i].m_connId, respondJO.ToString());
                        }
                    }
                }

                // 删除该房间
                {
                    LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":PVP所有局数已打完，强制解散该房间：" + now_room.getRoomId());
                    GameLogic.removeRoom(this, now_room);
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":gameOver异常：" + ex.Message);
        }
    }

    void jueshengju(RoomData room)
    {
        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":开始决胜局：roomid = " + room.getRoomId() + "  gameRoomType = " + room.m_gameRoomType);

        // 通知客户端即将开始决胜局
        {
            JObject respondJO;
            {
                respondJO = new JObject();

                respondJO.Add("tag", m_tag);
                respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_JueShengJuTongZhi);
            }

            // 给在线的人推送
            for (int i = 0; i < room.getPlayerDataList().Count; i++)
            {
                if (!room.getPlayerDataList()[i].isOffLine())
                {
                    PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, respondJO.ToString());
                }
            }
        }

        // 创建决胜局房间
        {
            RoomData new_room = new RoomData(this, m_roomList.Count + 1, room.m_gameRoomType);
            new_room.m_rounds_pvp = room.m_rounds_pvp + 1;
            m_roomList.Add(new_room);
            LogUtil.getInstance().addDebugLog("新建比赛场决胜局房间：" + new_room.getRoomId());

            for (int i = 0; i < room.getPlayerDataList().Count; i++)
            {
                PlayerData playData = new PlayerData(room.getPlayerDataList()[i].m_connId, room.getPlayerDataList()[i].m_uid, room.getPlayerDataList()[i].m_isAI, room.getPlayerDataList()[i].m_gameRoomType);
                playData.m_score = (room.getPlayerDataList()[i].m_score);
                new_room.joinPlayer(playData);
            }
            
            {
                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + "删除旧房间：" + room.getRoomId());
                GameLogic.removeRoom(this, room);
            }

            // 开始决胜局
            {
                // 延迟3秒开赛
                Thread.Sleep(3000);

                // 检测房间人数是否可以开赛
                GameLogic.checkRoomStartGame(new_room, m_tag, true);
            }
        }
    }
}