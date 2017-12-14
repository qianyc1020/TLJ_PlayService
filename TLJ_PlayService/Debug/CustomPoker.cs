using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class CustomPoker
{
    static List<PlayerCustomPokerInfo> s_playerCustomPokerInfoList = new List<PlayerCustomPokerInfo>();

    public static void setPlayerCustomPokerInfo(string uid, List<TLJCommon.PokerInfo> pokerLst)
    {
        PlayerCustomPokerInfo playerCustomPokerInfo = findPlayerByUid(uid);
        if (playerCustomPokerInfo != null)
        {
            playerCustomPokerInfo.setPokerList(pokerLst);
        }
        else
        {
            s_playerCustomPokerInfoList.Add(new PlayerCustomPokerInfo(uid, pokerLst));
        }
    }

    public static PlayerCustomPokerInfo findPlayerByUid(string uid)
    {
        for (int i = 0; i < s_playerCustomPokerInfoList.Count; i++)
        {
            if (s_playerCustomPokerInfoList[i].m_uid.CompareTo(uid) == 0)
            {
                return s_playerCustomPokerInfoList[i];
            }
        }

        return null;
    }
}

public class PlayerCustomPokerInfo
{
    public string m_uid = "";
    List<TLJCommon.PokerInfo> m_pokerList = new List<TLJCommon.PokerInfo>();

    public PlayerCustomPokerInfo(string uid, List<TLJCommon.PokerInfo> pokerLst)
    {
        m_uid = uid;
        m_pokerList = pokerLst;
    }

    public List<TLJCommon.PokerInfo> getPokerList()
    {
        return m_pokerList;
    }

    public void setPokerList(List<TLJCommon.PokerInfo> pokerList)
    {
        m_pokerList = pokerList;
    }

    public List<TLJCommon.PokerInfo> getPokerListForNew()
    {
        List<TLJCommon.PokerInfo> pokerList = new List<TLJCommon.PokerInfo>();

        for (int i = 0; i < m_pokerList.Count; i++)
        {
            pokerList.Add(m_pokerList[i]);
        }

        return pokerList;
    }
}
