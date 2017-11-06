using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TLJCommon;
using TLJ_PlayService;

class PlayLogic_PVP
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

                case (int)TLJCommon.Consts.PlayAction.PlayAction_WaitMatchTimeOut:
                {
                    doTask_WaitMatchTimeOut(connId, data);
                }
                break;

                case (int) TLJCommon.Consts.PlayAction.PlayAction_ExitPVP:
                {
                    doTask_ExitPVP(connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_ExitGame:
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
                //if (gameroomtype.CompareTo(m_roomList[i].m_gameRoomType) == 0)
                if ((gameroomtype.CompareTo(m_roomList[i].m_gameRoomType) == 0) && (1 == m_roomList[i].m_rounds_pvp))
                {
                    if (m_roomList[i].joinPlayer(new PlayerData(connId, uid, false)))
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
                room.joinPlayer(new PlayerData(connId, uid, false));

                m_roomList.Add(room);
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
            GameUtil.checkRoomStartGame(room, m_tag, true);
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_JoinGame异常：" + ex.Message);
        }
    }

    void doTask_WaitMatchTimeOut(IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();

            RoomData room = getRoomByPlayerUid(uid);

            if (room != null)
            {
                if (room.m_roomState == RoomData.RoomState.RoomState_waiting)
                {
                    // 由机器人填充缺的人
                    int needAICount = 4 - room.getPlayerDataList().Count;
                    for (int i = 0; i < needAICount; i++)
                    {
                        string ai_uid = AIDataScript.getInstance().getOneAI();
                        if (ai_uid.CompareTo("") != 0)
                        {
                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + "给room:" + room.getRoomId() + "创建机器人：" + ai_uid);

                            PlayerData playerData = new PlayerData((IntPtr)(-1), ai_uid, true);
                            playerData.m_isOffLine = true;
                            room.joinPlayer(playerData);
                        }
                        else
                        {
                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + "机器人不足");
                        }
                    }
                }
            }

            // 检测房间人数是否可以开赛
            GameUtil.checkRoomStartGame(room, m_tag, true);
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
                        // 还回报名费
                        if (m_roomList[i].m_roomState == RoomData.RoomState.RoomState_waiting)
                        {
                            PVPGameRoomData pvpGameRoomData = PVPGameRoomDataScript.getInstance().getDataByRoomType(m_roomList[i].m_gameRoomType);
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

                                Request_SendMailToUser.doRequest(uid, "报名费返还", content, baomingfei);
                            }
                        }

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
                respondJO.Add("code", (int) TLJCommon.Consts.Code.Code_CommonFail);

                // 发送给客户端
                PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_ExitGame异常：" + ex.Message);
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
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_QiangZhu异常：" + ex.Message);
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

                                    if (!playerDataList[k].m_isOffLine)
                                    {
                                        PlayService.m_serverUtil.sendMessage(playerDataList[k].m_connId, respondJO.ToString());
                                    }
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
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_QiangZhu异常：" + ex.Message);
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
                            room.m_roomState = RoomData.RoomState.RoomState_gaming;

                            // 开始本房间的比赛
                            doTask_CallPlayerOutPoker(room, data, true);
                        }
                        // 本房间可以炒底则通知玩家炒底
                        else
                        {
                            room.m_roomState = RoomData.RoomState.RoomState_chaodi;

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
                                room.m_roomState = RoomData.RoomState.RoomState_gaming;

                                // 开始本房间的比赛
                                doTask_CallPlayerOutPoker(room, data, true);
                            }
                            else
                            {
                                callPlayerChaoDi(room, playerData);
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

    // 通知玩家出炒底
    public void callPlayerChaoDi(RoomData room, PlayerData playerData)
    {
        try
        {
            room.m_curChaoDiPlayer = playerData;

            // 通知
            {
                JObject respondJO;
                {
                    respondJO = new JObject();

                    respondJO.Add("tag", m_tag);
                    respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_CallPlayerChaoDi);
                    respondJO.Add("uid", playerData.m_uid);
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

                // 如果当前抄底的人离线了，单独处理
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    if (room.getPlayerDataList()[i].m_isOffLine)
                    {
                        if (room.getPlayerDataList()[i].m_uid.CompareTo(playerData.m_uid) == 0)
                        {
                            trusteeshipLogic_ChaoDi(room,room.getPlayerDataList()[i]);

                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":callPlayerChaoDi异常：" + ex.Message);
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

                            room.m_curMaiDiPlayer = playerDataList[j];
                            room.m_roomState = RoomData.RoomState.RoomState_othermaidi;
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

                            // 抄底一轮后结束抄底，开始游戏
                            if (playerData.m_uid.CompareTo(room.m_zhuangjiaPlayerData.m_uid) == 0)
                            {
                                // 开始本房间的比赛
                                doTask_CallPlayerOutPoker(room, data, true);

                                room.m_roomState = RoomData.RoomState.RoomState_gaming;
                            }
                            else
                            {
                                callPlayerChaoDi(room, playerData);
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

                    respondJO.Add("tag", m_tag);
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
                            trusteeshipLogic_OutPoker(room,respondJO.ToString(), room.getPlayerDataList()[i]);

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
                            //LogUtil.getInstance().addDebugLog("出牌类型:"+CheckOutPoker.checkOutPokerType(outPokerList, room.m_levelPokerNum, room.m_masterPokerType).ToString());
                            CheckOutPoker.OutPokerType outPokerType = CheckOutPoker.checkOutPokerType(outPokerList, room.m_levelPokerNum, room.m_masterPokerType);
                            if (outPokerType == CheckOutPoker.OutPokerType.OutPokerType_ShuaiPai)
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

                                            respondJO.Add("tag", m_tag);
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
                                // 提交任务
                                if (outPokerType == CheckOutPoker.OutPokerType.OutPokerType_TuoLaJi)
                                {
                                    Request_ProgressTask.doRequest(room.getPlayerDataList()[i].m_uid, 204);
                                }

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
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_ReceivePlayerOutPoker异常：" + ex.Message);
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
                    respondJO.Add("tag", m_tag);
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
                            removeRoom(room);
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

                        GameUtil.checkRoomStartGame(room, m_tag, false);
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
                            respondJO.Add("tag", m_tag);
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
                        removeRoom(cur_room);
                    }
                }

                RoomData room = null;
                
                // 在已有的房间寻找可以加入的房间
                for (int i = 0; i < m_roomList.Count; i++)
                {
                    if (m_roomList[i].m_roomState == RoomData.RoomState.RoomState_waiting)
                    {
                        if (m_roomList[i].joinPlayer(new PlayerData(connId, uid, false)))
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
                    room.joinPlayer(new PlayerData(connId, uid, false));

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
                GameUtil.checkRoomStartGame(room, m_tag, true);
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
                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ".doTask_Chat:未找到此人所在房间：" + uid);
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_Chat异常：" + ex.Message);
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
                        // 记录逃跑数据
                        if ((m_roomList[i].m_roomState != RoomData.RoomState.RoomState_waiting) &&
                            (m_roomList[i].m_roomState != RoomData.RoomState.RoomState_end))
                        {
                            Request_RecordUserGameData.doRequest(room.getPlayerDataList()[i].m_uid, (int)TLJCommon.Consts.GameAction.GameAction_Run);
                        }

                        switch (m_roomList[i].m_roomState)
                        {
                            case RoomData.RoomState.RoomState_waiting:
                                {
                                    LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在本桌满人之前退出：" + playerDataList[j].m_uid);

                                    playerDataList.RemoveAt(j);

                                    checkAllOffLine(room);

                                    if (GameUtil.checkRoomNonePlayer(room))
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + room);
                                        removeRoom(room);
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            case RoomData.RoomState.RoomState_qiangzhu:
                                {
                                    if (!playerDataList[j].m_isOffLine)
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在抢主阶段退出：" + playerDataList[j].m_uid);

                                        playerDataList[j].m_isOffLine = true;

                                        checkAllOffLine(room);

                                        trusteeshipLogic_QiangZhu(room, playerDataList[j]);
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            case RoomData.RoomState.RoomState_zhuangjiamaidi:
                                {
                                    if (!playerDataList[j].m_isOffLine)
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在庄家埋底阶段退出：" + playerDataList[j].m_uid);

                                        playerDataList[j].m_isOffLine = true;

                                        checkAllOffLine(room);

                                        trusteeshipLogic_MaiDi(room, playerDataList[j]);
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

                            case RoomData.RoomState.RoomState_chaodi:
                                {
                                    if (!playerDataList[j].m_isOffLine)
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在抄底阶段退出：" + playerDataList[j].m_uid);

                                        playerDataList[j].m_isOffLine = true;

                                        checkAllOffLine(room);

                                        trusteeshipLogic_ChaoDi(room,playerDataList[j]);
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            case RoomData.RoomState.RoomState_othermaidi:
                                {
                                    if (!playerDataList[j].m_isOffLine)
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在Other埋底阶段退出：" + playerDataList[j].m_uid);

                                        playerDataList[j].m_isOffLine = true;

                                        checkAllOffLine(room);

                                        trusteeshipLogic_MaiDi(room, playerDataList[j]);
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            case RoomData.RoomState.RoomState_gaming:
                                {
                                    if (!playerDataList[j].m_isOffLine)
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在游戏中退出：" + playerDataList[j].m_uid);
                                        playerDataList[j].m_isOffLine = true;

                                        checkAllOffLine(room);

                                        // 如果当前房间正好轮到此人出牌
                                        if (m_roomList[i].m_curOutPokerPlayer.m_uid.CompareTo(playerDataList[j].m_uid) == 0)
                                        {
                                            trusteeshipLogic_OutPoker(m_roomList[i], playerDataList[j]);
                                        }
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            case RoomData.RoomState.RoomState_end:
                                {
                                    LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在本桌打完后退出：" + playerDataList[j].m_uid);

                                    playerDataList.RemoveAt(j);
                                    if (playerDataList.Count == 0)
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + m_roomList[i].getRoomId());
                                        removeRoom(room);
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
                                                removeRoom(room);
                                            }
                                        }
                                    }
                                }
                                break;

                            default:
                                {
                                    LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在未知阶段退出：" + playerDataList[j].m_uid);

                                    playerDataList.RemoveAt(j);

                                    if (GameUtil.checkRoomNonePlayer(room))
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + room.getRoomId());
                                        removeRoom(room);
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

    // 通知玩家开始埋底
    public void callPlayerMaiDi(RoomData room)
    {
        try
        {
            room.m_curMaiDiPlayer = room.m_zhuangjiaPlayerData;

            JObject respondJO = new JObject();
            respondJO.Add("tag", m_tag);
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
                    room.m_zhuangjiaPlayerData.getPokerList().Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType) pokerType));
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
                    if (room.getPlayerDataList()[i].m_uid.CompareTo(room.m_curMaiDiPlayer.m_uid) == 0)
                    {
                        trusteeshipLogic_MaiDi(room, room.getPlayerDataList()[i]);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":callPlayerMaiDi异常：" + ex.Message);
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

    // 托管:出牌
    void trusteeshipLogic_OutPoker(RoomData room, string jsonData, PlayerData playerData)
    {
        try
        {
            Thread.Sleep(room.m_tuoguanOutPokerDur);

            JObject jo = JObject.Parse(jsonData);
            // 轮到自己出牌
            {
                if (playerData.getPokerList().Count > 0)
                {
                    JObject backData = new JObject();

                    backData.Add("tag", m_tag);
                    backData.Add("uid", playerData.m_uid);
                    backData.Add("playAction", (int) TLJCommon.Consts.PlayAction.PlayAction_PlayerOutPoker);

                    // 自己出的牌
                    {
                        // 任意出
                        if (playerData.m_uid.CompareTo(room.m_curRoundFirstPlayer.m_uid) == 0)
                        {
                            //int num = playerData.getPokerList()[playerData.getPokerList().Count - 1].m_num;
                            //int pokerType = (int)playerData.getPokerList()[playerData.getPokerList().Count - 1].m_pokerType;

                            //JArray jarray = new JArray();
                            //{
                            //    JObject temp = new JObject();
                            //    temp.Add("num", num);
                            //    temp.Add("pokerType", pokerType);
                            //    jarray.Add(temp);
                            //}
                            //backData.Add("pokerList", jarray);

                            List<TLJCommon.PokerInfo> listPoker = PlayRuleUtil.GetPokerWhenFirst(playerData.getPokerList(), room.m_levelPokerNum, room.m_masterPokerType);
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

                    //LogUtil.getInstance().addDebugLog(m_logFlag + "----" + "托管出牌：" + playerData.m_uid);
                    doTask_ReceivePlayerOutPoker(playerData.m_connId, backData.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":trusteeshipLogic异常1：" + ex.Message);
        }
    }

    // 托管:出牌
    void trusteeshipLogic_OutPoker(RoomData room, PlayerData playerData)
    {
        try
        {
            Thread.Sleep(room.m_tuoguanOutPokerDur);

            // 轮到自己出牌
            {
                if (playerData.getPokerList().Count > 0)
                {
                    JObject backData = new JObject();

                    backData.Add("tag", m_tag);
                    backData.Add("uid", playerData.m_uid);
                    backData.Add("playAction", (int) TLJCommon.Consts.PlayAction.PlayAction_PlayerOutPoker);

                    // 自己出的牌
                    {
                        // 任意出
                        if (playerData.m_uid.CompareTo(room.m_curRoundFirstPlayer.m_uid) == 0)
                        {
                            //int num = playerData.getPokerList()[playerData.getPokerList().Count - 1].m_num;
                            //int pokerType = (int)playerData.getPokerList()[playerData.getPokerList().Count - 1].m_pokerType;

                            //JArray jarray = new JArray();
                            //{
                            //    JObject temp = new JObject();
                            //    temp.Add("num", num);
                            //    temp.Add("pokerType", pokerType);
                            //    jarray.Add(temp);
                            //}
                            //backData.Add("pokerList", jarray);

                            List<TLJCommon.PokerInfo> listPoker = PlayRuleUtil.GetPokerWhenFirst(playerData.getPokerList(), room.m_levelPokerNum, room.m_masterPokerType);
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

                    //LogUtil.getInstance().addDebugLog(m_logFlag + "----" + "托管出牌：" + playerData.m_uid);
                    doTask_ReceivePlayerOutPoker(playerData.m_connId, backData.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":trusteeshipLogic异常2：" + ex.Message);
        }
    }

    // 托管:抢主
    void trusteeshipLogic_QiangZhu(RoomData room, PlayerData playerData)
    {
        Thread.Sleep(room.m_tuoguanOutPokerDur);

        bool isAllOffLine = true;
        for (int k = 0; k < room.getPlayerDataList().Count; k++)
        {
            if (!room.getPlayerDataList()[k].m_isOffLine)
            {
                isAllOffLine = false;
                break;
            }
        }

        if (isAllOffLine)
        {
            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":托管：帮" + playerData.m_uid + "抢主：本房间所有人都离线，我被迫抢主");

            JObject data = new JObject();

            data["tag"] = m_tag;
            data["uid"] = playerData.m_uid;
            data["playAction"] = (int)TLJCommon.Consts.PlayAction.PlayAction_QiangZhuEnd;

            doTask_QiangZhuEnd(playerData.m_connId, data.ToString());
        }
        else
        {
            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":托管：帮" + playerData.m_uid + "抢主：放弃抢主");
        }
    }

    // 托管:埋底
    void trusteeshipLogic_MaiDi(RoomData room, PlayerData playerData)
    {
        try
        {
            Thread.Sleep(room.m_tuoguanOutPokerDur);

            if (room.m_curMaiDiPlayer.m_uid.CompareTo(playerData.m_uid) == 0)
            {
                JObject data = new JObject();
                data.Add("tag", m_tag);
                data.Add("uid", playerData.m_uid);
                data.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_MaiDi);

                List<TLJCommon.PokerInfo> myOutPokerList = new List<TLJCommon.PokerInfo>();

                if (playerData.getPokerList().Count < 8)
                {
                    LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":托管埋底:" + "数量不足");
                    return;
                }

                // 自己出的牌
                {
                    JArray ja = new JArray();
                    for (int k = 1; k <= 8; k++)
                    {
                        JObject jo = new JObject();
                        jo.Add("num", playerData.getPokerList()[playerData.getPokerList().Count - k].m_num);
                        jo.Add("pokerType", (int)playerData.getPokerList()[playerData.getPokerList().Count - k].m_pokerType);

                        ja.Add(jo);
                    }

                    data.Add("diPokerList", ja);
                }

                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":托管：帮" + playerData.m_uid + "埋底:" + data.ToString());
                doTask_MaiDi(playerData.m_connId, data.ToString());
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ".trusteeshipLogic_MaiDi: " + ex.Message);
        }
    }

    // 托管:抄底
    void trusteeshipLogic_ChaoDi(RoomData room,PlayerData playerData)
    {
        Thread.Sleep(room.m_tuoguanOutPokerDur);

        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":托管：帮" + playerData.m_uid + "抄底");

        JObject data = new JObject();

        data["tag"] = m_tag;
        data["uid"] = playerData.m_uid;
        data["playAction"] = (int)TLJCommon.Consts.PlayAction.PlayAction_PlayerChaoDi;
        data["hasPoker"] = 0;

        doTask_PlayerChaoDi(playerData.m_connId, data.ToString());
    }

    void checkAllOffLine(RoomData room)
    {
        bool isAllOffLine = true;
        for (int i = 0; i < room.getPlayerDataList().Count; i++)
        {
            if (!room.getPlayerDataList()[i].m_isOffLine)
            {
                isAllOffLine = false;
                break;
            }
        }

        if (isAllOffLine)
        {
            room.m_tuoguanOutPokerDur = 100;
        }
    }

    void removeRoom(RoomData room)
    {
        // 把机器人还回去
        for (int i = 0; i < room.getPlayerDataList().Count; i++)
        {
            if (room.getPlayerDataList()[i].m_isAI)
            {
                AIDataScript.getInstance().backOneAI(room.getPlayerDataList()[i].m_uid);
            }
        }

        m_roomList.Remove(room);
    }

    //------------------------------------------------------------------以上内容休闲场和PVP逻辑一样--------------------------------------------------------------

    // 游戏结束
    void gameOver(RoomData now_room, string data)
    {
        try
        {
            //LogUtil.getInstance().addDebugLog("比赛结束，解散该房间：" + now_room.getRoomId());
            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":比赛结束,roomid = :" + now_room.getRoomId());

            // 计算每个玩家的金币（积分）
            GameUtil.setPlayerScore(now_room,false);

            List<PlayerData> winPlayerList = new List<PlayerData>();

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
                            }

                            // 记录胜利次数数据
                            {
                                Request_RecordUserGameData.doRequest(now_room.getPlayerDataList()[i].m_uid, (int)TLJCommon.Consts.GameAction.GameAction_Win);
                            }
                        }
                        // 加入淘汰人员列表里
                        else
                        {
                            PVPChangCiUtil.getInstance().addPlayerToThere(now_room.m_gameRoomType, now_room.getPlayerDataList()[i]);
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
                                Request_ProgressTask.doRequest(now_room.getPlayerDataList()[i].m_uid, 203);
                            }

                            // 记录胜利次数数据
                            {
                                Request_RecordUserGameData.doRequest(now_room.getPlayerDataList()[i].m_uid, (int)TLJCommon.Consts.GameAction.GameAction_Win);
                            }
                        }
                        // 加入淘汰人员列表里
                        else
                        {
                            PVPChangCiUtil.getInstance().addPlayerToThere(now_room.m_gameRoomType, now_room.getPlayerDataList()[i]);
                        }
                    }
                }
            }

            for (int i = 0; i < winPlayerList.Count; i++)
            {
                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":胜利的人：" + winPlayerList[i].m_uid + "  isBanker:" + winPlayerList[i].m_isBanker);
            }

            JObject jo = JObject.Parse(data);

            // 通知
            {
                JObject respondJO;
                {
                    respondJO = new JObject();

                    respondJO.Add("tag", m_tag);
                    respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_GameOver);
                    respondJO.Add("getAllScore", now_room.m_getAllScore);
                    respondJO.Add("isBankerWin", now_room.m_getAllScore >= 80 ? 0 : 1);
                    respondJO.Add("pre_uid", jo.GetValue("uid"));
                    respondJO.Add("pre_outPokerList", jo.GetValue("pokerList"));
                }

                // 给在线的人推送
                for (int i = 0; i < now_room.getPlayerDataList().Count; i++)
                {
                    // 推送给客户端
                    if (!now_room.getPlayerDataList()[i].m_isOffLine)
                    {
                        PlayService.m_serverUtil.sendMessage(now_room.getPlayerDataList()[i].m_connId,
                            respondJO.ToString());
                    }

                    if (now_room.getPlayerDataList()[i].m_isAI)
                    {
                        AIDataScript.getInstance().backOneAI(now_room.getPlayerDataList()[i].m_uid);
                    }
                }
            }

            now_room.m_roomState = RoomData.RoomState.RoomState_end;

            //// 检查是否删除该房间
            //{
            //    if (GameUtil.checkRoomNonePlayer(now_room))
            //    {
            //        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":所有人都离线，解散该房间：" + now_room.getRoomId());
            //        removeRoom(now_room);

            //        return;
            //    }
            //}

            string gameRoomType = now_room.m_gameRoomType;
            int rounds_pvp = now_room.m_rounds_pvp;
            
            // 检查是否打到最后一轮
            bool isContiune = true;
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
                            jueshengju(now_room);

                            return;
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
                            jueshengju(now_room);

                            return;
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
                            jueshengju(now_room);

                            return;
                        }
                    }
                    break;
                }
            }

            // 进入新房间，准备开始下一局
            if (isContiune)
            {
                // 删除该房间
                {
                    LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":本局打完，强制解散该房间：" + now_room.getRoomId());
                    removeRoom(now_room);
                }

                for (int i = 0; i < winPlayerList.Count; i++)
                {
                    if (!winPlayerList[i].m_isOffLine)
                    {
                        {
                            RoomData room = null;

                            // 在已有的房间寻找可以加入的房间
                            for (int j = 0; j < m_roomList.Count; j++)
                            {
                                if ((gameRoomType.CompareTo(m_roomList[j].m_gameRoomType) == 0) && ((rounds_pvp + 1) == m_roomList[j].m_rounds_pvp))
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
                                room = new RoomData(m_roomList.Count + 1, gameRoomType);
                                room.joinPlayer(winPlayerList[i]);
                                room.m_rounds_pvp = now_room.m_rounds_pvp + 1;

                                m_roomList.Add(room);

                                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":创建新房间：" + room.getRoomId());
                            }

                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":该新房间人数：" + room.getPlayerDataList().Count);

                            //// 加入房间成功，给客户端回复
                            //{
                            //    JObject respondJO = new JObject();
                            //    respondJO.Add("tag", m_tag);
                            //    respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_JoinGame);
                            //    respondJO.Add("gameroomtype", room.m_gameRoomType);
                            //    respondJO.Add("code", (int)TLJCommon.Consts.Code.Code_OK);
                            //    respondJO.Add("roomId", room.getRoomId());

                            //    // 发送给客户端
                            //    PlayService.m_serverUtil.sendMessage(winPlayerList[i].m_connId, respondJO.ToString());
                            //}

                            if (room.getPlayerDataList().Count == 4)
                            {
                                // 延迟3秒开赛
                                Thread.Sleep(3000);

                                // 检测房间人数是否可以开赛
                                GameUtil.checkRoomStartGame(room, m_tag, true);
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
                            string title = curPVPRoomPlayerList + "奖励";
                            string content = "恭喜您在" + gameroomname + "获得第" + curPVPRoomPlayerList.m_playerList[i].m_rank + "名，为您送上专属奖励";
                            Request_SendMailToUser.doRequest(curPVPRoomPlayerList.m_playerList[i].m_uid, title, content, curPVPRoomPlayerList.m_playerList[i].m_pvpReward);
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
                        if (!now_room.getPlayerDataList()[i].m_isOffLine)
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
                    removeRoom(now_room);
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
                if (!room.getPlayerDataList()[i].m_isOffLine)
                {
                    PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, respondJO.ToString());
                }
            }
        }

        // 创建决胜局房间
        {
            RoomData new_room = new RoomData(m_roomList.Count + 1, room.m_gameRoomType);
            new_room.m_rounds_pvp = room.m_rounds_pvp + 1;
            m_roomList.Add(new_room);

            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":创建新房间：" + new_room.getRoomId());

            for (int i = 0; i < room.getPlayerDataList().Count; i++)
            {
                PlayerData playData = new PlayerData(room.getPlayerDataList()[i].m_connId, room.getPlayerDataList()[i].m_uid, room.getPlayerDataList()[i].m_isAI);
                playData.m_score = room.getPlayerDataList()[i].m_score;
                new_room.joinPlayer(playData);
            }
            
            {
                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + "删除旧房间：" + room.getRoomId());
                removeRoom(room);
            }

            // 开始决胜局
            {
                // 延迟3秒开赛
                Thread.Sleep(3000);

                // 检测房间人数是否可以开赛
                GameUtil.checkRoomStartGame(new_room, m_tag, true);
            }
        }
    }
}