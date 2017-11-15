using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    public string m_pvpReward = "";

    List<TLJCommon.PokerInfo> m_pokerList = new List<TLJCommon.PokerInfo>();
    public List<TLJCommon.PokerInfo> m_allotPokerList = new List<TLJCommon.PokerInfo>();
    public List<TLJCommon.PokerInfo> m_curOutPokerList = new List<TLJCommon.PokerInfo>();

    public List<BuffData> m_buffData = new List<BuffData>();

    public TimerUtil m_timerUtil = new TimerUtil();

    public PlayerData(IntPtr connId, string uid, bool isAI)
    {
        m_connId = connId;
        m_uid = uid;
        m_isAI = isAI;

        if (m_isAI)
        {
            m_isOffLine = true;
        }

        m_timerUtil.setTimerCallBack(timerCallback);
    }

    void timerCallback(object obj)
    {
        switch ((TimerType)obj)
        {
            case TimerType.TimerType_outPoker:
                {
                    RoomData room = GameUtil.getRoomByUid(m_uid);
                    TrusteeshipLogic.trusteeshipLogic_OutPoker(room.m_gameBase, room,this);
                }
                break;

            case TimerType.TimerType_maidi:
                {
                    LogUtil.getInstance().addDebugLog("埋底时间结束");
                    RoomData room = GameUtil.getRoomByUid(m_uid);
                    TrusteeshipLogic.trusteeshipLogic_MaiDi(room.m_gameBase, room, this);
                }
                break;

            case TimerType.TimerType_chaodi:
                {
                    LogUtil.getInstance().addDebugLog("炒底时间结束");
                    RoomData room = GameUtil.getRoomByUid(m_uid);
                    TrusteeshipLogic.trusteeshipLogic_ChaoDi(room.m_gameBase, room, this);
                }
                break;
        }
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

public class BuffData
{
    public int m_prop_id;
    public int m_buff_num;

    public BuffData(int prop_id,int buff_num)
    {
        m_prop_id = prop_id;
        m_buff_num = buff_num;
    }
}