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

class GameLogic
{
    static string m_logFlag = "GameLogic";

    /*
     * 检测该房间是否可以开始打牌
     * 如果可以的话就通知房间内的玩家
     * 然后每隔500毫秒给玩家发一张牌
     */
    public static void checkRoomStartGame(RoomData room, string tag, bool initLevelPokerNum)
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

                                        // 提交任务
                                        if (tempList[2].CompareTo("16") == 0)
                                        {
                                            Request_ProgressTask.doRequest(room.getPlayerDataList()[i].m_uid, 218);
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
                
                // 每个人做的处理
                {
                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        // 记录总局数数据
                        Request_RecordUserGameData.doRequest(room.getPlayerDataList()[i].m_uid, room.m_gameRoomType, (int)TLJCommon.Consts.GameAction.GameAction_StartGame);

                        // 获取游戏数据
                        Request_UserInfo_Game.doRequest(room.getPlayerDataList()[i].m_uid);
                    }
                }

                room.m_isStartGame = true;
                room.m_roomState = RoomState.RoomState_qiangzhu;

                // 设置级牌
                if (initLevelPokerNum)
                {
                    if (room.m_wanfaType == (int)TLJCommon.Consts.WanFaType.WanFaType_PVP)
                    {
                        room.m_levelPokerNum = RandomUtil.getRandom(2,14);
                    }
                    else
                    {
                        room.m_levelPokerNum = 2;
                    }

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
                        temp.Add("score", room.getPlayerDataList()[i].m_score);

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

                Thread thread = new Thread(fapaiThread);
                thread.Start(room);
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

    static void fapaiThread(object obj)
    {
        RoomData room = (RoomData)obj;
        // 一张一张给每人发牌
        {
            for (int i = 0; i < 25; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    int num = room.getPlayerDataList()[j].getPokerList()[i].m_num;
                    int pokerType = (int)room.getPlayerDataList()[j].getPokerList()[i].m_pokerType;

                    if (!room.getPlayerDataList()[j].m_isOffLine)
                    {
                        JObject jo2 = new JObject();
                        jo2.Add("tag", room.m_tag);
                        jo2.Add("uid", room.getPlayerDataList()[j].m_uid);
                        jo2.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_FaPai);
                        jo2.Add("num", num);
                        jo2.Add("pokerType", pokerType);

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

                    room.getPlayerDataList()[j].m_allotPokerList.Add(new PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));
                }

                Thread.Sleep(400);
            }
        }

        LogUtil.getInstance().addDebugLog("牌已发完");

        // 抢主倒计时
        room.m_timerUtil.startTimer(room.m_qiangzhuTime, TimerType.TimerType_qiangzhu);
    }

    public static void removeRoom(GameBase gameBase,RoomData room)
    {
        // 把机器人还回去
        for (int i = 0; i < room.getPlayerDataList().Count; i++)
        {
            if (room.getPlayerDataList()[i].m_isAI)
            {
                AIDataScript.getInstance().backOneAI(room.getPlayerDataList()[i].m_uid);
            }
        }

        LogUtil.getInstance().addDebugLog("GameUtils.removeRoom：删除房间：" + room.getRoomId());

        gameBase.getRoomList().Remove(room);
    }

    public static  RoomData getRoomByPlayerUid(GameBase gameBase, string uid)
    {
        RoomData room = null;

        // 找到玩家所在的房间
        for (int i = 0; i < gameBase.getRoomList().Count; i++)
        {
            List<PlayerData> playerDataList = gameBase.getRoomList()[i].getPlayerDataList();

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

    public static void doTask_ContinueGame(GameBase gameBase, IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            RoomData room = GameLogic.getRoomByPlayerUid(gameBase, uid);

            if (room != null)
            {
                bool isOK = true;

                {
                    JObject respondJO = new JObject();
                    respondJO.Add("tag", tag);
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
                        room.deletePlayer(uid);

                        if (GameUtil.checkRoomNonePlayer(room))
                        {
                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + room.getRoomId());
                            GameLogic.removeRoom(gameBase, room);
                        }
                    }
                }

                // 检查是否4个人都愿意继续游戏
                if (isOK)
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

                        GameLogic.checkRoomStartGame(room, tag, false);
                    }
                }
            }
            else
            {
                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ".doTask_ContinueGame:未找到此人所在房间：" + uid);
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_ContinueGame异常：" + ex.Message);
        }
    }

    public static void doTask_ChangeRoom(GameBase gameBase, IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            RoomData cur_room = GameLogic.getRoomByPlayerUid(gameBase, uid);
            string gameroomtype = cur_room.m_gameRoomType;

            if (cur_room != null)
            {
                // 检查是否有人想继续游戏，有的话则告诉他失败
                {
                    for (int i = cur_room.getPlayerDataList().Count - 1; i >= 0; i--)
                    {
                        if (cur_room.getPlayerDataList()[i].m_isContinueGame)
                        {
                            JObject respondJO = new JObject();
                            respondJO.Add("tag", tag);
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

                    if (GameUtil.checkRoomNonePlayer(cur_room))
                    {
                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ".doTask_ChangeRoom:玩家换桌，该房间:" + cur_room.getRoomId() + "人数为空，解散该房间");
                        GameLogic.removeRoom(gameBase, cur_room);
                    }
                }

                RoomData room = null;

                // 在已有的房间寻找可以加入的房间
                for (int i = 0; i < gameBase.getRoomList().Count; i++)
                {
                    if ((gameroomtype.CompareTo(gameBase.getRoomList()[i].m_gameRoomType) == 0) && (1 == gameBase.getRoomList()[i].m_rounds_pvp) && (gameBase.getRoomList()[i].m_roomState == RoomState.RoomState_waiting))
                    //if (gameBase.getRoomList()[i].m_roomState == RoomState.RoomState_waiting)
                    {
                        if (gameBase.getRoomList()[i].joinPlayer(new PlayerData(connId, uid, false, gameroomtype)))
                        {
                            room = gameBase.getRoomList()[i];
                            break;
                        }
                    }
                }

                // 当前没有房间可加入的话则创建一个新的房间
                if (room == null)
                {
                    room = new RoomData(gameBase, gameBase.getRoomList().Count + 1, gameroomtype);
                    room.joinPlayer(new PlayerData(connId, uid, false,gameroomtype));

                    gameBase.getRoomList().Add(room);
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
                GameLogic.checkRoomStartGame(room, tag, true);
            }
            else
            {
                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ".doTask_ChangeRoom:未找到此人所在房间：" + uid);
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":ChangeRoom异常：" + ex.Message);
        }
    }

    public static void doTask_Chat(GameBase gameBase, IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            int type = Convert.ToInt32(jo.GetValue("type"));
            int content_id = Convert.ToInt32(jo.GetValue("content_id"));

            RoomData room = GameLogic.getRoomByPlayerUid(gameBase, uid);

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
                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ".doTask_Chat:未找到此人所在房间：" + uid);
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_Chat异常：" + ex.Message);
        }
    }

    public static void doTask_ExitGame(GameBase gameBase, IntPtr connId, string data)
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
                List<PlayerData> playerDataList = gameBase.getRoomList()[i].getPlayerDataList();

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

                        gameBase.doTaskPlayerCloseConn(connId);

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
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_ExitGame异常：" + ex.Message);
        }
    }

    public static void doTask_WaitMatchTimeOut(GameBase gameBase, IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            RoomData room = getRoomByPlayerUid(gameBase, uid);
            
            if (room != null)
            {
                lock (room)
                {
                    if (room.m_roomState == RoomState.RoomState_waiting)
                    {
                        // 由机器人填充缺的人
                        int needAICount = 4 - room.getPlayerDataList().Count;
                        for (int i = 0; i < needAICount; i++)
                        {
                            string ai_uid = AIDataScript.getInstance().getOneAI();
                            if (ai_uid.CompareTo("") != 0)
                            {
                                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + "给room:" + room.getRoomId() + "创建机器人：" + ai_uid);

                                PlayerData playerData = new PlayerData((IntPtr)(-1), ai_uid, true, room.m_gameRoomType);
                                playerData.m_isOffLine = true;

                                // 如果是PVP的话，从第二轮开始，机器人的分数不能太假，要在合理的范围
                                if (room.m_rounds_pvp > 1)
                                {
                                    for (int j = 0; j < room.getPlayerDataList().Count; j++)
                                    {
                                        if (!room.getPlayerDataList()[j].m_isAI)
                                        {
                                            playerData.m_score = RandomUtil.getRandom((int)(room.getPlayerDataList()[j].m_score * 0.7f), (int)(room.getPlayerDataList()[j].m_score * 1.3f));
                                            break;
                                        }
                                    }
                                }

                                room.joinPlayer(playerData);
                            }
                            else
                            {
                                LogUtil.getInstance().addErrorLog(m_logFlag + "----" + "机器人不足");
                            }
                        }

                        // 检测房间人数是否可以开赛
                        GameLogic.checkRoomStartGame(room, tag, true);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_JoinGame异常：" + ex.Message);
        }
    }

    public static void doTask_QiangZhuEnd(RoomData room)
    {
        {
            JObject respondJO = new JObject();
            respondJO.Add("tag", room.m_tag);
            respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_QiangZhuEnd);
            respondJO.Add("zhuangjiaUid", room.m_zhuangjiaPlayerData.m_uid);
            respondJO.Add("masterPokerType", room.m_masterPokerType);

            // 提交任务
            {
                Request_ProgressTask.doRequest(room.m_zhuangjiaPlayerData.m_uid, 215);
            }

            // 发送给客户端
            for (int k = 0; k < room.getPlayerDataList().Count; k++)
            {
                if (respondJO.GetValue("isBanker") != null)
                {
                    respondJO.Remove("isBanker");
                }

                if ((room.getPlayerDataList()[k].m_uid.CompareTo(room.m_zhuangjiaPlayerData.m_uid) == 0) ||
                    (room.getPlayerDataList()[k].m_uid.CompareTo(room.m_zhuangjiaPlayerData.m_teammateUID) ==
                     0))
                {
                    room.getPlayerDataList()[k].m_isBanker = 1;
                    respondJO.Add("isBanker", 1);
                }
                else
                {
                    room.getPlayerDataList()[k].m_isBanker = 0;
                    respondJO.Add("isBanker", 0);
                }

                if (!room.getPlayerDataList()[k].m_isOffLine)
                {
                    PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[k].m_connId, respondJO.ToString());
                }
            }
        }
    }

    // 玩家抢主
    public static void doTask_QiangZhu(GameBase gameBase, IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            int playAction = Convert.ToInt32(jo.GetValue("playAction"));

            for (int i = 0; i < gameBase.getRoomList().Count; i++)
            {
                RoomData room = gameBase.getRoomList()[i];
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
                        if (isQiangZhuSuccess)
                        {
                            JObject respondJO = new JObject();
                            respondJO.Add("tag", tag);
                            respondJO.Add("playAction", playAction);
                            respondJO.Add("uid", uid);
                            respondJO.Add("pokerList", jo.GetValue("pokerList"));

                            // 发送给客户端
                            for (int k = 0; k < playerDataList.Count; k++)
                            {
                                PlayService.m_serverUtil.sendMessage(playerDataList[k].m_connId, respondJO.ToString());
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_QiangZhu异常：" + ex.Message);
        }
    }

    // 庄家埋底
    public static void doTask_MaiDi(GameBase gameBase, IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            for (int i = 0; i < gameBase.getRoomList().Count; i++)
            {
                RoomData room = gameBase.getRoomList()[i];
                List<PlayerData> playerDataList = room.getPlayerDataList();


                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                    {
                        if (playerDataList[j].getPokerList().Count != 33)
                        {
                            LogUtil.getInstance().addDebugLog(m_logFlag + "----doTask_MaiDi错误：手牌张数不是33：" + uid);
                            return;
                        }

                        // 埋底倒计时停止
                        playerDataList[j].m_timerUtil.stopTimer();

                        // 返回埋底结果
                        if (!playerDataList[j].m_isOffLine)
                        {
                            JObject temp = new JObject();
                            temp.Add("tag", tag);
                            temp.Add("uid",uid);
                            temp.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_MaiDiBack);
                            temp.Add("diPokerList", jo.GetValue("diPokerList"));

                            PlayService.m_serverUtil.sendMessage(playerDataList[j].m_connId, temp.ToString());
                        }

                        // 删除此人出的牌、替换底牌
                        {
                            room.getDiPokerList().Clear();

                            JArray ja = (JArray)JsonConvert.DeserializeObject(jo.GetValue("diPokerList").ToString());
                            for (int m = 0; m < ja.Count; m++)
                            {
                                int num = Convert.ToInt32(ja[m]["num"]);
                                int pokerType = Convert.ToInt32(ja[m]["pokerType"]);

                                for (int n = playerDataList[j].getPokerList().Count - 1; n >= 0; n--)
                                {
                                    if ((playerDataList[j].getPokerList()[n].m_num == num) &&
                                        ((int)playerDataList[j].getPokerList()[n].m_pokerType == pokerType))
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

                        // 给108所有牌设置权重
                        {
                            PlayRuleUtil.SetAllPokerWeight(room);
                        }

                        // 本房间不可以炒底则直接开始游戏
                        if (!room.m_canChaoDi)
                        {
                            room.m_roomState = RoomState.RoomState_gaming;

                            // 开始本房间的比赛
                            GameLogic.doTask_CallPlayerOutPoker(gameBase, room, data, true);
                        }
                        // 本房间可以炒底则通知玩家炒底
                        else
                        {
                            room.m_roomState = RoomState.RoomState_chaodi;

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
                                room.m_roomState = RoomState.RoomState_gaming;

                                // 开始本房间的比赛
                                GameLogic.doTask_CallPlayerOutPoker(gameBase, room, data, true);
                            }
                            else
                            {
                                GameLogic.callPlayerChaoDi(gameBase, room, playerData);
                            }
                        }

                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_MaiDi异常：" + ex.Message);
        }
    }

    // 玩家炒底
    public static void doTask_PlayerChaoDi(GameBase gameBase, IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            int playAction = Convert.ToInt32(jo.GetValue("playAction"));
            int hasPoker = Convert.ToInt32(jo.GetValue("hasPoker"));

            for (int i = 0; i < gameBase.getRoomList().Count; i++)
            {
                RoomData room = gameBase.getRoomList()[i];
                List<PlayerData> playerDataList = room.getPlayerDataList();

                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                    {
                        PlayerData playerData = playerDataList[j];

                        // 停止倒计时
                        playerData.m_timerUtil.stopTimer();

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
                                    playerData.getPokerList().Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));
                                }

                                respondJO.Add("diPokerList", pokerList);

                                {
                                    room.m_curMaiDiPlayer = playerData;
                                    room.m_roomState = RoomState.RoomState_othermaidi;

                                    // 开始倒计时
                                    playerData.m_timerUtil.startTimer(room.m_maidiTime, TimerType.TimerType_maidi);

                                    // 如果当前要炒底的人离线
                                    if (playerData.m_isOffLine)
                                    {
                                        TrusteeshipLogic.trusteeshipLogic_MaiDi(gameBase, room, playerData);
                                    }
                                }
                            }                            

                            // 发送给客户端
                            for (int k = 0; k < playerDataList.Count; k++)
                            {
                                if (!playerDataList[k].m_isOffLine)
                                {
                                    PlayService.m_serverUtil.sendMessage(playerDataList[k].m_connId, respondJO.ToString());
                                }
                            }
                        }

                        // 此玩家没有炒底，通知下一个人炒底
                        if (hasPoker == 0)
                        {
                            PlayerData playerDataNext = null;
                            if (j == 3)
                            {
                                playerDataNext = room.getPlayerDataList()[0];
                            }
                            else if (j <= 2)
                            {
                                PlayService.log.Warn("该房间人数：" + room.getPlayerDataList().Count + "  J=" + j);

                                for (int m = 0; m < room.getPlayerDataList().Count; m++)
                                {
                                    PlayService.log.Warn("该房间玩家信息：" + room.getRoomId() +"    "+ room.getPlayerDataList()[m].m_uid);
                                }

                                playerDataNext = room.getPlayerDataList()[j + 1];
                            }
                            else
                            {
                                LogUtil.getInstance().addErrorLog("m_logFlag----doTask_PlayerChaoDi：J越界，直接开始游戏");
                                playerDataNext = room.m_zhuangjiaPlayerData;
                            }

                            // 抄底一轮后结束抄底，开始游戏
                            if (playerDataNext.m_uid.CompareTo(room.m_zhuangjiaPlayerData.m_uid) == 0)
                            {
                                // 开始本房间的比赛
                                doTask_CallPlayerOutPoker(gameBase, room, data, true);

                                room.m_roomState = RoomState.RoomState_gaming;
                            }
                            else
                            {
                                callPlayerChaoDi(gameBase, room, playerDataNext);
                            }
                        }

                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_PlayerChaoDi异常：" + ex.Message);
            PlayService.log.Warn(ex);
        }
    }

    // 推送上一个玩家出的牌,并让下一个玩家出牌
    public static void doTask_CallPlayerOutPoker(GameBase gameBase, RoomData room, string data, bool isFirst)
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

            // 闲家抓到的分数
            int getScore = 0;
            bool curRoundXianJiaWin = false;  // 这一轮出牌是否是闲家最大

            if (!isFirst)
            {
                // 一轮出牌结束
                if (room.m_curRoundFirstPlayer.m_uid.CompareTo(room.m_curOutPokerPlayer.m_uid) == 0)
                {
                    // 选出这一轮出的牌最大的人，作为下一轮先出牌的人
                    PlayerData playerData = CompareWhoMax.compareWhoMax(room);
                    room.m_curOutPokerPlayer = playerData;
                    room.m_curRoundFirstPlayer = playerData;
                    
                    // 如果是闲家这一轮出牌最大，则把这一轮出的牌中5、10、k加到他们的所得分数上
                    if (playerData.m_isBanker == 0)
                    {
                        curRoundXianJiaWin = true;

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
                    // 如果最后一轮出牌闲家赢了，则把底牌分数X2加给闲家
                    if (curRoundXianJiaWin)
                    {
                        int dipaiScore = 0;
                        for (int i = 0; i < room.getDiPokerList().Count; i++)
                        {
                            if (room.getDiPokerList()[i].m_num == 5)
                            {
                                dipaiScore += 5;
                            }
                            else if (room.getDiPokerList()[i].m_num == 10)
                            {
                                dipaiScore += 10;
                            }
                            // 13代表“K”
                            else if (room.getDiPokerList()[i].m_num == 13)
                            {
                                dipaiScore += 10;
                            }
                        }

                        dipaiScore *= 2;
                        room.m_getAllScore += dipaiScore;
                    }
                    gameBase.gameOver(room, data);

                    return;
                }
            }

            // 通知
            {
                JObject respondJO;
                {
                    respondJO = new JObject();

                    respondJO.Add("tag", tag);
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
                        PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId,
                            respondJO.ToString());
                    }
                }

                // 对当前出牌的人做其他处理
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    if (room.getPlayerDataList()[i].m_uid.CompareTo(room.m_curOutPokerPlayer.m_uid) == 0)
                    {
                        // 开始倒计时
                        room.getPlayerDataList()[i].m_timerUtil.startTimer(room.m_outPokerDur, TimerType.TimerType_outPoker);

                        // 如果离线了则托管出牌
                        if (room.getPlayerDataList()[i].m_isOffLine)
                        {
                            Thread.Sleep(room.m_tuoguanOutPokerDur);
                            TrusteeshipLogic.trusteeshipLogic_OutPoker(gameBase, room, room.getPlayerDataList()[i]);

                            break;
                        }   
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_CallPlayerOutPoker异常：" + ex.Message);
        }
    }

    // 收到玩家出的牌
    public static void doTask_ReceivePlayerOutPoker(GameBase gameBase, IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            RoomData room = null;

            // 找到玩家所在的房间
            for (int i = 0; i < gameBase.getRoomList().Count; i++)
            {
                List<PlayerData> playerDataList = gameBase.getRoomList()[i].getPlayerDataList();

                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                    {
                        // 停止倒计时
                        playerDataList[j].m_timerUtil.stopTimer();

                        room = gameBase.getRoomList()[i];

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
                            //LogUtil.getInstance().addDebugLog("出牌类型:"+CheckOutPoker.checkOutPokerType(outPokerList, room.m_levelPokerNum, room.m_masterPokerType).ToString());
                            CheckOutPoker.OutPokerType outPokerType = CheckOutPoker.checkOutPokerType(outPokerList, room.m_levelPokerNum, room.m_masterPokerType);
                            if (outPokerType == CheckOutPoker.OutPokerType.OutPokerType_ShuaiPai)
                            {
                                if (uid.CompareTo(room.m_curRoundFirstPlayer.m_uid) == 0)
                                {
                                    //检测是否甩牌成功
                                    List<PokerInfo> shuaiPaiPoker = PlayRuleUtil.GetShuaiPaiPoker(room, outPokerList);
                                    bool isSuccess = (shuaiPaiPoker.Count == 0 ? true : false);
                                    //PlayService.log.Info("甩牌结果:" + isSuccess);
                                    //   甩牌成功
                                    if (isSuccess)
                                    {
                                        // 提交任务
                                        {
                                            Request_ProgressTask.doRequest(uid, 208);
                                        }

                                        // 提交任务
                                        for (int k = 0; k < outPokerList.Count; k++)
                                        {
                                            if (outPokerList[k].m_num == 14)
                                            {
                                                Request_ProgressTask.doRequest(uid, 211);
                                            }
                                        }

                                        // 从此人牌堆里删除他出的牌
                                        {
                                            for (int m = 0; m < ja.Count; m++)
                                            {
                                                int num = Convert.ToInt32(ja[m]["num"]);
                                                int pokerType = Convert.ToInt32(ja[m]["pokerType"]);

                                                // 加到所有已出的牌集合里
                                                room.addOutPoker(num, pokerType);

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
                                    //  甩牌失败
                                    else
                                    {
                                        JObject respondJO;
                                        {
                                            respondJO = new JObject();

                                            respondJO.Add("tag", tag);
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

                                                // 加到所有已出的牌集合里
                                                room.addOutPoker(num, pokerType);

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

                                                        // 提交任务
                                                        if (num == 14)
                                                        {
                                                            Request_ProgressTask.doRequest(uid, 211);
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

                                        // 加到所有已出的牌集合里
                                        room.addOutPoker(num, pokerType);

                                        for (int n = playerDataList[j].getPokerList().Count - 1; n >= 0; n--)
                                        {
                                            if ((playerDataList[j].getPokerList()[n].m_num == num) &&
                                                ((int)playerDataList[j].getPokerList()[n].m_pokerType == pokerType))
                                            {
                                                // 加到当前这一轮出牌的牌堆里面
                                                playerDataList[j].m_curOutPokerList.Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));

                                                // 出的牌从自己的牌堆里删除
                                                {
                                                    playerDataList[j].getPokerList().RemoveAt(n);
                                                }

                                                // 提交任务
                                                if (num == 14)
                                                {
                                                    Request_ProgressTask.doRequest(uid, 211);
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
                                // 提交任务
                                if (outPokerType == CheckOutPoker.OutPokerType.OutPokerType_TuoLaJi)
                                {
                                    Request_ProgressTask.doRequest(uid, 204);

                                    for (int k = 0; k < outPokerList .Count / 2; k++)
                                    {
                                        Request_ProgressTask.doRequest(uid, 210);
                                    }
                                }

                                // 提交任务
                                if (outPokerType == CheckOutPoker.OutPokerType.OutPokerType_Double)
                                {
                                    Request_ProgressTask.doRequest(uid, 210);
                                }

                                // 提交任务
                                for(int k = 0; k < outPokerList.Count; k++)
                                {
                                    if (outPokerList[k].m_num == 14)
                                    {
                                        Request_ProgressTask.doRequest(uid, 211);
                                    }
                                }

                                for (int m = 0; m < ja.Count; m++)
                                {
                                    int num = Convert.ToInt32(ja[m]["num"]);
                                    int pokerType = Convert.ToInt32(ja[m]["pokerType"]);

                                    // 加到所有已出的牌集合里
                                    room.addOutPoker(num, pokerType);

                                    for (int n = playerDataList[j].getPokerList().Count - 1; n >= 0; n--)
                                    {
                                        if ((playerDataList[j].getPokerList()[n].m_num == num) &&
                                            ((int)playerDataList[j].getPokerList()[n].m_pokerType == pokerType))
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
            doTask_CallPlayerOutPoker(gameBase, room, jo.ToString(), false);
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_ReceivePlayerOutPoker异常：" + ex.Message);
        }
    }

    // 通知玩家开始埋底
    public static void callPlayerMaiDi(GameBase gameBase, RoomData room)
    {
        try
        {
            LogUtil.getInstance().addDebugLog("通知玩家开始埋底");

            room.m_curMaiDiPlayer = room.m_zhuangjiaPlayerData;

            JObject respondJO = new JObject();
            respondJO.Add("tag", room.m_tag);
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
            {
                // 开始倒计时
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    if (room.getPlayerDataList()[i].m_uid.CompareTo(room.m_zhuangjiaPlayerData.m_uid) == 0)
                    {
                        room.getPlayerDataList()[i].m_timerUtil.startTimer(room.m_maidiTime, TimerType.TimerType_maidi);
                    }
                }
                    
                // 给在线的人发送
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    if (!room.getPlayerDataList()[i].m_isOffLine)
                    {
                        PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, respondJO.ToString());
                    }
                }

                // 如果当前埋底的人离线
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    if ((room.getPlayerDataList()[i].m_isOffLine) && (room.getPlayerDataList()[i].m_uid.CompareTo(room.m_zhuangjiaPlayerData.m_uid) == 0))
                    {
                        TrusteeshipLogic.trusteeshipLogic_MaiDi(gameBase, room, room.getPlayerDataList()[i]);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":callPlayerMaiDi异常：" + ex.Message);
        }
    }

    // 通知玩家炒底
    public static void callPlayerChaoDi(GameBase gameBase, RoomData room, PlayerData playerData)
    {
        try
        {
            room.m_curChaoDiPlayer = playerData;

            // 通知
            {
                JObject respondJO;
                {
                    respondJO = new JObject();

                    respondJO.Add("tag", room.m_tag);
                    respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_CallPlayerChaoDi);
                    respondJO.Add("uid", playerData.m_uid);
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

                // 对当前出牌的人做其他处理
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    if (room.getPlayerDataList()[i].m_uid.CompareTo(playerData.m_uid) == 0)
                    {
                        // 开始倒计时
                        room.getPlayerDataList()[i].m_timerUtil.startTimer(room.m_chaodiTime, TimerType.TimerType_chaodi);

                        // 如果离线了，则托管炒底
                        if (room.getPlayerDataList()[i].m_isOffLine)
                        {
                            Thread.Sleep(room.m_tuoguanOutPokerDur);
                            TrusteeshipLogic.trusteeshipLogic_ChaoDi(gameBase, room, room.getPlayerDataList()[i]);
                        }

                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":callPlayerChaoDi异常：" + ex.Message);
        }
    }
}
