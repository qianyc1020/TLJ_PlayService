using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TLJ_PlayService;
using TLJCommon;
using static TLJCommon.Consts;

class DDZ_GameLogic
{
    static string m_logFlag = "DDZ_GameLogic";

    /*
     * 检测该房间是否可以开始打牌
     * 如果可以的话就通知房间内的玩家
     * 然后每隔500毫秒给玩家发一张牌
     */
    public static void checkRoomStartGame(DDZ_RoomData room, string tag)
    {
        try
        {
            lock (room)
            {
                if (room.getRoomState() != DDZ_RoomState.RoomState_waiting)
                {
                    TLJ_PlayService.PlayService.log.Error("DDZ_GameUtil.checkRoomStartGame()错误----RoomState != RoomState_waiting");
                    return;
                }

                bool canStartGame = false;
                if (room.m_gameRoomType.CompareTo(TLJCommon.Consts.GameRoomType_DDZ_Normal) == 0)
                {
                    if (room.getPlayerDataList().Count == 3)
                    {
                        canStartGame = true;
                    }
                }

                if (canStartGame)
                {
                    // 停止匹配队友倒计时
                    room.m_timerUtil.stopTimer();

                    // 开始解散房间倒计时（设为15分钟，目的是房间异常后强制解散房间，否则玩家和机器人无法释放）
                    room.startBreakRoomTimer();

                    room.m_isStartGame = true;
                    room.setRoomState(DDZ_RoomState.RoomState_fapai);

                    //// 提交任务
                    //{
                    //    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    //    {
                    //        if ((!room.getPlayerDataList()[i].isOffLine()) && (!room.getPlayerDataList()[i].m_isAI))
                    //        {
                    //            Request_ProgressTask.doRequest(room.getPlayerDataList()[i].m_uid, 218);
                    //        }
                    //    }
                    //}

                    JObject respondJO = new JObject();
                    respondJO.Add("tag", tag);
                    respondJO.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_StartGame);

                    // 生成每个人的牌
                    {
                        // 随机分配牌
                        List<List<TLJCommon.PokerInfo>> pokerInfoList = DDZ_AllotPoker.AllotPokerToPlayer(room.m_gameRoomType);

                        for (int i = 0; i < room.getPlayerDataList().Count; i++)
                        {
                            room.getPlayerDataList()[i].setPokerList(pokerInfoList[i]);
                        }

                        room.setDiPokerList(pokerInfoList[pokerInfoList.Count - 1]);

                        // 自定义牌型
                        if (DebugConfig.s_isDebug)
                        {
                            for (int i = 0; i < room.getPlayerDataList().Count; i++)
                            {
                                PlayerCustomPokerInfo playerCustomPokerInfo = CustomPoker.findPlayerByUid(room.getPlayerDataList()[i].m_uid);
                                if (playerCustomPokerInfo != null)
                                {
                                    room.getPlayerDataList()[i].setPokerList(playerCustomPokerInfo.getPokerListForNew());
                                }
                            }
                        }
                    }

                    // 本房间的所有玩家
                    {
                        JArray userList = new JArray();
                        for (int i = 0; i < room.getPlayerDataList().Count; i++)
                        {
                            JObject temp = new JObject();
                            temp.Add("uid", room.getPlayerDataList()[i].m_uid);

                            userList.Add(temp);
                        }
                        respondJO.Add("userList", userList);
                    }

                    // 通知房间内的人开始比赛
                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        // 人数已满,可以开赛，发送给客户端
                        PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, respondJO.ToString());
                    }

                    room.startFaPaiTimer();

                    // 每个人做的处理
                    {
                        for (int i = 0; i < room.getPlayerDataList().Count; i++)
                        {
                            // 记录总局数数据
                            Request_RecordUserGameData.doRequest(room.getPlayerDataList()[i].m_uid, room.m_gameRoomType, (int)TLJCommon.Consts.GameAction.GameAction_StartGame);

                            // 获取游戏数据
                            Request_UserInfo_Game.doRequest(room.getPlayerDataList()[i].m_uid);

                            // 游戏在线统计
                            Request_OnlineStatistics.doRequest(room.getPlayerDataList()[i].m_uid,room.getRoomId(),room.m_gameRoomType, room.getPlayerDataList()[i].m_isAI, (int)Request_OnlineStatistics.OnlineStatisticsType.OnlineStatisticsType_Join);

                            // 休闲场扣除服务费:金币*500
                            {
                                if (room.m_gameRoomType.CompareTo(TLJCommon.Consts.GameRoomType_DDZ_Normal) == 0)
                                {
                                    Request_ChangeUserWealth.doRequest(room.getPlayerDataList()[i].m_uid, 1, -500, "斗地主经典玩法报名费");
                                }
                            }
                        }
                    }
                }
                else
                {
                    LogUtil.getInstance().addDebugLog("GameUtils----" + ":人数不够无法开赛：count = " + room.getPlayerDataList().Count);
                }
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error("DDZ_GameUtil.checkRoomStartGame()----" + ex + "tag:" + tag + "  roomid:" + room.getRoomId() + "gameroomtype:" + room.m_gameRoomType);
        }
    }

    public static void removeRoom(DDZ_GameBase gameBase, DDZ_RoomData room,bool isClearRoom)
    {
        try
        {
            // 把机器人还回去
            if (isClearRoom)
            {
                DDZ_GameUtil.clearRoomNonePlayer(room);
            }

            room.m_timerUtil.stopTimer();
            room.m_timerUtil_breakRom.stopTimer();
            room.m_timerUtil_FaPai.stopTimer();

            LogUtil.getInstance().writeRoomLog(room, "DDZ_GameUtils.removeRoom：删除房间：" + room.getRoomId());

            gameBase.getRoomList().Remove(room);
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ".removeRoom: " + ex);
        }
    }

    public static DDZ_RoomData getRoomByPlayerUid(DDZ_GameBase gameBase, string uid)
    {
        DDZ_RoomData room = null;

        // 找到玩家所在的房间
        for (int i = 0; i < gameBase.getRoomList().Count; i++)
        {
            List<DDZ_PlayerData> playerDataList = gameBase.getRoomList()[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                {
                    room = gameBase.getRoomList()[i];

                    return room;
                }
            }
        }

        return room;
    }

    public static void doTask_Chat(DDZ_GameBase gameBase, IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            int type = Convert.ToInt32(jo.GetValue("type"));
            int content_id = Convert.ToInt32(jo.GetValue("content_id"));

            DDZ_RoomData room = DDZ_GameLogic.getRoomByPlayerUid(gameBase, uid);

            if (room != null)
            {
                // 给在线的人推送
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    // 推送给客户端
                    if (!room.getPlayerDataList()[i].isOffLine())
                    {
                        PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, data);
                    }
                }
            }
            else
            {
                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ".doTask_Chat:未找到此人所在房间：" + uid);
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":doTask_Chat异常：" + ex);
        }
    }

    public static void doTask_SetTuoGuanState(DDZ_GameBase gameBase, IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            bool isTuoGuan = (bool)jo.GetValue("isTuoGuan");

            DDZ_RoomData room = DDZ_GameLogic.getRoomByPlayerUid(gameBase, uid);

            if (room != null)
            {
                DDZ_PlayerData playerData = room.getPlayerDataByUid(uid);

                if (playerData != null)
                {
                    playerData.setIsTuoGuan(isTuoGuan);

                    // 推送给客户端
                    if (!playerData.isOffLine())
                    {
                        JObject respondJO = new JObject();
                        respondJO.Add("tag", tag);
                        respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_OK);
                        respondJO.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_SetTuoGuanState);
                        respondJO.Add("uid", uid);
                        respondJO.Add("isTuoGuan", isTuoGuan);

                        PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
                    }
                }
            }
            else
            {
                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ".doTask_TuoGuan:未找到此人所在房间：" + uid);

                JObject respondJO = new JObject();
                respondJO.Add("tag", tag);
                respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_CommonFail);
                respondJO.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_SetTuoGuanState);
                respondJO.Add("uid", uid);
                respondJO.Add("isTuoGuan", isTuoGuan);

                PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":doTask_TuoGuan：" + ex);
        }
    }

    public static void tellPlayerTuoGuanState(DDZ_GameBase gameBase, DDZ_PlayerData playerData, bool isTuoGuan)
    {
        try
        {
            // 推送给客户端
            if (!playerData.isOffLine())
            {
                JObject respondJO = new JObject();
                respondJO.Add("tag", TLJCommon.Consts.Tag_DouDiZhu_Game);
                respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_OK);
                respondJO.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_SetTuoGuanState);
                respondJO.Add("uid", playerData.m_uid);
                respondJO.Add("isTuoGuan", isTuoGuan);

                PlayService.m_serverUtil.sendMessage(playerData.m_connId, respondJO.ToString());
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ".tellPlayerTuoGuanState: " + ex);
        }
    }

    public static void doTask_ExitGame(DDZ_GameBase gameBase, IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            int playAction = Convert.ToInt32(jo.GetValue("playAction"));

            // 检测该玩家是否已经加入房间
            for (int i = 0; i < gameBase.getRoomList().Count; i++)
            {
                List<DDZ_PlayerData> playerDataList = gameBase.getRoomList()[i].getPlayerDataList();

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
                            respondJO.Add("gameroomtype", gameBase.getRoomList()[i].m_gameRoomType);
                            respondJO.Add("roomId", gameBase.getRoomList()[i].getRoomId());

                            // 发送给客户端
                            PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
                        }

                        gameBase.doTaskPlayerCloseConn(uid);

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
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":doTask_ExitGame异常：" + ex);
        }
    }

    public static void doTask_QiangDiZhu(DDZ_GameBase gameBase, IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            int playAction = Convert.ToInt32(jo.GetValue("playAction"));
            int fen = Convert.ToInt32(jo.GetValue("fen"));
            
            DDZ_RoomData room = DDZ_GameUtil.getRoomByUid(uid);
            if (room != null)
            {
                DDZ_PlayerData playerData = room.getPlayerDataByUid(uid);

                // 抢地主失败
                if (room.m_maxJiaoFenPlayerData != null)
                {
                    if ((fen > 0) && (fen <= room.m_maxJiaoFenPlayerData.m_jiaofen))
                    {
                        // 给客户端回复
                        JObject respondJO = new JObject();
                        respondJO.Add("tag", tag);
                        respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_CommonFail);
                        respondJO.Add("playAction", playAction);
                        respondJO.Add("uid", uid);
                        respondJO.Add("fen", fen);
                        respondJO.Add("msg", "叫分失败，必须大于" + room.m_maxJiaoFenPlayerData.m_jiaofen + "分。");

                        // 发送给客户端
                        PlayService.m_serverUtil.sendMessage(playerData.m_connId, respondJO.ToString());

                        return;
                    }
                }

                // 停止倒计时
                playerData.m_timerUtil.stopTimer();
                playerData.m_timerUtilOffLine.stopTimer();

                List<DDZ_PlayerData> playerDataList = room.getPlayerDataList();
                int curQiangDiZhuPlayerIndex = -1;
                
                {
                    // 给客户端回复
                    JObject respondJO = new JObject();
                    respondJO.Add("tag", tag);
                    respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_OK);
                    respondJO.Add("playAction", playAction);
                    respondJO.Add("uid", uid);
                    respondJO.Add("fen", fen);

                    for (int j = 0; j < playerDataList.Count; j++)
                    {
                        if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                        {
                            curQiangDiZhuPlayerIndex = j;
                            playerDataList[j].m_jiaofen = fen;
                        }

                        // 发送给客户端
                        PlayService.m_serverUtil.sendMessage(playerDataList[j].m_connId, respondJO.ToString());
                    }
                }

                // 有人叫3分，直接定为地主
                if (fen == 3)
                {
                    playerData.m_isDiZhu = 1;
                    playerData.m_isJiaBang = 0;
                    room.m_diZhuPlayer = playerData;
                    room.m_maxJiaoFenPlayerData = playerData;
                    room.biggestPlayerData = room.m_diZhuPlayer;

                    tellPlayerWhoIsDiZhu(room);

                    // 通知农民加棒
                    {
                        room.setRoomState(DDZ_RoomState.RoomState_jiabang);

                        JObject jo2 = new JObject();
                        jo2.Add("tag", tag);
                        jo2.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_CallPlayerJiaBang);

                        for (int i = 0; i < playerDataList.Count; i++)
                        {
                            if (playerDataList[i].m_isDiZhu == 0)
                            {
                                // 开始倒计时
                                playerDataList[i].m_timerUtil.startTimer(room.m_jiaBangTime, TimerType.TimerType_jiaBang);
                                PlayService.m_serverUtil.sendMessage(playerDataList[i].m_connId, jo2.ToString());

                                // 如果离线了则托管出牌
                                if (playerDataList[i].isTuoGuan())
                                {
                                    // 开始倒计时
                                    playerDataList[i].m_timerUtilOffLine.startTimer(room.m_tuoguanOutPokerDur, TimerType.TimerType_jiaBang);
                                }
                            }
                        }
                    }

                    return;
                }

                // 叫下一个人抢地主
                {
                    room.m_maxJiaoFenPlayerData = playerData;

                    DDZ_PlayerData nextPlayer = null;

                    if ((curQiangDiZhuPlayerIndex + 1) < playerDataList.Count)
                    {
                        nextPlayer = playerDataList[curQiangDiZhuPlayerIndex + 1];
                    }
                    else
                    {
                        nextPlayer = playerDataList[0];
                    }

                    // 一轮叫分结束
                    if (nextPlayer.m_uid.CompareTo(room.m_firstQiangDiZhuPlayer.m_uid) == 0)
                    {
                        // 定好地主，开始加棒
                        DDZ_PlayerData maxPlayer = playerDataList[0];
                        for (int i = 1; i < playerDataList.Count; i++)
                        {
                            if (playerDataList[i].m_jiaofen > maxPlayer.m_jiaofen)
                            {
                                maxPlayer = playerDataList[i];
                            }
                        }

                        // 有人抢地主
                        if (maxPlayer.m_jiaofen > 0)
                        {
                            maxPlayer.m_isDiZhu = 1;
                            maxPlayer.m_isJiaBang = 0;
                            room.m_diZhuPlayer = maxPlayer;
                            room.m_maxJiaoFenPlayerData = room.m_diZhuPlayer;
                            room.biggestPlayerData = room.m_diZhuPlayer;

                            tellPlayerWhoIsDiZhu(room);

                            // 通知农民加棒
                            {
                                room.setRoomState(DDZ_RoomState.RoomState_jiabang);

                                JObject jo2 = new JObject();
                                jo2.Add("tag", tag);
                                jo2.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_CallPlayerJiaBang);

                                for (int i = 0; i < playerDataList.Count; i++)
                                {
                                    if (playerDataList[i].m_isDiZhu == 0)
                                    {
                                        // 开始倒计时
                                        playerDataList[i].m_timerUtil.startTimer(room.m_jiaBangTime, TimerType.TimerType_jiaBang);
                                        PlayService.m_serverUtil.sendMessage(playerDataList[i].m_connId, jo2.ToString());

                                        // 如果离线了则托管出牌
                                        if (playerDataList[i].isTuoGuan())
                                        {
                                            // 开始倒计时
                                            playerDataList[i].m_timerUtilOffLine.startTimer(room.m_tuoguanOutPokerDur, TimerType.TimerType_jiaBang);
                                        }
                                    }
                                }
                            }
                        }
                        // 没人抢地主,重新发牌
                        else
                        {
                            // 先通知
                            {
                                JObject jo2 = new JObject();
                                jo2.Add("tag", tag);
                                jo2.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_NoOneQiangDiZhu);

                                for (int i = 0; i < playerDataList.Count; i++)
                                {
                                    PlayService.m_serverUtil.sendMessage(playerDataList[i].m_connId, jo2.ToString());
                                }
                            }

                            // 清空数据
                            {
                                room.m_fapaiIndex = 0;
                                room.m_firstQiangDiZhuPlayer = null;
                                room.m_curQiangDiZhuPlayer = null;

                                room.getDiPokerList().Clear();

                                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                                {
                                    room.getPlayerDataList()[i].m_pokerList.Clear();
                                    room.getPlayerDataList()[i].m_allotPokerList.Clear();
                                }
                            }

                            // 重新发牌
                            {
                                // 开始解散房间倒计时（设为15分钟，目的是房间异常后强制解散房间，否则玩家和机器人无法释放）
                                room.startBreakRoomTimer();

                                room.m_isStartGame = true;
                                room.setRoomState(DDZ_RoomState.RoomState_fapai);

                                // 生成每个人的牌
                                {
                                    // 随机分配牌
                                    List<List<TLJCommon.PokerInfo>> pokerInfoList = DDZ_AllotPoker.AllotPokerToPlayer(room.m_gameRoomType);

                                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                                    {
                                        room.getPlayerDataList()[i].setPokerList(pokerInfoList[i]);
                                    }

                                    room.setDiPokerList(pokerInfoList[pokerInfoList.Count - 1]);

                                    // 自定义牌型
                                    if (DebugConfig.s_isDebug)
                                    {
                                        for (int i = 0; i < room.getPlayerDataList().Count; i++)
                                        {
                                            PlayerCustomPokerInfo playerCustomPokerInfo = CustomPoker.findPlayerByUid(room.getPlayerDataList()[i].m_uid);
                                            if (playerCustomPokerInfo != null)
                                            {
                                                room.getPlayerDataList()[i].setPokerList(playerCustomPokerInfo.getPokerListForNew());
                                            }
                                        }
                                    }
                                }

                                room.startFaPaiTimer();
                            }
                        }
                    }
                    // 叫下一个人抢地主
                    else
                    {
                        room.m_curQiangDiZhuPlayer = nextPlayer;

                        // 开始倒计时
                        nextPlayer.m_timerUtil.startTimer(room.m_qiangDiZhuTime, TimerType.TimerType_qiangDizhu);

                        JObject jo2 = new JObject();
                        jo2.Add("tag", tag);
                        jo2.Add("curMaxFen", fen);
                        jo2.Add("curJiaoDiZhuUid", nextPlayer.m_uid);
                        jo2.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_CallPlayerQiangDiZhu);

                        for (int i = 0; i < room.getPlayerDataList().Count; i++)
                        {
                            PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, jo2.ToString());
                        }

                        // 如果离线了则托管出牌
                        if (nextPlayer.isTuoGuan())
                        {
                            // 开始倒计时
                            nextPlayer.m_timerUtilOffLine.startTimer(room.m_tuoguanOutPokerDur, TimerType.TimerType_qiangDizhu);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":doTask_QiangDiZhu异常：" + ex);
        }
    }

    // 通知玩家谁是地主
    public static void tellPlayerWhoIsDiZhu(DDZ_RoomData room)
    {
        try
        {
            JObject jo2 = new JObject();
            jo2.Add("tag", room.m_tag);
            jo2.Add("uid", room.m_diZhuPlayer.m_uid);
            jo2.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_WhoIsDiZhu);

            for (int i = 0; i < room.getPlayerDataList().Count; i++)
            {
                jo2.Add("beishu_" + room.getPlayerDataList()[i].m_uid, room.getBeiShuByUid(room.getPlayerDataList()[i].m_uid));
            }

            // 底牌
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

                    // 把底牌加到地主牌里面去
                    room.m_diZhuPlayer.getPokerList().Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));
                }

                jo2.Add("diPokerList", pokerList);
            }

            for (int i = 0; i < room.getPlayerDataList().Count; i++)
            {
                PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, jo2.ToString());
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ".tellPlayerTuoGuanState: " + ex);
        }
    }

    public static void doTask_JiaBang(DDZ_GameBase gameBase, IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            int playAction = Convert.ToInt32(jo.GetValue("playAction"));
            int isJiaBang = Convert.ToInt32(jo.GetValue("isJiaBang"));

            DDZ_RoomData room = DDZ_GameUtil.getRoomByUid(uid);
            if (room != null)
            {
                lock (room)
                {
                    DDZ_PlayerData playerData = room.getPlayerDataByUid(uid);
                    playerData.m_isJiaBang = isJiaBang;

                    // 停止倒计时
                    playerData.m_timerUtil.stopTimer();
                    playerData.m_timerUtilOffLine.stopTimer();

                    List<DDZ_PlayerData> playerDataList = room.getPlayerDataList();

                    for (int i = 0; i < playerDataList.Count; i++)
                    {
                        // 给客户端回复
                        {
                            JObject respondJO = new JObject();
                            respondJO.Add("tag", tag);
                            respondJO.Add("playAction", playAction);
                            respondJO.Add("uid", uid);
                            respondJO.Add("isJiaBang", isJiaBang);

                            for (int j = 0; j < room.getPlayerDataList().Count; j++)
                            {
                                respondJO.Add("beishu_" + room.getPlayerDataList()[j].m_uid, room.getBeiShuByUid(room.getPlayerDataList()[j].m_uid));
                            }

                            // 发送给客户端
                            PlayService.m_serverUtil.sendMessage(playerDataList[i].m_connId, respondJO.ToString());
                        }
                    }

                    {
                        bool isJiaBangEnd = true;
                        for (int i = 0; i < playerDataList.Count; i++)
                        {
                            if (playerDataList[i].m_isJiaBang == -1)
                            {
                                isJiaBangEnd = false;
                                break;
                            }
                        }
                        
                        // 加棒结束，开始出牌
                        if (isJiaBangEnd)
                        {
                            room.setRoomState(DDZ_RoomState.RoomState_gaming);

                            room.m_diZhuPlayer.m_isFreeOutPoker = true;
                            room.m_curOutPokerPlayer = room.m_diZhuPlayer;

                            doTask_CallPlayerOutPoker(room.m_gameBase,room, room.m_diZhuPlayer);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":doTask_JiaBang异常：" + ex);
        }
    }

    public static void doTask_WaitMatchTimeOut(DDZ_RoomData room)
    {
        try
        {
            lock (room)
            {
                if (room.getRoomState() == DDZ_RoomState.RoomState_waiting)
                {
                    // 由机器人填充缺的人
                    int needAICount = 0;
                    if (room.m_gameRoomType.CompareTo(TLJCommon.Consts.GameRoomType_DDZ_Normal) == 0)
                    {
                        needAICount = 3 - room.getPlayerDataList().Count;
                    }

                    List<string> ai_list = new List<string>();
                    for (int i = 0; i < needAICount; i++)
                    {
                        string ai_uid = AIDataScript.getInstance().getOneAI();
                        ai_list.Add(ai_uid);

                        if (ai_uid.CompareTo("") != 0)
                        {
                            LogUtil.getInstance().writeRoomLog(room,m_logFlag + "----" + "给room:" + room.getRoomId() + "创建机器人：" + ai_uid);

                            DDZ_PlayerData playerData = new DDZ_PlayerData((IntPtr)(-1), ai_uid, true, room.m_gameRoomType);
                            
                            room.joinPlayer(playerData);
                        }
                        else
                        {
                            LogUtil.getInstance().writeRoomLog(room, m_logFlag + "----" + "机器人不足");

                            // 通知客户端
                            {
                                JObject respondJO = new JObject();
                                respondJO.Add("tag", room.m_tag);
                                respondJO.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_MatchFail);

                                for (int j = 0; j < room.getPlayerDataList().Count; j++)
                                {
                                    if (!room.getPlayerDataList()[j].isOffLine())
                                    {
                                        PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[j].m_connId, respondJO.ToString());
                                    }
                                }
                            }

                            // 把之前借的机器人还回去
                            for (int j = 0; j < ai_list.Count; j++)
                            {
                                AIDataScript.getInstance().backOneAI(ai_list[j]);
                            }

                            // 解散该房间
                            DDZ_GameLogic.removeRoom(room.m_gameBase, room, true);

                            return;
                        }
                    }

                    // 检测房间人数是否可以开赛
                    DDZ_GameLogic.checkRoomStartGame(room, room.m_tag);
                }
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":doTask_JoinGame异常：" + ex);
        }
    }
    
    // 通知玩家出牌
    public static void doTask_CallPlayerOutPoker(DDZ_GameBase gameBase, DDZ_RoomData room, DDZ_PlayerData playerData)
    {
        try
        {
            if (room.biggestPlayerData.m_uid.CompareTo(playerData.m_uid) == 0)
            {
                playerData.m_isFreeOutPoker = true;
            }
            else
            {
                playerData.m_isFreeOutPoker = false;
            }

            // 通知
            {
                JObject respondJO;
                {
                    respondJO = new JObject();

                    respondJO.Add("tag", room.m_tag);
                    respondJO.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_CallPlayerOutPoker);
                    respondJO.Add("uid", room.m_curOutPokerPlayer.m_uid);
                    
                    respondJO.Add("isFreeOutPoker", playerData.m_isFreeOutPoker);
                }

                // 给在线的人推送
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    // 推送给客户端
                    if (!room.getPlayerDataList()[i].isOffLine())
                    {
                        PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId,respondJO.ToString());
                    }
                }

                // 对当前出牌的人做其他处理
                {
                    // 开始倒计时
                    playerData.m_timerUtil.startTimer(room.m_outPokerDur, TimerType.TimerType_outPoker);

                    // 如果离线了则托管出牌
                    if (playerData.isTuoGuan())
                    {
                        // 开始倒计时
                        playerData.m_timerUtilOffLine.startTimer(room.m_tuoguanOutPokerDur, TimerType.TimerType_outPoker);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":doTask_CallPlayerOutPoker异常：" + ex);
        }
    }

    // 收到玩家出的牌
    public static void doTask_ReceivePlayerOutPoker(DDZ_GameBase gameBase, IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            bool hasOutPoker = (bool)jo.GetValue("hasOutPoker");

            DDZ_RoomData room = DDZ_GameUtil.getRoomByUid(uid);
            if (room != null)
            {
                lock (room)
                {
                    DDZ_PlayerData playerData = room.getPlayerDataByUid(uid);

                    if (playerData != null)
                    {
                        if (room.m_curOutPokerPlayer.m_uid.CompareTo(playerData.m_uid) != 0)
                        {
                            string str = "doTask_ReceivePlayerOutPoker错误：当前出牌人应该是：" + room.m_curOutPokerPlayer.m_uid + ",但是现在收到的出牌人是：" + playerData.m_uid;
                            LogUtil.getInstance().writeRoomLog(room, str);
                            return;
                        }

                        // 停止倒计时
                        playerData.m_timerUtil.stopTimer();
                        
                        {
                            if (hasOutPoker)
                            {
                                JArray ja = (JArray)JsonConvert.DeserializeObject(jo.GetValue("pokerList").ToString());
                                List<TLJCommon.PokerInfo> outPokerList = new List<TLJCommon.PokerInfo>();
                                for (int m = 0; m < ja.Count; m++)
                                {
                                    int num = Convert.ToInt32(ja[m]["num"]);
                                    int pokerType = Convert.ToInt32(ja[m]["pokerType"]);

                                    outPokerList.Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));
                                }

                                room.biggestPlayerData = playerData;

                                // 处理此人出的牌
                                playerOutPoker(room, playerData, outPokerList);

                                // 让下一个玩家出牌
                                checkNextOutPokerPlayerData(room, playerData);
                            }
                            else
                            { 
                                // 处理此人出的牌
                                playerOutPoker(room, playerData, new List<PokerInfo>());

                                // 让下一个玩家出牌
                                checkNextOutPokerPlayerData(room, playerData);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":doTask_ReceivePlayerOutPoker异常：" + ex);
        }
    }

    // 处理此人出的牌
    static void playerOutPoker(DDZ_RoomData room, DDZ_PlayerData playerData, List<PokerInfo> pokerList)
    {
        try
        {
            // 牌型判断
            {
                CrazyLandlords.Helper.LandlordsCardsHelper.SetWeight(pokerList);
                CrazyLandlords.Helper.CardsType cardsType;
                CrazyLandlords.Helper.LandlordsCardsHelper.GetCardsType(pokerList.ToArray(), out cardsType);

                switch (cardsType)
                {
                    case CrazyLandlords.Helper.CardsType.Boom:
                    case CrazyLandlords.Helper.CardsType.JokerBoom:
                        {
                            room.m_beishu_bomb *= 2;
                        }
                        break;
                }
            }

            // 其他处理
            {
                // 出牌次数
                if (pokerList.Count > 0)
                {
                    ++playerData.m_outPokerCiShu;
                }

                playerData.m_curOutPokerList.Clear();

                for (int i = 0; i < pokerList.Count; i++)
                {
                    int num = pokerList[i].m_num;
                    int pokerType = (int)pokerList[i].m_pokerType;

                    // 加到所有已出的牌集合里
                    room.addOutPoker(num, pokerType);

                    for (int n = playerData.getPokerList().Count - 1; n >= 0; n--)
                    {
                        if ((playerData.getPokerList()[n].m_num == num) && ((int)playerData.getPokerList()[n].m_pokerType == pokerType))
                        {
                            // 加到当前这一轮出牌的牌堆里面
                            playerData.m_curOutPokerList.Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));

                            // 出的牌从自己的牌堆里删除
                            {
                                playerData.getPokerList().RemoveAt(n);
                            }

                            break;
                        }
                    }
                }
            }

            // 推送给同桌所有玩家
            {
                JObject respondJO = new JObject();
                {
                    respondJO.Add("tag", room.m_tag);
                    respondJO.Add("uid", playerData.m_uid);
                    respondJO.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_PlayerOutPoker);
                    respondJO.Add("restPokerCount", playerData.getPokerList().Count);

                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        respondJO.Add("beishu_" + room.getPlayerDataList()[i].m_uid, room.getBeiShuByUid(room.getPlayerDataList()[i].m_uid));
                    }

                    {
                        JArray temp_ja = new JArray();
                        for (int m = 0; m < pokerList.Count; m++)
                        {
                            JObject temp_jo = new JObject();

                            int num = pokerList[m].m_num;
                            int pokerType = (int)pokerList[m].m_pokerType;

                            temp_jo.Add("num", num);
                            temp_jo.Add("pokerType", pokerType);

                            temp_ja.Add(temp_jo);
                        }

                        respondJO.Add("pokerList", temp_ja);
                    }
                }

                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    // 推送给客户端
                    if (!room.getPlayerDataList()[i].isOffLine())
                    {
                        PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, respondJO.ToString());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addDebugLog(m_logFlag + "----playerOutPoker:" + ex.Message);
        }
    }

    static void checkNextOutPokerPlayerData(DDZ_RoomData room, DDZ_PlayerData playerData)
    {
        try
        {
            // 检测是否有人出完全部牌
            {
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    // 结束
                    if (room.getPlayerDataList()[i].getPokerList().Count == 0)
                    {
                        room.m_winPlayerData = room.getPlayerDataList()[i];
                        room.m_gameBase.gameOver(room);
                        return;
                    }
                }
            }

            DDZ_PlayerData nextPlayerData = null;

            {
                int index = 0;
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    if (playerData.m_uid.CompareTo(room.getPlayerDataList()[i].m_uid) == 0)
                    {
                        index = i;
                        break;
                    }
                }

                if (index == (room.getPlayerDataList().Count - 1))
                {
                    nextPlayerData = room.getPlayerDataList()[0];
                }
                else
                {
                    nextPlayerData = room.getPlayerDataList()[index + 1];
                }

                LogUtil.getInstance().writeRoomLog(room, m_logFlag + "----roomID = " + room.getRoomId() + "  当前出牌人：" + playerData.m_uid + "  下一个出牌人：" + nextPlayerData.m_uid);

                if (nextPlayerData == null)
                {
                    LogUtil.getInstance().writeRoomLog(room, m_logFlag + "----roomID = " + room.getRoomId() + ":下一个出牌人为空");

                    return;
                }

                room.m_curOutPokerPlayer = nextPlayerData;

                // 让下一个人出牌
                DDZ_GameLogic.doTask_CallPlayerOutPoker(room.m_gameBase, room, room.m_curOutPokerPlayer);
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addDebugLog(m_logFlag + "----checkNextOutPokerPlayerData:" + ex.Message);
        }
    }
    
    public static bool breakRoomByRoomID(int roomID)
    {
        // 休闲场
        {
            List<DDZ_RoomData> roomList = PlayLogic_DDZ.getInstance().getRoomList();

            for (int i = 0; i < roomList.Count; i++)
            {
                DDZ_RoomData room = roomList[i];
                if (room.getRoomId() == roomID)
                {
                    // 游戏在线统计
                    for (int j = 0; j < room.getPlayerDataList().Count; j++)
                    {
                        Request_OnlineStatistics.doRequest(room.getPlayerDataList()[j].m_uid, room.getRoomId(), room.m_gameRoomType, room.getPlayerDataList()[j].m_isAI, (int)Request_OnlineStatistics.OnlineStatisticsType.OnlineStatisticsType_exit);
                    }

                    removeRoom(PlayLogic_DDZ.getInstance(), room, true);

                    return true;
                }
            }
        }
        
        return false;
    }

    public static void doTask_RetryJoinGame(IntPtr connId, string reqData)
    {
        JObject respondJO = new JObject();

        try
        {
            JObject jo = JObject.Parse(reqData);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            // 逻辑处理
            {
                DDZ_RoomData room = DDZ_GameUtil.getRoomByUid(uid);
                DDZ_PlayerData playerData = room.getPlayerDataByUid(uid);

                if (room != null)
                {
                    // 游戏在线统计
                    Request_OnlineStatistics.doRequest(playerData.m_uid, room.getRoomId(), room.m_gameRoomType, playerData.m_isAI, (int)Request_OnlineStatistics.OnlineStatisticsType.OnlineStatisticsType_Join);

                    respondJO.Add("tag", TLJCommon.Consts.Tag_ResumeGame);
                    respondJO.Add("gameroomtype", room.m_gameRoomType);
                    respondJO.Add("roomState", (int)room.getRoomState());
                    respondJO.Add("beishu", room.getBeiShuByUid(uid));

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

                    // 每个人剩余的牌
                    {
                        JArray ja = new JArray();
                        for (int i = 0; i < room.getPlayerDataList().Count; i++)
                        {
                            JObject temp = new JObject();
                            temp.Add("uid", room.getPlayerDataList()[i].m_uid);
                            temp.Add("restPokerCount", room.getPlayerDataList()[i].getPokerList().Count);

                            ja.Add(temp);
                        }
                        respondJO.Add("playerRestPokerCount", ja);
                    }

                    // 加棒
                    {
                        // 加棒状态
                        {
                            JArray ja = new JArray();
                            for (int i = 0; i < room.getPlayerDataList().Count; i++)
                            {
                                JObject temp = new JObject();
                                temp.Add("uid", room.getPlayerDataList()[i].m_uid);
                                temp.Add("isJiaBang", room.getPlayerDataList()[i].m_isJiaBang);

                                ja.Add(temp);
                            }
                            respondJO.Add("jiabangState", ja);
                        }

                        // 是否需要选择加棒
                        {
                            respondJO.Add("isNeedChoiceJiaBang", (playerData.m_isJiaBang == -1) ? 1 : 0);
                        }
                    }

                    // 当前出牌的人
                    if (room.m_curOutPokerPlayer != null)
                    {
                        respondJO.Add("curOutPokerPlayer", room.m_curOutPokerPlayer.m_uid);
                        respondJO.Add("isFreeOutPoker", room.m_curOutPokerPlayer.m_isFreeOutPoker);
                    }
                    else
                    {
                        respondJO.Add("curOutPokerPlayer", "");
                    }

                    // 地主
                    if (room.m_diZhuPlayer != null)
                    {
                        respondJO.Add("dizhuUID", room.m_diZhuPlayer.m_uid);
                    }
                    else
                    {
                        respondJO.Add("dizhuUID", "");
                    }
                    
                    {
                        // 当前叫分最大的人
                        if (room.m_maxJiaoFenPlayerData != null)
                        {
                            respondJO.Add("curMaxFen", room.m_maxJiaoFenPlayerData.m_jiaofen);
                        }
                        else
                        {
                            respondJO.Add("curMaxFen", 0);
                        }

                        // 当前抢地主的人
                        if (room.m_curQiangDiZhuPlayer != null)
                        {
                            respondJO.Add("curJiaoDiZhuUid", room.m_curQiangDiZhuPlayer.m_uid);
                        }
                        else
                        {
                            respondJO.Add("curJiaoDiZhuUid", "");
                        }
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

                    // 当前回合出牌最大的人
                    {
                        if (room.biggestPlayerData != null)
                        {
                            respondJO.Add("biggestPlayerUID", room.biggestPlayerData.m_uid);
                        }
                        else
                        {
                            respondJO.Add("biggestPlayerUID", "");
                        }
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

                    // diPokerList
                    {
                        JArray ja = new JArray();
                        for (int i = 0; i < room.getDiPokerList().Count; i++)
                        {
                            JObject temp = new JObject();
                            temp.Add("num", room.getDiPokerList()[i].m_num);
                            temp.Add("pokerType", (int)room.getDiPokerList()[i].m_pokerType);

                            ja.Add(temp);
                        }
                        respondJO.Add("diPokerList", ja);
                    }

                    // 把玩家设为在线
                    {
                        playerData.setIsOffLine(false);
                        playerData.m_connId = connId;
                    }

                    // 发送给客户端
                    PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());

                    // 推送头像昵称等信息
                    {
                        for (int i = 0; i < room.getPlayerDataList().Count; i++)
                        {
                            UserInfo_Game userInfo_Game = UserInfo_Game_Manager.getDataByUid(room.getPlayerDataList()[i].m_uid);
                            if (userInfo_Game != null)
                            {
                                string data = Newtonsoft.Json.JsonConvert.SerializeObject(userInfo_Game);

                                for (int j = 0; j < room.getPlayerDataList().Count; j++)
                                {
                                    if ((!room.getPlayerDataList()[j].isOffLine()) && (room.getPlayerDataList()[j].m_uid.CompareTo(userInfo_Game.uid) != 0))
                                    {
                                        // 发送给客户端
                                        PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[j].m_connId, data);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error("NetRespond_RetryJoinGame----" + ex);

            // 客户端参数错误
            respondJO.Add("code", Convert.ToInt32(TLJCommon.Consts.Code.Code_ParamError));
        }
    }
}
