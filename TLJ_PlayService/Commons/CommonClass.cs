using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum TimerType
{
    TimerType_waitMatchTimeOut,
    TimerType_qiangzhu,
    TimerType_outPoker,
    TimerType_maidi,
    TimerType_chaodi,
    TimerType_breakRoom,
    TimerType_fapai,
    TimerType_pvpNextStartGame,
    TimerType_callPlayerOutPoker,
    TimerType_gameOver,

    TimerType_qiangDizhu,
    TimerType_jiaBang,
}

public class PVPRoomPlayerList
{
    public string m_gameRoomType = "";
    public List<PlayerData> m_playerList = new List<PlayerData>();

    public void addPlayer(PlayerData playerData)
    {
        // 先在已有的里面找
        for (int i = 0; i < m_playerList.Count; i++)
        {
            if (m_playerList[i].m_uid.CompareTo(playerData.m_uid) == 0)
            {
                m_playerList[i] = playerData;

                return;
            }
        }

        // 已有的没有的话则新加入
        m_playerList.Add(playerData);
    }
}