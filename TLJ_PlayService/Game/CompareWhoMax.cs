using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJCommon;

class CompareWhoMax
{

    // 比较这一轮出牌的大小
    public static PlayerData compareWhoMax(RoomData room)
    {
        /*
         * listPlayerData是按玩家进入房间顺序排的 
         * 这里要改成按这一轮出牌顺序排
         */
        List<PlayerData> tempList = new List<PlayerData>();

        // 重新排序
        {
            int index = room.getPlayerDataList().IndexOf(room.m_curRoundFirstPlayer);

            for (int i = index; i < room.getPlayerDataList().Count; i++)
            {
                tempList.Add(room.getPlayerDataList()[i]);
            }

            for (int i = 0; i < index; i++)
            {
                tempList.Add(room.getPlayerDataList()[i]);
            }
        }

        for (int i = 0; i < tempList.Count; i++)
        {
            PlayerData playerData = tempList[i];
            List<PokerInfo> outPokerList = playerData.m_curOutPokerList;
            PlayRuleUtil.SetPokerWeight(outPokerList, room.m_levelPokerNum, (Consts.PokerType) room.m_masterPokerType);
        }
        PlayerData maxPlayer1 = CompareBoth(tempList[0], tempList[1], room.m_levelPokerNum, room.m_masterPokerType);
        PlayerData maxPlayer2 = CompareBoth(maxPlayer1, tempList[2], room.m_levelPokerNum, room.m_masterPokerType);
        PlayerData maxPlayer3 = CompareBoth(maxPlayer2, tempList[3], room.m_levelPokerNum, room.m_masterPokerType);
        return maxPlayer3;
    }


    /// <summary>
    /// 两两比较
    /// </summary>
    /// <param name="player1"></param>
    /// <param name="player2"></param>
    /// <returns>牌最大的玩家数据</returns>
    static PlayerData CompareBoth(PlayerData player1, PlayerData player2, int roomMLevelPokerNum, int roomMMasterPokerType)
    {
        List<PokerInfo> playerOutPokerList1 = player1.m_curOutPokerList;
        List<PokerInfo> playerOutPokerList2 = player2.m_curOutPokerList;

        if (playerOutPokerList1 == null || playerOutPokerList2 == null)
        {
            return player1;
        }

        if (playerOutPokerList2.Count == 0 || playerOutPokerList1.Count == 0)
        {
            return player1;
        }

        if (playerOutPokerList1.Count != playerOutPokerList2.Count) return player1;

        LogUtil.getInstance().writeLogToLocalNow("玩家一：" + playerOutPokerList1[0].m_num + " " +
                                                 playerOutPokerList1[0].m_pokerType + " " +
                                                 playerOutPokerList1[0].m_weight
                                                 + "---玩家二" + playerOutPokerList2[0].m_num + " " +
                                                 playerOutPokerList2[0].m_pokerType + " " +
                                                 playerOutPokerList2[0].m_weight);
        CheckOutPoker.OutPokerType outPokerType = CheckOutPoker.checkOutPokerType(playerOutPokerList1, roomMLevelPokerNum, roomMMasterPokerType);
        CheckOutPoker.OutPokerType outPokerType2 = CheckOutPoker.checkOutPokerType(playerOutPokerList2, roomMLevelPokerNum, roomMMasterPokerType);

        //单牌
        if (outPokerType.Equals(CheckOutPoker.OutPokerType.OutPokerType_Single))
        {
            if (PlayRuleUtil.IsMasterPoker(playerOutPokerList1[0], roomMLevelPokerNum, roomMMasterPokerType))
            {
                if (PlayRuleUtil.IsMasterPoker(playerOutPokerList2[0], roomMLevelPokerNum, roomMMasterPokerType))
                {
                    return playerOutPokerList1[0].m_weight >= playerOutPokerList2[0].m_weight ? player1 : player2;
                }
                else
                {
                    return player1;
                }
            }
            else
            {
                //毙了
                if (PlayRuleUtil.IsMasterPoker(playerOutPokerList1[0], roomMLevelPokerNum, roomMMasterPokerType))
                {
                    return player2;
                }
                //双方都是副牌
                else
                {
                    if (playerOutPokerList1[0].m_pokerType != playerOutPokerList2[0].m_pokerType)
                    {
                        return player1;
                    }
                    else
                    {
                        return playerOutPokerList1[0].m_weight >= playerOutPokerList2[0].m_weight ? player1 : player2;
                    }
                }
            }
        }
        //对子比较
        else if (outPokerType.Equals(CheckOutPoker.OutPokerType.OutPokerType_Double))
        {
            if (outPokerType2.Equals(CheckOutPoker.OutPokerType.OutPokerType_Double))
            {
                if (PlayRuleUtil.IsMasterPoker(playerOutPokerList1[0], roomMLevelPokerNum, roomMMasterPokerType))
                {
                    if (PlayRuleUtil.IsMasterPoker(playerOutPokerList1[0], roomMLevelPokerNum, roomMMasterPokerType))
                    {
                        return playerOutPokerList1[0].m_weight >= playerOutPokerList2[0].m_weight ? player1 : player2;
                    }
                    else
                    {
                        return player1;
                    }
                }
                else
                {
                    //毙了
                    if (PlayRuleUtil.IsMasterPoker(playerOutPokerList1[0], roomMLevelPokerNum, roomMMasterPokerType))
                    {
                        return player2;
                    }
                    else
                    {
                        if (playerOutPokerList1[0].m_pokerType != playerOutPokerList2[0].m_pokerType)
                        {
                            return player1;
                        }
                        else
                        {
                            return playerOutPokerList1[0].m_weight >= playerOutPokerList2[0].m_weight
                                ? player1
                                : player2;
                        }
                    }
                }
            }
            else
            {
                return player1;
            }
        }
        //拖拉机
        else if (outPokerType.Equals(CheckOutPoker.OutPokerType.OutPokerType_TuoLaJi))
        {
            if (outPokerType2.Equals(CheckOutPoker.OutPokerType.OutPokerType_TuoLaJi))
            {
                if (PlayRuleUtil.IsMasterPoker(playerOutPokerList1[0], roomMLevelPokerNum, roomMMasterPokerType))
                {
                    if (PlayRuleUtil.IsMasterPoker(playerOutPokerList1[0], roomMLevelPokerNum, roomMMasterPokerType))
                    {
                        return playerOutPokerList1[0].m_weight >= playerOutPokerList2[0].m_weight ? player1 : player2;
                    }
                    else
                    {
                        return player1;
                    }
                }
                //玩家1不是主牌
                else
                {
                    //毙了
                    if (PlayRuleUtil.IsMasterPoker(playerOutPokerList1[0], roomMLevelPokerNum, roomMMasterPokerType))
                    {
                        return player2;
                    }
                    else
                    {
                        if (playerOutPokerList1[0].m_pokerType != playerOutPokerList2[0].m_pokerType)
                        {
                            return player1;
                        }
                        else
                        {
                            return playerOutPokerList1[0].m_weight >= playerOutPokerList2[0].m_weight ? player1 : player2;
                        }
                    }
                }
            }
        }
        return player1;
    }
}