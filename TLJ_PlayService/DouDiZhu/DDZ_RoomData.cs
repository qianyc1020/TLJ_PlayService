using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;
using TLJCommon;
using static TLJCommon.Consts;

public class DDZ_RoomData
{
    int m_roomId;
    public string m_gameRoomType;
    public int m_outPokerDur = 15000;       // 出牌时间：毫秒
    public int m_tuoguanOutPokerDur = 2000; // 托管出牌时间:毫秒
    public int m_matchTime = 15000;         // 匹配队友时间：毫秒
    public int m_qiangDiZhuTime = 10000;    // 抢地主时间：毫秒
    public int m_jiaBangTime = 10000;       // 加棒时间：毫秒
    public int m_roomAliveTime = 900000;    // 房间生命周期：毫秒：15分钟
    public int m_fapaiDurTime = 400;        // 发牌间隔：毫秒

    public string m_tag;                    // "DouDiZhu_Game"

    public bool m_isStartGame = false;
    DDZ_RoomState m_roomState = DDZ_RoomState.RoomState_waiting;

    public DDZ_PlayerData m_diZhuPlayer = null;
    public DDZ_PlayerData m_curOutPokerPlayer = null;
    public DDZ_PlayerData m_firstQiangDiZhuPlayer = null;
    public DDZ_PlayerData m_curQiangDiZhuPlayer = null;

    public List<TLJCommon.PokerInfo> m_allOutPokerList = new List<TLJCommon.PokerInfo>();

    // 本房间玩家信息
    List<DDZ_PlayerData> m_playerDataList = new List<DDZ_PlayerData>();

    public DDZ_PlayerData m_maxJiaoFenPlayerData = null;    // 当前叫分最大的人
    public DDZ_PlayerData biggestPlayerData = null;         // 当前出牌最大的人
    public DDZ_PlayerData m_winPlayerData = null;

    // 底牌
    List<TLJCommon.PokerInfo> m_DiPokerList = new List<TLJCommon.PokerInfo>();

    public TimerUtil m_timerUtil = new TimerUtil();
    public TimerUtil m_timerUtil_breakRom = new TimerUtil();

    int m_fapaiIndex = 0;
    public TimerUtil m_timerUtil_FaPai = new TimerUtil();

    public DDZ_GameBase m_gameBase;

    public float m_beishu_bomb = 1;
    public float m_beishu_chuntian = 1;

    public void clearData()
    {
        m_fapaiIndex = 0;
        m_isStartGame = false;
        m_roomState = DDZ_RoomState.RoomState_waiting;

        m_curOutPokerPlayer = null;

        m_DiPokerList.Clear();

        for (int i = 0; i < m_playerDataList.Count; i++)
        {
            m_playerDataList[i].clearData();
        }
    }

    public DDZ_RoomData(DDZ_GameBase gameBase, string gameRoomType)
    {
        m_gameBase = gameBase;
        m_roomId = RoomManager.getOneRoomID();
        m_gameRoomType = gameRoomType;

        m_tag = TLJCommon.Consts.Tag_DouDiZhu_Game;

        m_timerUtil.setTimerCallBack(timerCallback);
        m_timerUtil_breakRom.setTimerCallBack(timerCallback_breakRom);
        m_timerUtil_FaPai.setTimerCallBack(timerCallback_fapai);

        // 匹配队友倒计时
        m_timerUtil.startTimer(RandomUtil.getRandom(3, m_matchTime), TimerType.TimerType_waitMatchTimeOut);
    }

    // 强制解散房间倒计时
    public void startBreakRoomTimer()
    {
        m_timerUtil_breakRom.startTimer(m_roomAliveTime, TimerType.TimerType_breakRoom);
    }

    // 开始定时发牌
    public void startFaPaiTimer()
    {
        m_timerUtil_FaPai.startTimer(m_fapaiDurTime, TimerType.TimerType_fapai);
    }

    void timerCallback(object obj)
    {
        try
        {
            switch ((TimerType)obj)
            {
                case TimerType.TimerType_waitMatchTimeOut:
                    {
                        if (m_roomState == DDZ_RoomState.RoomState_waiting)
                        {
                            LogUtil.getInstance().writeRoomLog(this, "匹配时间结束，给房间添加机器人");
                            DDZ_GameLogic.doTask_WaitMatchTimeOut(this);
                        }
                    }
                    break;

                case TimerType.TimerType_pvpNextStartGame:
                    {
                        // 检测房间人数是否可以开赛
                        DDZ_GameLogic.checkRoomStartGame(this, m_tag);
                    }
                    break;

                case TimerType.TimerType_callPlayerOutPoker:
                    {
                        // 让下一个人出牌
                        DDZ_GameLogic.doTask_CallPlayerOutPoker(m_gameBase, this, m_curOutPokerPlayer);
                    }
                    break;

                case TimerType.TimerType_gameOver:
                    {
                        m_gameBase.gameOver(this);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error("RoomData----" + "timerCallback异常: " + ex);
        }
    }

    void timerCallback_breakRom(object obj)
    {
        try
        {
            switch ((TimerType)obj)
            {
                case TimerType.TimerType_breakRoom:
                    {

                        LogUtil.getInstance().writeRoomLog(this, "房间时限已到，强制解散该房间");

                        m_timerUtil.stopTimer();

                        // 发送给客户端
                        {
                            JObject respondJO = new JObject();
                            respondJO.Add("tag", m_tag);
                            respondJO.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_BreakRoom);

                            for (int i = 0; i < m_playerDataList.Count; i++)
                            {
                                if ((!m_playerDataList[i].m_isAI) && (!m_playerDataList[i].isOffLine()))
                                {
                                    PlayService.m_serverUtil.sendMessage(m_playerDataList[i].m_connId, respondJO.ToString());
                                }
                            }
                        }

                        // 强制解散该房间
                        {
                            // 所有玩家倒计时停止
                            for (int i = 0; i < m_playerDataList.Count; i++)
                            {
                                m_playerDataList[i].m_timerUtil.stopTimer();
                            }

                            DDZ_GameLogic.removeRoom(m_gameBase, this,true);
                        }

                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error("RoomData----" + "timerCallback_breakRom异常: " + ex);
        }
    }

    void timerCallback_fapai(object obj)
    {
        try
        {
            switch ((TimerType)obj)
            {
                case TimerType.TimerType_fapai:
                    {
                        m_timerUtil_FaPai.stopTimer();

                        for (int i = 0; i< getPlayerDataList().Count ; i++)
                        {
                            int num = getPlayerDataList()[i].getPokerList()[m_fapaiIndex].m_num;
                            int pokerType = (int)getPlayerDataList()[i].getPokerList()[m_fapaiIndex].m_pokerType;
                            
                            //if (!getPlayerDataList()[i].isOffLine())
                            {
                                JObject jo2 = new JObject();
                                jo2.Add("tag", m_tag);
                                jo2.Add("uid", getPlayerDataList()[i].m_uid);
                                jo2.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_FaPai);
                                jo2.Add("num", num);
                                jo2.Add("pokerType", pokerType);

                                if (m_fapaiIndex == 16)
                                {
                                    jo2.Add("isEnd", 1);
                                }
                                else
                                {
                                    jo2.Add("isEnd", 0);
                                }

                                PlayService.m_serverUtil.sendMessage(getPlayerDataList()[i].m_connId, jo2.ToString());
                            }

                            getPlayerDataList()[i].m_allotPokerList.Add(new PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));

                            LogUtil.getInstance().writeRoomLog(this, "发牌给：" + getPlayerDataList()[i].m_uid + "----num = " + num + "  pokerType = " + pokerType);
                        }

                        // 牌未发完
                        if (m_fapaiIndex < 16)
                        {
                            ++m_fapaiIndex;

                            startFaPaiTimer();
                        }
                        // 牌已发完
                        else
                        {
                            for (int i = 0; i < getDiPokerList().Count; i++)
                            {
                                LogUtil.getInstance().writeRoomLog(this, "底牌：----num = " + getDiPokerList()[i].m_num + "  pokerType = " + (int)getDiPokerList()[i].m_pokerType);
                            }

                            // 随机一个玩家开始抢地主
                            {
                                m_roomState = DDZ_RoomState.RoomState_qiangdizhu;

                                DDZ_PlayerData playerData = null;
                                int r = RandomUtil.getRandom(0, getPlayerDataList().Count - 1);
                                //r = 0;
                                playerData = getPlayerDataList()[r];

                                m_firstQiangDiZhuPlayer = playerData;
                                m_curQiangDiZhuPlayer = playerData;

                                // 开始倒计时
                                playerData.m_timerUtil.startTimer(m_qiangDiZhuTime, TimerType.TimerType_qiangDizhu);

                                JObject jo2 = new JObject();
                                jo2.Add("tag", m_tag);
                                jo2.Add("curMaxFen", 0);
                                jo2.Add("curJiaoDiZhuUid", m_curQiangDiZhuPlayer.m_uid);
                                jo2.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_CallPlayerQiangDiZhu);

                                for (int i = 0; i < getPlayerDataList().Count; i++)
                                {
                                    PlayService.m_serverUtil.sendMessage(getPlayerDataList()[i].m_connId, jo2.ToString());
                                }

                                // 如果离线了则托管出牌
                                if (playerData.isTuoGuan())
                                {
                                    // 开始倒计时
                                    playerData.m_timerUtilOffLine.startTimer(m_tuoguanOutPokerDur, TimerType.TimerType_qiangDizhu);
                                }
                            }
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error("DDZ_RoomData----" + "timerCallback_fapai异常: " + ex);
        }
    }

    public int getRoomId()
    {
        return m_roomId;
    }

    public DDZ_RoomState getRoomState()
    {
        return m_roomState;
    }

    public void setRoomState(DDZ_RoomState roomState)
    {
        m_roomState = roomState;
    }

    public List<DDZ_PlayerData> getPlayerDataList()
    {
        return m_playerDataList;
    }

    public DDZ_PlayerData getPlayerDataByUid(string uid)
    {
        for (int i = 0; i < m_playerDataList.Count; i++)
        {
            if (m_playerDataList[i].m_uid.CompareTo(uid) == 0)
            {
                return m_playerDataList[i];
            }
        }

        return null;
    }

    // 根据uid查找此人上一个出牌的人
    public DDZ_PlayerData getBeforePlayerData(string uid)
    {
        int index = 0;
        for (int i = 0; i < m_playerDataList.Count; i++)
        {
            if (m_playerDataList[i].m_uid.CompareTo(uid) == 0)
            {
                index = i;
                break;
            }
        }

        if (index == 0)
        {
            return m_playerDataList[m_playerDataList.Count - 1];
        }
        else
        {
            return m_playerDataList[index - 1];
        }
    }

    public bool joinPlayer(DDZ_PlayerData playerData)
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

    public void deletePlayer(string uid)
    {
        for (int i = 0; i < m_playerDataList.Count; i++)
        {
            if (m_playerDataList[i].m_uid.CompareTo(uid) == 0)
            {
                m_playerDataList.RemoveAt(i);
                break;
            }
        }
    }

    public void addOutPoker(int num,int pokerType)
    {
        m_allOutPokerList.Add(new TLJCommon.PokerInfo(num, (TLJCommon.Consts.PokerType)pokerType));
    }

    public int getBeiShuByUid(string uid)
    {
        float beishu = m_maxJiaoFenPlayerData.m_jiaofen * m_beishu_chuntian * m_beishu_bomb;

        if (getPlayerDataByUid(uid).m_isJiaBang == 1)
        {
            beishu *= 2;
        }
        return (int)beishu;
    }
}
