using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class PVPChangCiUtil
{
    public List<PVPRoomPlayerList> m_allPVPChangCiList= new List<PVPRoomPlayerList>();

    static PVPChangCiUtil s_instance = null;

    public static PVPChangCiUtil getInstance()
    {
        if (s_instance == null)
        {
            s_instance = new PVPChangCiUtil();
        }

        return s_instance;
    }

    public List<PVPRoomPlayerList> getAllPVPChangCiList()
    {
        return m_allPVPChangCiList;
    }

    public void sortPVPRoomPlayerList(PVPRoomPlayerList pvpRoomPlayerList)
    {
        for (int i = 0; i < pvpRoomPlayerList.m_playerList.Count; i++)
        {
            LogUtil.getInstance().addDebugLog("排序前sortPVPRoomPlayerList index=" + i + "  uid = " + pvpRoomPlayerList.m_playerList[i].m_uid + "  score = " + pvpRoomPlayerList.m_playerList[i].m_score);
        }

        for (int i = 0; i < pvpRoomPlayerList.m_playerList.Count - 1; i++)
        {
            for (int j = (i + 1); j < pvpRoomPlayerList.m_playerList.Count; j++)
            {
                if (pvpRoomPlayerList.m_playerList[j].m_score > pvpRoomPlayerList.m_playerList[i].m_score)
                {
                    PlayerData temp = pvpRoomPlayerList.m_playerList[j];
                    pvpRoomPlayerList.m_playerList[j] = pvpRoomPlayerList.m_playerList[i];
                    pvpRoomPlayerList.m_playerList[i] = temp;
                }
            }
        }

        for (int i = 0; i < pvpRoomPlayerList.m_playerList.Count; i++)
        {
            LogUtil.getInstance().addDebugLog("排序后sortPVPRoomPlayerList index=" + i + "  uid = " + pvpRoomPlayerList.m_playerList[i].m_uid + "  score = " + pvpRoomPlayerList.m_playerList[i].m_score);
        }

        for (int i = 0; i < pvpRoomPlayerList.m_playerList.Count; i++)
        {
            pvpRoomPlayerList.m_playerList[i].m_rank = i + 1;
        }
    }

    public void deletePVPRoomPlayerList(PVPRoomPlayerList pvpRoomPlayerList)
    {
        m_allPVPChangCiList.Remove(pvpRoomPlayerList);
    }

    public PVPRoomPlayerList getPVPRoomPlayerListByUid(string uid)
    {
        //LogUtil.getInstance().addDebugLog("PVPChangCiUtil---m_allPVPChangCiList.count = "+ m_allPVPChangCiList.Count);

        for (int i = 0; i < m_allPVPChangCiList.Count; i++)
        {
            for (int j = 0; j < m_allPVPChangCiList[i].m_playerList.Count; j++)
            {
                //LogUtil.getInstance().addDebugLog("PVPChangCiUtil---PVPRoomPlayerList index" + j);
                if (m_allPVPChangCiList[i].m_playerList[j].m_uid.CompareTo(uid) == 0)
                {
                    return m_allPVPChangCiList[i];
                }
            }
        }

        return null;
    }

    public bool addPlayerToThere(RoomData room)
    {
        lock (m_allPVPChangCiList)
        {
            LogUtil.getInstance().addDebugLog("PVPChangCiUtil---addPlayerToThere--room id = :" + room.getRoomId());

            // 在已有的PVPRoomPlayerList里加入
            for (int i = 0; i < room.getPlayerDataList().Count; i++)
            {
                PVPRoomPlayerList temp_PVPRoomPlayerList = getPVPRoomPlayerListByUid(room.getPlayerDataList()[i].m_uid);

                if (temp_PVPRoomPlayerList != null)
                {
                    for (int j = 0; j < room.getPlayerDataList().Count; j++)
                    {
                        temp_PVPRoomPlayerList.addPlayer(room.getPlayerDataList()[j]);

                        LogUtil.getInstance().addDebugLog("PVPChangCiUtil---在已有的PVPRoomPlayerList里加入,PVPRoomPlayerList index = " + m_allPVPChangCiList.IndexOf(temp_PVPRoomPlayerList) + "  uid = " + room.getPlayerDataList()[j].m_uid);
                    }

                    return true;
                }
            }

            // 创建新的PVPRoomPlayerList加入
            {
                PVPRoomPlayerList temp_PVPRoomPlayerList = new PVPRoomPlayerList();
                temp_PVPRoomPlayerList.m_gameRoomType = room.m_gameRoomType;
                m_allPVPChangCiList.Add(temp_PVPRoomPlayerList);

                for (int j = 0; j < room.getPlayerDataList().Count; j++)
                {
                    temp_PVPRoomPlayerList.addPlayer(room.getPlayerDataList()[j]);

                    LogUtil.getInstance().addDebugLog("PVPChangCiUtil---创建新的PVPRoomPlayerList以供玩家加入,PVPRoomPlayerList index = " + m_allPVPChangCiList.IndexOf(temp_PVPRoomPlayerList) + "  uid = " + room.getPlayerDataList()[j].m_uid);
                }
            }
        }

        return true;
    }

    //public bool addPlayerToThere(string gameRoomType, PlayerData playerData)
    //{
    //    lock (m_allPVPChangCiList)
    //    {
    //        LogUtil.getInstance().addDebugLog("PVPChangCiUtil---addPlayerToThere:" + playerData.m_uid);

    //        // 检查是否已经加入过,已经加入过则替换旧的
    //        for (int i = 0; i < m_allPVPChangCiList.Count; i++)
    //        {
    //            for (int j = 0; j < m_allPVPChangCiList[i].m_playerList.Count; j++)
    //            {
    //                if (m_allPVPChangCiList[i].m_playerList[j].m_uid.CompareTo(playerData.m_uid) == 0)
    //                {
    //                    m_allPVPChangCiList[i].m_playerList[j] = playerData;
    //                    LogUtil.getInstance().addDebugLog("PVPChangCiUtil---以替换的方式加入玩家：" + playerData.m_uid);

    //                    return true;
    //                }
    //            }
    //        }

    //        // 检查是否有房间可以加入
    //        for (int i = 0; i < m_allPVPChangCiList.Count; i++)
    //        {
    //            if (gameRoomType.CompareTo(m_allPVPChangCiList[i].m_gameRoomType) == 0)
    //            {
    //                // 检查是否可以加入
    //                {
    //                    List<string> tempList = new List<string>();
    //                    CommonUtil.splitStr(gameRoomType, tempList, '_');

    //                    if (m_allPVPChangCiList[i].m_playerList.Count < int.Parse(tempList[2]))
    //                    {
    //                        // 可以加入
    //                        {
    //                            m_allPVPChangCiList[i].m_playerList.Add(playerData);

    //                            LogUtil.getInstance().addDebugLog("PVPChangCiUtil---加入玩家成功,gameRoomType = " + gameRoomType);

    //                            return true;
    //                        }
    //                    }
    //                }
    //            }
    //        }

    //        // 如果没有地方可加入，就创建一个新的
    //        {
    //            PVPRoomPlayerList pvpRoomPlayerList = new PVPRoomPlayerList();
    //            pvpRoomPlayerList.m_gameRoomType = gameRoomType;
    //            pvpRoomPlayerList.m_playerList.Add(playerData);

    //            m_allPVPChangCiList.Add(pvpRoomPlayerList);

    //            LogUtil.getInstance().addDebugLog("PVPChangCiUtil---创建新的PVPRoomPlayerList以供玩家加入,gameRoomType = " + gameRoomType);
    //        }
    //    }

    //    return true;
    //}
}