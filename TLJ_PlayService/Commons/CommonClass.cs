using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum TimerType
{
    TimerType_qiangzhu,
    TimerType_outPoker,
    TimerType_maidi,
    TimerType_chaodi,
}

public class PVPRoomPlayerList
{
    public string m_gameRoomType = "";
    public List<PlayerData> m_playerList = new List<PlayerData>();
}