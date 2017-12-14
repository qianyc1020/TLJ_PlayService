using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static TLJCommon.Consts;

public class RoomData
{
    public enum TimerType
    {
        TimerType_qiangzhu,
    }

    public int m_roomId;
    public int m_wanfaType;
    public string m_gameRoomType;
    public int m_rounds_pvp = 1;
    public int m_outPokerDur = 15000;       // 出牌时间：毫秒
    public int m_tuoguanOutPokerDur = 2000; // 托管出牌时间:毫秒
    //public int m_tuoguanOutPokerDur = 500; // 托管出牌时间:毫秒
    public int m_qiangzhuTime = 10000;      // 抢主时间：毫秒
    public int m_maidiTime = 40000;         // 埋底时间：毫秒
    public int m_chaodiTime = 10000;        // 炒底时间：毫秒

    public string m_tag;

    public bool m_isStartGame = false;
    public bool m_canChaoDi = true;
    public RoomState m_roomState = RoomState.RoomState_waiting;

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

    public RoomData(GameBase gameBase, int roomId, string gameRoomType)
    {
        m_gameBase = gameBase;
        m_roomId = roomId;
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
    }

    void timerCallback(object obj)
    {
        switch ((TimerType)obj)
        {
            case TimerType.TimerType_qiangzhu:
                {
                    if (m_roomState == RoomState.RoomState_qiangzhu)
                    {
                        LogUtil.getInstance().addDebugLog("抢主时间结束");
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
        }
    }

    public int getRoomId()
    {
        return m_roomId;
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
