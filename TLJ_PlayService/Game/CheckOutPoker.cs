using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJCommon;

public class CheckOutPoker
{
    public enum OutPokerType
    {
        OutPokerType_Error,
        OutPokerType_Single,
        OutPokerType_Double,
        OutPokerType_ShuaiPai,
        OutPokerType_TuoLaJi,
    }

    // 检测出牌合理性
    public static bool checkOutPoker(bool isFreeOutPoker, List<TLJCommon.PokerInfo> myOutPokerList,
        List<TLJCommon.PokerInfo> beforeOutPokerList, List<TLJCommon.PokerInfo> myRestPokerList,
        int mLevelPokerNum, int masterPokerType)
    {
        // 自由出牌
        if (isFreeOutPoker)
        {
            //判断是否是主牌
            if (PlayRuleUtil.IsAllMasterPoker(myOutPokerList,mLevelPokerNum, masterPokerType))
            {
                return true;
            }
            //判断是否为同花色副牌
            else if (PlayRuleUtil.IsAllFuPoker(myOutPokerList))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        // 跟牌
        else
        {
            // 检查张数是否一致
            {
                if (myOutPokerList.Count != beforeOutPokerList.Count)
                {
                    return false;
                }
            }

            switch (CheckOutPoker.checkOutPokerType(beforeOutPokerList, mLevelPokerNum, masterPokerType))
            {
                case CheckOutPoker.OutPokerType.OutPokerType_Single:
                {
                    //第一个人出的是主牌
                    if (PlayRuleUtil.IsMasterPoker(beforeOutPokerList[0], mLevelPokerNum, masterPokerType))
                    {
                        if (PlayRuleUtil.IsContainMasterPoker(myRestPokerList, mLevelPokerNum, masterPokerType))
                        {
                            if (PlayRuleUtil.IsMasterPoker(myOutPokerList[0], mLevelPokerNum, masterPokerType))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                    //出的是副牌
                    else
                    {
                        //如果有该花色必须出该花色
                        List<PokerInfo> typeInfo;
                        if (PlayRuleUtil.IsContainTypePoke(myRestPokerList, beforeOutPokerList[0].m_pokerType,out typeInfo))
                        {
                            if (myOutPokerList[0].m_pokerType == beforeOutPokerList[0].m_pokerType)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }

                }
                break;

                case CheckOutPoker.OutPokerType.OutPokerType_Double:
                    {
                        //第一个人出的是主牌对子
                        if (PlayRuleUtil.IsMasterPoker(beforeOutPokerList[0], mLevelPokerNum, masterPokerType))
                        {
                            List<PokerInfo> masterPoker =
                                PlayRuleUtil.GetMasterPoker(myRestPokerList, mLevelPokerNum, masterPokerType);
                            //手中有主牌
                            if (masterPoker.Count > 0)
                            {
                                if (masterPoker.Count == 1)
                                {
                                    if (PlayRuleUtil.IsContainMasterPoker(myOutPokerList, mLevelPokerNum,
                                        masterPokerType))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                                //主牌中包含对子
                                if (PlayRuleUtil.IsContainDoublePoker(masterPoker))
                                {
                                    //出的牌都要是主牌
                                    if (PlayRuleUtil.IsAllMasterPoker(myOutPokerList, mLevelPokerNum, masterPokerType))
                                    {
                                        if (myOutPokerList[0].m_num == myOutPokerList[1].m_num
                                            && myOutPokerList[0].m_pokerType == myOutPokerList[1].m_pokerType)
                                        {
                                            return true;
                                        }
                                        else
                                        {
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                                //主牌中没有对子，且大于两张
                                else
                                {
                                    if (PlayRuleUtil.IsAllMasterPoker(myOutPokerList, mLevelPokerNum, masterPokerType))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                            }
                            //手中没有主牌
                            else
                            {
                                return true;
                            }

                        }
                        //出的是副牌
                        else
                        {
                            List<PokerInfo> typeInfo;
                            if (PlayRuleUtil.IsContainTypePoke(myRestPokerList, beforeOutPokerList[0].m_pokerType, out typeInfo))
                            {
                                if (typeInfo.Count == 1)
                                {
                                    if (myOutPokerList[0].m_pokerType == beforeOutPokerList[0].m_pokerType ||
                                        myOutPokerList[1].m_pokerType == beforeOutPokerList[0].m_pokerType)
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                                else
                                {
                                    if(myOutPokerList[0].m_pokerType == beforeOutPokerList[0].m_pokerType &&
                                        myOutPokerList[1].m_pokerType == beforeOutPokerList[0].m_pokerType)
                                    {
                                        return true;
                                    }else
                                    {
                                        return false;
                                    }
                                }
                            }
                            else
                            {
                                return true;
                            }
                        }
                    }
                    break;

                case CheckOutPoker.OutPokerType.OutPokerType_TuoLaJi:
                    {
                        int count = beforeOutPokerList.Count;
                        {
                            // 是拖拉机
                            {
                                if (checkOutPokerType(myOutPokerList, mLevelPokerNum, masterPokerType) == OutPokerType.OutPokerType_TuoLaJi)
                                {
                                    return true;
                                }
                                else
                                {
                                    // 检测是否有拖拉机而玩家没出，是的话则出牌失败
                                    {
                                        List<TLJCommon.PokerInfo> tempList = new List<TLJCommon.PokerInfo>();
                                        for (int i = myRestPokerList.Count - 1; i >= (count - 1); i--)
                                        {
                                            if (myRestPokerList[i].m_pokerType == beforeOutPokerList[0].m_pokerType)
                                            {
                                                for (int j = 0; j < count; j++)
                                                {
                                                    tempList.Add(new TLJCommon.PokerInfo(myRestPokerList[i - j].m_num, myRestPokerList[i - j].m_pokerType));
                                                }

                                                // 找到拖拉机了
                                                if (CheckOutPoker.checkOutPokerType(tempList, mLevelPokerNum, masterPokerType) == CheckOutPoker.OutPokerType.OutPokerType_TuoLaJi)
                                                {
                                                    return false;
                                                }
                                                else
                                                {
                                                    tempList.Clear();
                                                }
                                            }
                                            else
                                            {
                                                tempList.Clear();
                                            }
                                        }
                                    }
                                }
                            }

                            // 不是拖拉机，检测是否都是对子
                            {
                                int doubleNum = beforeOutPokerList.Count / 2;
                                int myOutDoubleNum = 0;
                                int myRestDoubleNum = 0;
                                for (int i = 0; i < myOutPokerList.Count; i += 2)
                                {
                                    if ((myOutPokerList[i].m_num != myOutPokerList[i + 1].m_num) &&
                                        (myOutPokerList[i].m_pokerType != myOutPokerList[i + 1].m_pokerType) &&
                                        (myOutPokerList[i].m_pokerType != beforeOutPokerList[0].m_pokerType))
                                    {
                                        ++myOutDoubleNum;
                                    }
                                }

                                // 都是对子
                                if (myOutDoubleNum == doubleNum)
                                {
                                    return true;
                                }
                                else
                                {
                                    for (int i = myRestPokerList.Count - 1; i >= 1; i--)
                                    {
                                        if ((myRestPokerList[i].m_num == myRestPokerList[i - 1].m_num) &&
                                            (myRestPokerList[i].m_pokerType == myRestPokerList[i - 1].m_pokerType) &&
                                            (myRestPokerList[i].m_pokerType == beforeOutPokerList[0].m_pokerType))
                                        {
                                            ++myRestDoubleNum;
                                        }
                                    }

                                    // 我出的不都是对子，但是剩余的牌中明明有对子，则出牌失败
                                    if (myRestDoubleNum > myOutDoubleNum)
                                    {
                                        return false;
                                    }
                                    else
                                    {
                                        int restSampleTypeNum = 0;
                                        int outSampleTypeNum = 0;

                                        for (int i = 0; i < myRestPokerList.Count; i++)
                                        {
                                            if (myRestPokerList[i].m_pokerType == beforeOutPokerList[0].m_pokerType)
                                            {
                                                ++restSampleTypeNum;
                                            }
                                        }

                                        for (int i = 0; i < myOutPokerList.Count; i++)
                                        {
                                            if (myOutPokerList[i].m_pokerType == beforeOutPokerList[0].m_pokerType)
                                            {
                                                ++outSampleTypeNum;
                                            }
                                        }

                                        if (outSampleTypeNum == count)
                                        {
                                            return true;
                                        }
                                        else
                                        {
                                            if (restSampleTypeNum > outSampleTypeNum)
                                            {
                                                return false;
                                            }
                                            else
                                            {
                                                return true;
                                            }
                                        }

                                    }
                                }
                            }
                        }

                    }
                    break;

                case CheckOutPoker.OutPokerType.OutPokerType_ShuaiPai:
                    {

                    }
                    break;
            }
        }

        return true;
    }

    public static OutPokerType checkOutPokerType(List<TLJCommon.PokerInfo> outPokerList, int mLevelPokerNum, int masterPokerType)
    {
        PlayRuleUtil.SetPokerWeight(outPokerList, mLevelPokerNum, (Consts.PokerType) masterPokerType);

        int count = outPokerList.Count;

        if (count == 0)
        {
            return OutPokerType.OutPokerType_Error;
        }
        // 单牌
        else if (count == 1)
        {
            return OutPokerType.OutPokerType_Single;
        }
        // 检查是否是对子
        else if (count == 2)
        {
            if ((outPokerList[0].m_pokerType == outPokerList[1].m_pokerType) && (outPokerList[0].m_num == outPokerList[1].m_num))
            {
                return OutPokerType.OutPokerType_Double;
            }
        }
        else if (count % 2 == 0 && count >= 4)
        {

            if (PlayRuleUtil.IsTuoLaJi(outPokerList, mLevelPokerNum, masterPokerType))
            {
                LogUtil.getInstance().writeLogToLocalNow("出的是拖拉机");
                return OutPokerType.OutPokerType_TuoLaJi;
            }
        }
        return OutPokerType.OutPokerType_Error;
        // 检查是否是拖拉机
        //        else if (((count % 2) == 0) && (count >= 4))
        //        {
        //            bool isSampleType = true;
        //
        //            for (int i = 1; i < outPokerList.Count; i++)
        //            {
        //                if (outPokerList[i].m_pokerType != outPokerList[0].m_pokerType)
        //                {
        //                    isSampleType = false;
        //                    break;
        //                }
        //            }
        //
        //            if (isSampleType)
        //            {
        //                bool isTuoLaJi = true;
        //                int beforeNum = outPokerList[0].m_num + 1;
        //                for (int i = 0; i < outPokerList.Count - 1; i += 2)
        //                {
        //                    if ((outPokerList[i].m_num == outPokerList[i + 1].m_num) && ((outPokerList[i].m_num - beforeNum) == -1))
        //                    {
        //                        beforeNum = outPokerList[i].m_num;
        //                    }
        //                    else
        //                    {
        //                        isTuoLaJi = false;
        //                        break;
        //                    }
        //                }
        //
        //                if (isTuoLaJi)
        //                {
        //                    return OutPokerType.OutPokerType_TuoLaJi;
        //                }
        //            }
        //    }
    }
}
