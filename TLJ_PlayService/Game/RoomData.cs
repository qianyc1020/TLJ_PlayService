using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;
using static TLJCommon.Consts;

public class RoomData
{
    int m_roomId;
    public int m_wanfaType;
    public string m_gameRoomType;
    public int m_rounds_pvp = 1;
    public int m_outPokerDur = 15000;       // 出牌时间：毫秒
    public int m_tuoguanOutPokerDur = 2000; // 托管出牌时间:毫秒
    //public int m_tuoguanOutPokerDur = 500; // 托管出牌时间:毫秒
    public int m_qiangzhuTime = 10000;      // 抢主时间：毫秒
    public int m_maidiTime = 40000;         // 埋底时间：毫秒
    public int m_chaodiTime = 10000;        // 炒底时间：毫秒
    public int m_matchTime = 30000;         // 匹配队友时间：毫秒
    public int m_roomAliveTime = 900000;    // 房间生命周期：毫秒：15分钟
    //public int m_roomAliveTime = 300000;    // 房间生命周期：毫秒：5分钟
    
    public string m_tag;

    public bool m_isStartGame = false;
    public bool m_canChaoDi = true;
    RoomState m_roomState = RoomState.RoomState_waiting;

    public PlayerData m_curOutPokerPlayer = null;
    public PlayerData m_curMaiDiPlayer = null;
    public PlayerData m_curChaoDiPlayer = null;
    public PlayerData m_curRoundFirstPlayer = null;
    public PlayerData m_lastMaiDiPlayer = null;

    public List<TLJCommon.PokerInfo> m_allOutPokerList = new List<TLJCommon.PokerInfo>();

    // 本房间玩家信息
    List<PlayerData> m_playerDataList = new List<PlayerData>();

    // 底牌
    List<TLJCommon.PokerInfo> m_DiPokerList = new List<TLJCommon.PokerInfo>();

    // 默认为-1，代表没有被赋值过
    public int m_levelPokerNum = -1; // 级牌

    public int m_masterPokerType = -1; // 主牌花色

    // 上一次玩家抢主的牌
    public List<TLJCommon.PokerInfo> m_qiangzhuPokerList = new List<TLJCommon.PokerInfo>();

    public int m_getAllScore = 0; // 庄家对家抓到的分数

    public PlayerData m_zhuangjiaPlayerData = null;

    public TimerUtil m_timerUtil = new TimerUtil();
    public TimerUtil m_timerUtil_breakRom = new TimerUtil();

    public GameBase m_gameBase;

    public void clearData()
    {
        m_isStartGame = false;
        m_roomState = RoomState.RoomState_waiting;

        m_curOutPokerPlayer = null;
        m_curRoundFirstPlayer = null;

        m_DiPokerList.Clear();
        m_qiangzhuPokerList.Clear();
        m_getAllScore = 0;
        m_zhuangjiaPlayerData = null;

        for (int i = 0; i < m_playerDataList.Count; i++)
        {
            m_playerDataList[i].clearData();
        }
    }

    public RoomData(GameBase gameBase, string gameRoomType)
    {
        m_gameBase = gameBase;
        m_roomId = RoomManager.getOneRoomID();
        m_gameRoomType = gameRoomType;

        // 场次tag
        {
            List<string> list = new List<string>();
            CommonUtil.splitStr(m_gameRoomType, list, '_');
            if (list[0].CompareTo("PVP") == 0)
            {
                m_tag = TLJCommon.Consts.Tag_JingJiChang;
                m_wanfaType = (int)TLJCommon.Consts.WanFaType.WanFaType_PVP;
            }
            if (list[0].CompareTo("XiuXian") == 0)
            {
                m_tag = TLJCommon.Consts.Tag_XiuXianChang;
                m_wanfaType = (int)TLJCommon.Consts.WanFaType.WanFaType_Relax;
            }
        }

        // 经典场不可抄底
        if ((m_gameRoomType.CompareTo(TLJCommon.Consts.GameRoomType_XiuXian_JingDian_ChuJi) == 0) ||
            (m_gameRoomType.CompareTo(TLJCommon.Consts.GameRoomType_XiuXian_JingDian_ZhongJi) == 0) ||
            (m_gameRoomType.CompareTo(TLJCommon.Consts.GameRoomType_XiuXian_JingDian_GaoJi) == 0))
        {
            m_canChaoDi = false;
        }

        m_timerUtil.setTimerCallBack(timerCallback);
        m_timerUtil_breakRom.setTimerCallBack(timerCallback_breakRom);

        // 匹配队友倒计时
        m_timerUtil.startTimer(RandomUtil.getRandom(5, m_matchTime), TimerType.TimerType_waitMatchTimeOut);

        
    }

    // 强制解散房间倒计时
    public void startBreakRoomTimer()
    {
        m_timerUtil_breakRom.startTimer(m_roomAliveTime, TimerType.TimerType_breakRoom);
    }

    void timerCallback(object obj)
    {
        try
        {
            switch ((TimerType)obj)
            {
                case TimerType.TimerType_qiangzhu:
                    {
                        if (m_roomState == RoomState.RoomState_qiangzhu)
                        {
                            //LogUtil.getInstance().writeRoomLog(this, "抢主时间结束");
                            m_roomState = RoomState.RoomState_zhuangjiamaidi;

                            if (m_zhuangjiaPlayerData == null)
                            {
                                m_zhuangjiaPlayerData = getPlayerDataList()[0];
                            }

                            GameLogic.doTask_QiangZhuEnd(this);
                            GameLogic.callPlayerMaiDi(m_gameBase, this);
                        }
                    }
                    break;

                case TimerType.TimerType_waitMatchTimeOut:
                    {
                        if (m_roomState == RoomState.RoomState_waiting)
                        {
                            LogUtil.getInstance().writeRoomLog(this, "匹配时间结束，给房间添加机器人");
                            GameLogic.doTask_WaitMatchTimeOut(this);
                        }
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
                            respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_BreakRoom);

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

                            GameLogic.removeRoom(m_gameBase, this,true);
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

    public int getRoomId()
    {
        return m_roomId;
    }

    public RoomState getRoomState()
    {
        return m_roomState;
    }

    public void setRoomState(RoomState roomState)
    {
        m_roomState = roomState;
    }

    public List<PlayerData> getPlayerDataList()
    {
        return m_playerDataList;
    }

    public PlayerData getPlayerDataByUid(string uid)
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
}
