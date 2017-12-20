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
    public int m_gold = 0;
    public int m_rank = 0;
    public int m_vipLevel = 0;
    bool m_isOffLine = false;
    bool m_isTuoGuan = false;
    public bool m_isContinueGame = false;
    public bool m_isAI = false;
    public bool m_isPVP = false;
    public bool m_isUseJiPaiQi = false;
    public string m_pvpReward = "";
    public string m_gameRoomType;

    List<TLJCommon.PokerInfo> m_pokerList = new List<TLJCommon.PokerInfo>();
    public List<TLJCommon.PokerInfo> m_allotPokerList = new List<TLJCommon.PokerInfo>();
    public List<TLJCommon.PokerInfo> m_curOutPokerList = new List<TLJCommon.PokerInfo>();

    public List<BuffData> m_buffData = new List<BuffData>();

    public TimerUtil m_timerUtil = new TimerUtil();

    public PlayerData(IntPtr connId, string uid, bool isAI,string gameRoomType)
    {
        m_connId = connId;
        m_uid = uid;
        m_isAI = isAI;
        m_gameRoomType = gameRoomType;

        // 场次
        {
            List<string> list = new List<string>();
            CommonUtil.splitStr(m_gameRoomType, list, '_');
            if (list[0].CompareTo("PVP") == 0)
            {
                m_score = 1000;
                m_isPVP = true;
            }
            if (list[0].CompareTo("XiuXian") == 0)
            {
            }
        }

        if (m_isAI)
        {
            m_isOffLine = true;
            m_isTuoGuan = true;
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

                    if (!m_isTuoGuan)
                    {
                        m_isTuoGuan = true;
                        changeTuoGuanState();
                    }
                    TrusteeshipLogic.trusteeshipLogic_OutPoker(room.m_gameBase, room,this);
                }
                break;

            case TimerType.TimerType_maidi:
                {
                    RoomData room = GameUtil.getRoomByUid(m_uid);

                    if (!m_isTuoGuan)
                    {
                        m_isTuoGuan = true;
                        changeTuoGuanState();
                    }
                    TrusteeshipLogic.trusteeshipLogic_MaiDi(room.m_gameBase, room, this);
                }
                break;

            case TimerType.TimerType_chaodi:
                {
                    RoomData room = GameUtil.getRoomByUid(m_uid);

                    if (!m_isTuoGuan)
                    {
                        m_isTuoGuan = true;
                        changeTuoGuanState();
                    }
                    TrusteeshipLogic.trusteeshipLogic_ChaoDi(room.m_gameBase, room, this);
                }
                break;
        }
    }

    public void setPokerList(List<TLJCommon.PokerInfo> pokerList)
    {
        if (pokerList.Count != 25)
        {
            LogUtil.getInstance().addDebugLog("PlayerData.setPokerList----给" + m_uid + "发牌的张数不对：" + pokerList.Count);
        }

        m_pokerList = pokerList;
    }

    public List<TLJCommon.PokerInfo> getPokerList()
    {
        return m_pokerList;
    }

    public void clearData()
    {
        m_isBanker = 0;
        //m_isOffLine = false;
        m_isTuoGuan = false;
        m_isContinueGame = false;
        m_isUseJiPaiQi = false;

        m_pokerList.Clear();
        m_allotPokerList.Clear();
        m_curOutPokerList.Clear();
        m_buffData.Clear();
    }

    public bool isOffLine()
    {
        return m_isOffLine;
    }

    public void setIsOffLine(bool isOffLine)
    {
        m_isOffLine = isOffLine;
        m_isTuoGuan = isOffLine;
    }

    public bool isTuoGuan()
    {
        return m_isTuoGuan;
    }

    public void setIsTuoGuan(bool isTuoGuan)
    {
        m_isTuoGuan = isTuoGuan;
    }

    public GameBase getGameBase()
    {
        RoomData room = GameUtil.getRoomByUid(m_uid);

        if (room != null)
        {
            return room.m_gameBase;
        }

        return null;
    }

    public void changeTuoGuanState()
    {
        GameBase gameBase = getGameBase();
        if (gameBase != null)
        {
            GameLogic.tellPlayerTuoGuanState(gameBase, this, m_isTuoGuan);
        }
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