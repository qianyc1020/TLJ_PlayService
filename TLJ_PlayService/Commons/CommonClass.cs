using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class PVPRoomPlayerList
{
    public string m_gameRoomType = "";
    public List<PlayerData> m_playerList = new List<PlayerData>();
}

public class RoomData
{
    public enum RoomState
    {
        RoomState_waiting,
        RoomState_qiangzhu,
        RoomState_zhuangjiamaidi,
        RoomState_fanzhu,
        RoomState_chaodi,
        RoomState_othermaidi,
        RoomState_koudi,
        RoomState_gaming,
        RoomState_end,
    }

    public int m_roomId;
    public string m_gameRoomType;
    public int m_rounds_pvp = 1;

    public bool m_isStartGame = false;
    public bool m_canChaoDi = true;
    public RoomState m_roomState = RoomState.RoomState_waiting;

    public PlayerData m_curOutPokerPlayer = null;
    public PlayerData m_curMaiDiPlayer = null;
    public PlayerData m_curChaoDiPlayer = null;
    public PlayerData m_curRoundFirstPlayer = null;

    // 本房间玩家信息
    List<PlayerData> m_playerDataList = new List<PlayerData>();

    // 底牌
    List<TLJCommon.PokerInfo> m_DiPokerList = new List<TLJCommon.PokerInfo>();

    // 默认为-1，代表没有被赋值过
    public int m_levelPokerNum = -1; // 级牌

    // 上一次玩家抢主的牌
    public List<TLJCommon.PokerInfo> m_qiangzhuPokerList = new List<TLJCommon.PokerInfo>();

    public int m_masterPokerType = -1; // 主牌花色

    public int m_getAllScore = 0; // 庄家对家抓到的分数

    public PlayerData m_zhuangjiaPlayerData = null;

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

    public RoomData(int roomId,string gameRoomType)
    {
        m_roomId = roomId;
        m_gameRoomType = gameRoomType;

        // 经典场不可抄底
        if ((m_gameRoomType.CompareTo(TLJCommon.Consts.GameRoomType_XiuXian_JingDian_ChuJi) == 0) ||
            (m_gameRoomType.CompareTo(TLJCommon.Consts.GameRoomType_XiuXian_JingDian_ZhongJi) == 0) ||
            (m_gameRoomType.CompareTo(TLJCommon.Consts.GameRoomType_XiuXian_JingDian_GaoJi) == 0))
        {
            m_canChaoDi = false;
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
}

public class PlayerData
{
    public IntPtr m_connId;
    public string m_uid;
    public string m_teammateUID;    // 队友uid
    public int m_isBanker = 0;      // 是否是庄家
    public int m_myLevelPoker = 2;
    public int m_score = 0;
    public int m_rank = 0;
    public bool m_isOffLine = false;
    public bool m_isContinueGame = false;
    public bool m_isAI = false;

    List<TLJCommon.PokerInfo> m_pokerList = new List<TLJCommon.PokerInfo>();
    public List<TLJCommon.PokerInfo> m_curOutPokerList = new List<TLJCommon.PokerInfo>();

    public PlayerData(IntPtr connId, string uid, bool isAI)
    {
        m_connId = connId;
        m_uid = uid;
        m_isAI = isAI;
    }

    public void setPokerList(List<TLJCommon.PokerInfo> pokerList)
    {
        m_pokerList = pokerList;
    }

    public List<TLJCommon.PokerInfo> getPokerList()
    {
        return m_pokerList;
    }

    public void clearData()
    {
        m_isBanker = 0;
        m_isOffLine = false;
        m_isContinueGame = false;
    }
}