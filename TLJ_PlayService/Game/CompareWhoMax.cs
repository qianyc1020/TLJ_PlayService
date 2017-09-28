using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJCommon;

class CompareWhoMax
{
    //当前主牌花色
    static int mMasterPokerType;

    //级牌
    static int mLevelPokerNum;


    // 比较这一轮出牌的大小
    public static PlayerData compareWhoMax(RoomData room)
    {
        /*
         * listPlayerData是按玩家进入房间顺序排的 
         * 这里要改成按这一轮出牌顺序排
         */

        //当前主牌花色
        mMasterPokerType = room.m_masterPokerType;
        //级牌
        mLevelPokerNum = room.m_levelPokerNum;

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
        return CompareBoth(CompareBoth(CompareBoth(tempList[0], tempList[1]), tempList[2]), tempList[3]);
    }


    /// <summary>
    /// 两两比较
    /// </summary>
    /// <param name="player1"></param>
    /// <param name="player2"></param>
    /// <returns>牌最大的玩家数据</returns>
    static PlayerData CompareBoth(PlayerData player1, PlayerData player2)
    {
        List<PokerInfo> playerOutPokerList1 =
            SetPokerWeight(player1.m_curOutPokerList, mLevelPokerNum, (Consts.PokerType) mMasterPokerType);
        List<PokerInfo> playerOutPokerList2 =
            SetPokerWeight(player2.m_curOutPokerList, mLevelPokerNum, (Consts.PokerType) mMasterPokerType);

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
        CheckOutPoker.OutPokerType outPokerType = CheckOutPoker.checkOutPokerType(playerOutPokerList1);
        CheckOutPoker.OutPokerType outPokerType2 = CheckOutPoker.checkOutPokerType(playerOutPokerList2);

        //单牌
        if (outPokerType.Equals(CheckOutPoker.OutPokerType.OutPokerType_Single))
        {
            if (IsMasterPoker(playerOutPokerList1[0]))
            {
                if (IsMasterPoker(playerOutPokerList2[0]))
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
                if (IsMasterPoker(playerOutPokerList2[0]))
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
                if (IsMasterPoker(playerOutPokerList1[0]))
                {
                    if (IsMasterPoker(playerOutPokerList2[0]))
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
                    if (IsMasterPoker(playerOutPokerList2[0]))
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
                if (IsMasterPoker(playerOutPokerList1[0]))
                {
                    if (IsMasterPoker(playerOutPokerList2[0]))
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
                    if (IsMasterPoker(playerOutPokerList2[0]))
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


    //检查是否是拖拉机
    public static bool CheckTuoLaJi(List<PokerInfo> playerOutPokerList)
    {
        if (playerOutPokerList.Count % 2 == 0 && playerOutPokerList.Count >= 4)
        {
            //都是主牌或者都是同一花色的副牌
            if (IsAllMasterPoker(playerOutPokerList) || IsAllFuPoker(playerOutPokerList))
            {
                //先判断是否为对子
                for (int i = 0; i < playerOutPokerList.Count; i += 2)
                {
                    if (playerOutPokerList[i].m_num != playerOutPokerList[i + 1].m_num
                        || playerOutPokerList[i].m_pokerType != playerOutPokerList[i + 1].m_pokerType)
                    {
                        return false;
                    }
                }
                //判断权重
                for (int i = 0; i < playerOutPokerList.Count - 2; i += 2)
                {
                    if (playerOutPokerList[i + 2].m_weight - playerOutPokerList[i].m_weight != 1)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        return false;
    }

    //单牌是否为主牌
    static bool IsMasterPoker(PokerInfo pokerInfo)
    {
        if (pokerInfo.m_num == mLevelPokerNum || (int) pokerInfo.m_pokerType == mMasterPokerType
            || pokerInfo.m_pokerType == Consts.PokerType.PokerType_Wang)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //是否都是主牌
    public static bool IsAllMasterPoker(List<PokerInfo> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (!IsMasterPoker(list[i]))
            {
                return false;
            }
        }
        return true;
    }

    //是否都是同一花色
    public static bool IsAllFuPoker(List<PokerInfo> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            if (list[i].m_pokerType != list[i + 1].m_pokerType)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///  给weight重新赋值，从2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17
    ///  17为大王，16为小王，15为主级牌,14为副级牌
    ///  无主情况下,16为大王,15为小王，14为级牌
    /// </summary>
    /// <param name="list">牌</param>
    /// <param name="levelPokerNum">级牌</param>
    /// <param name="masterPokerType">主牌花色</param>
    /// <returns></returns>
    public static List<PokerInfo> SetPokerWeight(List<PokerInfo> list, int levelPokerNum,
        Consts.PokerType masterPokerType)
    {
        for (int i = 0; i < list.Count; i++)
        {
            PokerInfo pokerInfo = list[i];
            //是级牌
            if (pokerInfo.m_num == levelPokerNum)
            {
                if (pokerInfo.m_pokerType == masterPokerType)
                {
                    pokerInfo.m_weight = 15;
                }
                else
                {
                    pokerInfo.m_weight = 14;
                }
            }
            //大王
            else if (pokerInfo.m_num == 16)
            {
                pokerInfo.m_weight = (int)masterPokerType != (-1) ? 17 : 16;
            }
            //小王
            else if (pokerInfo.m_num == 15)
            {
                pokerInfo.m_weight = (int)masterPokerType != (-1) ? 16 : 15;
            }
            else if (pokerInfo.m_num < levelPokerNum)
            {
                pokerInfo.m_weight = pokerInfo.m_num;
            }
            else
            {
                pokerInfo.m_weight = pokerInfo.m_num - 1;
            }
        }
        return list;
    }

    public static List<PokerInfo> GetTljFromPoker(List<PokerInfo> list, List<PokerInfo> firstOutPoker)
    {




        return null;
    }
}