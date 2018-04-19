using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class DDZ_PlayerData
{
    public IntPtr m_connId;
    public string m_uid;
    public int m_isDiZhu = 0;       // 是否是地主
    public int m_jiaofen = 0;
    public int m_isJiaBang = -1;    // -1代表玩家还没选择是否加棒
    public int m_gold = 0;
    bool m_isOffLine = false;
    bool m_isTuoGuan = false;
    public bool m_isAI = false;
    public string m_gameRoomType;
    public bool m_isFreeOutPoker = false;
    public int m_score = 0;
    public int m_outPokerCiShu = 0;

    public List<TLJCommon.PokerInfo> m_pokerList = new List<TLJCommon.PokerInfo>();             // 玩家手牌（发完牌之后）
    public List<TLJCommon.PokerInfo> m_allotPokerList = new List<TLJCommon.PokerInfo>();        // 当前已经发的牌
    public List<TLJCommon.PokerInfo> m_curOutPokerList = new List<TLJCommon.PokerInfo>();

    public TimerUtil m_timerUtil = new TimerUtil();
    public TimerUtil m_timerUtilOffLine = new TimerUtil();

    public DDZ_PlayerData(IntPtr connId, string uid, bool isAI,string gameRoomType)
    {
        m_connId = connId;
        m_uid = uid;
        m_isAI = isAI;
        m_gameRoomType = gameRoomType;

        if (m_isAI)
        {
            m_isOffLine = true;
            m_isTuoGuan = true;
        }

        m_timerUtil.setTimerCallBack(timerCallback);
        m_timerUtilOffLine.setTimerCallBack(timerCallback_offLine);
    }

    void timerCallback(object obj)
    {
        try
        {
            switch ((TimerType)obj)
            {
                case TimerType.TimerType_qiangDizhu:
                    {
                        DDZ_RoomData room = DDZ_GameUtil.getRoomByUid(m_uid);
                        if (room != null)
                        {
                            DDZ_TrusteeshipLogic.trusteeshipLogic_QiangDiZhu(room.m_gameBase, room, this);
                        }
                    }
                    break;

                case TimerType.TimerType_jiaBang:
                    {
                        DDZ_RoomData room = DDZ_GameUtil.getRoomByUid(m_uid);
                        if (room != null)
                        {
                            DDZ_TrusteeshipLogic.trusteeshipLogic_JiaBang(room.m_gameBase, room, this);
                        }
                    }
                    break;

                case TimerType.TimerType_outPoker:
                    {
                        DDZ_RoomData room = DDZ_GameUtil.getRoomByUid(m_uid);

                        if (room.m_curOutPokerPlayer.m_uid.CompareTo(m_uid) != 0)
                        {
                            string str = "DDZ_PlayerData.timerCallback case TimerType.TimerType_outPoker错误：当前出牌人应该是：" + room.m_curOutPokerPlayer.m_uid + ",但是现在出牌倒计时结束的人是：" + m_uid;
                            LogUtil.getInstance().writeRoomLog(room, str);
                            return;
                        }

                        if (!m_isTuoGuan)
                        {
                            m_isTuoGuan = true;
                            changeTuoGuanState();
                        }
                        DDZ_TrusteeshipLogic.trusteeshipLogic_OutPoker(room.m_gameBase, room, this);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error("DDZ_PlayerData----" + "timerCallback: " + ex);
        }
    }

    void timerCallback_offLine(object obj)
    {
        try
        {
            switch ((TimerType)obj)
            {
                case TimerType.TimerType_qiangDizhu:
                    {
                        DDZ_RoomData room = DDZ_GameUtil.getRoomByUid(m_uid);
                        if (room != null)
                        {
                            DDZ_TrusteeshipLogic.trusteeshipLogic_QiangDiZhu(room.m_gameBase, room, this);
                        }
                    }
                    break;

                case TimerType.TimerType_jiaBang:
                    {
                        DDZ_RoomData room = DDZ_GameUtil.getRoomByUid(m_uid);
                        if (room != null)
                        {
                            DDZ_TrusteeshipLogic.trusteeshipLogic_JiaBang(room.m_gameBase, room, this);
                        }
                    }
                    break;

                case TimerType.TimerType_outPoker:
                    {
                        DDZ_RoomData room = DDZ_GameUtil.getRoomByUid(m_uid);

                        if (room.m_curOutPokerPlayer.m_uid.CompareTo(m_uid) != 0)
                        {
                            string str = "DDZ_PlayerData.timerCallback case TimerType.TimerType_outPoker错误：当前出牌人应该是：" + room.m_curOutPokerPlayer.m_uid + ",但是现在出牌倒计时结束的人是：" + m_uid;
                            LogUtil.getInstance().writeRoomLog(room, str);
                            return;
                        }

                        if (!m_isTuoGuan)
                        {
                            m_isTuoGuan = true;
                            changeTuoGuanState();
                        }
                        DDZ_TrusteeshipLogic.trusteeshipLogic_OutPoker(room.m_gameBase, room, this);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error("DDZ_PlayerData----" + "timerCallback: " + ex);
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
        m_isTuoGuan = false;

        m_pokerList.Clear();
        m_allotPokerList.Clear();
        m_curOutPokerList.Clear();
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

    public DDZ_GameBase getGameBase()
    {
        DDZ_RoomData room = DDZ_GameUtil.getRoomByUid(m_uid);

        if (room != null)
        {
            return room.m_gameBase;
        }

        return null;
    }

    public void changeTuoGuanState()
    {
        DDZ_GameBase gameBase = getGameBase();
        if (gameBase != null)
        {
            DDZ_GameLogic.tellPlayerTuoGuanState(gameBase, this, m_isTuoGuan);
        }
    }
}