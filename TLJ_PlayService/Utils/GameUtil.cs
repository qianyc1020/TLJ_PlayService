using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TLJ_PlayService;
using TLJCommon;

class GameUtil
{
    static string m_logFlag = "GameUtil";

    // 找出一组牌中某种花色的单牌
    public static List<TLJCommon.PokerInfo> choiceSinglePoker(List<TLJCommon.PokerInfo> myPokerList, TLJCommon.Consts.PokerType pokerType)
    {
        // 先筛选出同花色的牌
        List<TLJCommon.PokerInfo> pokerList = new List<TLJCommon.PokerInfo>();
        for (int i = myPokerList.Count - 1; i >= 0; i--)
        {
            if (myPokerList[i].m_pokerType == pokerType)
            {
                pokerList.Add(myPokerList[i]);
            }
        }

        List<TLJCommon.PokerInfo> singleList = new List<TLJCommon.PokerInfo>();
        List<TLJCommon.PokerInfo> doubleList = new List<TLJCommon.PokerInfo>();

        if (pokerList.Count > 1)
        {
            for (int i = pokerList.Count - 1; i >= 1; i--)
            {
                if (pokerList[i].m_num == pokerList[i - 1].m_num)
                {
                    doubleList.Add(pokerList[i]);
                    --i;

                    if (i == 1)
                    {
                        singleList.Add(pokerList[i - 1]);
                    }
                }
                else
                {
                    singleList.Add(pokerList[i]);

                    if (i == 1)
                    {
                        singleList.Add(pokerList[i - 1]);
                    }
                }
            }
        }
        else if (pokerList.Count == 1)
        {
            singleList.Add(pokerList[0]);
        }

        return singleList;
    }

    // 找出一组牌中某种花色的对子
    public static List<TLJCommon.PokerInfo> choiceDoublePoker(List<TLJCommon.PokerInfo> myPokerList, TLJCommon.Consts.PokerType pokerType)
    {
        // 先筛选出同花色的牌
        List<TLJCommon.PokerInfo> pokerList = new List<TLJCommon.PokerInfo>();
        for (int i = myPokerList.Count - 1; i >= 0; i--)
        {
            if (myPokerList[i].m_pokerType == pokerType)
            {
                pokerList.Add(myPokerList[i]);
            }
        }

        List<TLJCommon.PokerInfo> singleList = new List<TLJCommon.PokerInfo>();
        List<TLJCommon.PokerInfo> doubleList = new List<TLJCommon.PokerInfo>();

        if (pokerList.Count > 1)
        {
            for (int i = pokerList.Count - 1; i >= 1; i--)
            {
                if (pokerList[i].m_num == pokerList[i - 1].m_num)
                {
                    doubleList.Add(pokerList[i]);
                    --i;

                    if (i == 1)
                    {
                        singleList.Add(pokerList[i - 1]);
                    }
                }
                else
                {
                    singleList.Add(pokerList[i]);

                    if (i == 1)
                    {
                        singleList.Add(pokerList[i - 1]);
                    }
                }
            }
        }
        else if (pokerList.Count == 1)
        {
            singleList.Add(pokerList[0]);
        }

        return doubleList;
    }

    public static void checkAllOffLine(RoomData room)
    {
        bool isAllOffLine = true;
        for (int i = 0; i < room.getPlayerDataList().Count; i++)
        {
            if (!room.getPlayerDataList()[i].m_isOffLine)
            {
                isAllOffLine = false;
                break;
            }
        }

        if (isAllOffLine)
        {
            //room.m_tuoguanOutPokerDur = 100;
        }
    }

    public static bool checkRoomNonePlayer(RoomData room)
    {
        bool isRemove = true;
        for (int i = 0; i < room.getPlayerDataList().Count; i++)
        {
            if (!room.getPlayerDataList()[i].m_isOffLine)
            {
                isRemove = false;
                break;
            }
        }

        return isRemove;
    }

    public static int getGameRoomPlayerCount(string gameRoomType)
    {
        int count = 0;
        for (int i = 0; i < PlayLogic_PVP.getInstance().getRoomList().Count; i++)
        {
            RoomData tempRoom = PlayLogic_PVP.getInstance().getRoomList()[i];
            if (tempRoom.m_gameRoomType.CompareTo(gameRoomType) == 0)
            {
                for (int j = 0; j < tempRoom.getPlayerDataList().Count; j++)
                {
                    ++count;
                }
            }
        }

        return count;
    }

    /*
     * canFuShu:是否可以为负数？
     * 休闲场score代表金币，是要扣除的，所以可以为负数
     * PVP场score代表积分，扣到0就不扣了，不能为负数
     */
    public static void setPlayerScore(RoomData room,bool canFuShu)
    {
        try
        {
            float jichufenshu = 1000;
            float changcixishu = 1;
            float defenxishu = 1;
            float xianjiadefen;

            // 计算场次系数
            {
                List<string> tempList = new List<string>();
                CommonUtil.splitStr(room.m_gameRoomType, tempList, '_');

                switch (tempList[0])
                {
                    case "XiuXian":
                        {
                            if (tempList[2].CompareTo("ChuJi") == 0)
                            {
                                changcixishu = 1;
                            }
                            else if (tempList[2].CompareTo("ZhongJi") == 0)
                            {
                                changcixishu = 2;
                            }
                            else if (tempList[2].CompareTo("GaoJi") == 0)
                            {
                                changcixishu = 3;
                            }
                        }
                        break;

                    case "PVP":
                        {
                            changcixishu = 1;
                        }
                        break;
                }
            }

            // 计算得分系数
            {
                if (room.m_getAllScore == 0)
                {
                    defenxishu = -3.8f;
                }
                else if ((room.m_getAllScore >= 5) && (room.m_getAllScore <= 40))
                {
                    defenxishu = -2.8f;
                }
                else if ((room.m_getAllScore >= 45) && (room.m_getAllScore <= 75))
                {
                    defenxishu = -1.8f;
                }
                else if (room.m_getAllScore == 80)
                {
                    defenxishu = 1.0f;
                }
                else if ((room.m_getAllScore >= 85) && (room.m_getAllScore <= 120))
                {
                    defenxishu = 1.8f;
                }
                else if ((room.m_getAllScore >= 125) && (room.m_getAllScore <= 195))
                {
                    defenxishu = 2.8f;
                }
                else if (room.m_getAllScore == 200)
                {
                    defenxishu = 3.8f;
                }
            }

            xianjiadefen = jichufenshu * changcixishu * defenxishu;

            for (int i = 0; i < room.getPlayerDataList().Count; i++)
            {
                if (room.getPlayerDataList()[i].m_isBanker == 1)
                {
                    room.getPlayerDataList()[i].m_score += (-(int)xianjiadefen);
                }
                else
                {
                    room.getPlayerDataList()[i].m_score += (int)xianjiadefen;
                }

                if (!canFuShu)
                {
                    if (room.getPlayerDataList()[i].m_score < 0)
                    {
                        room.getPlayerDataList()[i].m_score = 0;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("GameUtil.setPlayerScore()----" + ex.Message + "gameRoomType:" + room.m_gameRoomType);
        }
    }

    public static void setPVPReward(PVPRoomPlayerList curPVPRoomPlayerList)
    {
        if (curPVPRoomPlayerList.m_gameRoomType.CompareTo(TLJCommon.Consts.GameRoomType_PVP_JinBi_8) == 0)
        {
            curPVPRoomPlayerList.m_playerList[0].m_pvpReward = "1:10000;110:2";
            curPVPRoomPlayerList.m_playerList[1].m_pvpReward = "1:7000;110:1";
        }
        else if(curPVPRoomPlayerList.m_gameRoomType.CompareTo(TLJCommon.Consts.GameRoomType_PVP_JinBi_16) == 0)
        {
            curPVPRoomPlayerList.m_playerList[0].m_pvpReward = "1:20000;110:2";
            curPVPRoomPlayerList.m_playerList[1].m_pvpReward = "1:15000;110:1";
        }
        else if (curPVPRoomPlayerList.m_gameRoomType.CompareTo(TLJCommon.Consts.GameRoomType_PVP_HuaFei_8) == 0)
        {
            curPVPRoomPlayerList.m_playerList[0].m_pvpReward = "111:1;110:1";
            curPVPRoomPlayerList.m_playerList[1].m_pvpReward = "1:1000;110:1";
        }
        else if (curPVPRoomPlayerList.m_gameRoomType.CompareTo(TLJCommon.Consts.GameRoomType_PVP_HuaFei_16) == 0)
        {
            curPVPRoomPlayerList.m_playerList[0].m_pvpReward = "112:1;110:3";
            curPVPRoomPlayerList.m_playerList[1].m_pvpReward = "1:10000;110:2";
        }
    }

    public static bool checkPlayerIsInRoom(string uid)
    {
        bool b = false;

        // 先在休闲场里找
        for (int i = 0; i < PlayLogic_Relax.getInstance().getRoomList().Count;  i++)
        {
            List<PlayerData> playerDataList = PlayLogic_Relax.getInstance().getRoomList()[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                {
                    b = true;
                    break;
                }
            }
        }

        // 然后在比赛场里找
        if (!b)
        {
            for (int i = 0; i < PlayLogic_PVP.getInstance().getRoomList().Count; i++)
            {
                List<PlayerData> playerDataList = PlayLogic_PVP.getInstance().getRoomList()[i].getPlayerDataList();

                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                    {
                        b = true;
                        break;
                    }
                }
            }
        }

        return b;
    }

    public static RoomData getRoomByUid(string uid)
    {
        RoomData room = null;

        // 先在休闲场里找
        for (int i = 0; i < PlayLogic_Relax.getInstance().getRoomList().Count; i++)
        {
            List<PlayerData> playerDataList = PlayLogic_Relax.getInstance().getRoomList()[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                {
                    room = PlayLogic_Relax.getInstance().getRoomList()[i];
                    return room;
                }
            }
        }

        // 然后在比赛场里找
        for (int i = 0; i < PlayLogic_PVP.getInstance().getRoomList().Count; i++)
        {
            List<PlayerData> playerDataList = PlayLogic_PVP.getInstance().getRoomList()[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                {
                    room = PlayLogic_PVP.getInstance().getRoomList()[i];
                    return room;
                }
            }
        }

        return room;
    }
}