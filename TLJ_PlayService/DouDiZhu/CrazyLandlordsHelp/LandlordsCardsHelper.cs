using System;
using System.Collections.Generic;
using System.Linq;

namespace CrazyLandlords.Helper
{
    public static class LandlordsCardsHelper
    {
        /// <summary>
        /// 获取牌组权重
        /// </summary>
        /// <param name="cards"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        public static int GetWeight(LandlordsCard[] cards, CardsType rule)
        {
            int totalWeight = 0;
            if (rule == CardsType.JokerBoom)
            {
                totalWeight = int.MaxValue;
            }
            else if (rule == CardsType.Boom)
            {
                totalWeight = (int)cards[0].CardWeight * (int)cards[1].CardWeight * (int)cards[2].CardWeight * (int)cards[3].CardWeight + (int.MaxValue / 2);
            }
            else if (rule == CardsType.BoomAndTwo || rule == CardsType.BoomAndOne)
            {
                for (int i = 0; i < cards.Length - 3; i++)
                {
                    if (cards[i].CardWeight == cards[i + 1].CardWeight &&
                        cards[i].CardWeight == cards[i + 2].CardWeight &&
                        cards[i].CardWeight == cards[i + 3].CardWeight)
                    {
                        totalWeight += (int)cards[i].CardWeight;
                        totalWeight *= 4;
                        break;
                    }
                }
            }
            else if (rule == CardsType.ThreeAndOne || rule == CardsType.ThreeAndTwo)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (i < cards.Length - 2)
                    {
                        if (cards[i].CardWeight == cards[i + 1].CardWeight &&
                            cards[i].CardWeight == cards[i + 2].CardWeight)
                        {
                            totalWeight += (int)cards[i].CardWeight;
                            totalWeight *= 3;
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    totalWeight += (int)cards[i].CardWeight;
                }
            }

            return totalWeight;
        }

        /// <summary>
        /// 根据Weight在卡组中找到所有重复的卡牌(两个及两个以上)
        /// </summary>
        /// <param name="cards"></param>
        /// <returns>Weight:类型，int:重复的个数</returns>
        public static Dictionary<Weight, int> FindSameCards(LandlordsCard[] cards)
        {
            return cards.GroupBy(x => x.CardWeight).Where(x => x.Count() > 1).ToDictionary(x => x.Key, y => y.Count());
        }

        /// <summary>
        /// 根据Weight在卡组中找到所有重复的卡牌(两个以上)
        /// </summary>
        /// <param name="cards"></param>
        /// <returns>Weight:类型，int:重复的个数</returns>
        private static Dictionary<Weight, int> FindTripleCards(LandlordsCard[] cards)
        {
            return cards.GroupBy(x => x.CardWeight).Where(x => x.Count() > 2).ToDictionary(x => x.Key, y => y.Count());
        }

        /// <summary>f
        /// 卡组排序
        /// </summary>
        /// <param name="cards"></param>
        public static void SortCards(List<LandlordsCard> cards)
        {
            cards.Sort(
                (LandlordsCard a, LandlordsCard b) =>
                {
                    //先按照权重降序，再按花色升序
                    return -a.CardWeight.CompareTo(b.CardWeight) * 2 + a.CardWeight.CompareTo(b.CardWeight);
                }
            );
        }

        /// <summary>
        /// 是否是单
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static bool IsSingle(LandlordsCard[] cards)
        {
            if (cards.Length == 1)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 是否是对子
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static bool IsDouble(LandlordsCard[] cards)
        {
            if (cards.Length == 2)
            {
                if (cards[0].CardWeight == cards[1].CardWeight)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 是否是4带2个对
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static bool IsBoomAndTwo(LandlordsCard[] cards)
        {
            //4444 + 55 + 77
            if (cards.Length == 8)
            {
                Dictionary<Weight, int> findSameCards = FindSameCards(cards);
                int totalNum = 0;
                foreach (var item in findSameCards.Values)
                {
                    totalNum += item;
                }
                if (findSameCards.Values.Contains(4) && findSameCards.Values.Contains(2) && totalNum == 8)
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// 是否是4带2个单
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static bool IsBoomAndOne(LandlordsCard[] cards)
        {
            //5555 + 3 + 8
            if (cards.Length == 6)
            {
                Dictionary<Weight, int> findSameCards = FindSameCards(cards);
                foreach (var item in findSameCards.Values)
                {
                    //有4个重复的元素
                    if (item == 4)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 是否是顺子
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static bool IsStraight(LandlordsCard[] cards)
        {
            if (cards.Length < 5 || cards.Length > 12)
                return false;
            for (int i = 0; i < cards.Length - 1; i++)
            {
                Weight w = cards[i].CardWeight;
                if (w - cards[i + 1].CardWeight != 1)
                    return false;

                //不能超过A
                if (w > Weight.One || cards[i + 1].CardWeight > Weight.One)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 是否是双顺子
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static bool IsDoubleStraight(LandlordsCard[] cards)
        {
            if (cards.Length < 6 || cards.Length % 2 != 0)
                return false;

            for (int i = 0; i < cards.Length; i += 2)
            {
                if (cards[i + 1].CardWeight != cards[i].CardWeight)
                    return false;

                if (i < cards.Length - 2)
                {
                    if (cards[i].CardWeight - cards[i + 2].CardWeight != 1)
                        return false;

                    //不能超过A
                    if (cards[i].CardWeight > Weight.One || cards[i + 2].CardWeight > Weight.One)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 飞机不带
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static bool IsTripleStraight(LandlordsCard[] cards)
        {
            if (cards.Length < 6 || cards.Length % 3 != 0)
                return false;

            for (int i = 0; i < cards.Length; i += 3)
            {
                if (cards[i + 1].CardWeight != cards[i].CardWeight)
                    return false;
                if (cards[i + 2].CardWeight != cards[i].CardWeight)
                    return false;
                if (cards[i + 1].CardWeight != cards[i + 2].CardWeight)
                    return false;

                if (i < cards.Length - 3)
                {
                    if (cards[i].CardWeight - cards[i + 3].CardWeight != 1)
                        return false;

                    //不能超过A
                    if (cards[i].CardWeight > Weight.One || cards[i + 3].CardWeight > Weight.One)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 三顺 带单
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static bool IsTripleStraightAndOne(LandlordsCard[] cards)
        {
            if (cards.Length % 4 != 0 || cards.Length < 8)
                return false;
            //找出3张以上的牌
            Dictionary<Weight, int> findSameCards = FindTripleCards(cards);
            List<Weight> weights = new List<Weight>(findSameCards.Keys);
            //两个3张以上
            if (weights.Count < 2)
                return false;
            //是否是连续
            for (int i = 0; i < weights.Count - 1; i++)
            {
                if (weights[i] - weights[i + 1] != 1)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 三顺 带双
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static bool IsTripleStraightAndTwo(LandlordsCard[] cards)
        {
            if (cards.Length % 5 != 0 || cards.Length < 10)
                return false;
            Dictionary<Weight, int> doubleCards = cards.GroupBy(x => x.CardWeight).Where(x => x.Count() == 2).ToDictionary(x => x.Key, y => y.Count());
            Dictionary<Weight, int> tripleCards = cards.GroupBy(x => x.CardWeight).Where(x => x.Count() == 3).ToDictionary(x => x.Key, y => y.Count());
            List<Weight> doubleweights = new List<Weight>(doubleCards.Keys);
            List<Weight> tripleweights = new List<Weight>(tripleCards.Keys);

            if (tripleweights.Count < 2 || tripleweights.Count != doubleweights.Count)
                return false;
            //是否是连续
            for (int i = 0; i < tripleweights.Count - 1; i++)
            {
                if (tripleweights[i] - tripleweights[i + 1] != 1)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 三不带
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static bool IsOnlyThree(LandlordsCard[] cards)
        {
            if (cards.Length != 3)
                return false;
            if (cards[0].CardWeight != cards[1].CardWeight)
                return false;
            if (cards[1].CardWeight != cards[2].CardWeight)
                return false;
            if (cards[0].CardWeight != cards[2].CardWeight)
                return false;
            return true;
        }

        /// <summary>
        /// 三带一
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static bool IsThreeAndOne(LandlordsCard[] cards)
        {
            if (cards.Length != 4)
                return false;
            if (cards[0].CardWeight == cards[1].CardWeight &&
                cards[1].CardWeight == cards[2].CardWeight)
                return true;
            else if (cards[1].CardWeight == cards[2].CardWeight &&
                cards[2].CardWeight == cards[3].CardWeight)
                return true;
            return false;
        }

        /// <summary>
        /// 三代二
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static bool IsThreeAndTwo(LandlordsCard[] cards)
        {
            if (cards.Length != 5)
                return false;

            if (cards[0].CardWeight == cards[1].CardWeight &&
                cards[1].CardWeight == cards[2].CardWeight)
            {
                if (cards[3].CardWeight == cards[4].CardWeight)
                    return true;
            }

            else if (cards[2].CardWeight == cards[3].CardWeight && 
                     cards[3].CardWeight == cards[4].CardWeight)
            {
                if (cards[0].CardWeight == cards[1].CardWeight)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 炸弹
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static bool IsBoom(LandlordsCard[] cards)
        {
            if (cards.Length != 4)
                return false;
            if (cards[0].CardWeight != cards[1].CardWeight)
                return false;
            if (cards[1].CardWeight != cards[2].CardWeight)
                return false;
            if (cards[2].CardWeight != cards[3].CardWeight)
                return false;
            return true;
        }

        /// <summary>
        /// 王炸
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public static bool IsJokerBoom(LandlordsCard[] cards)
        {
            if (cards.Length != 2)
                return false;
            if (cards[0].CardWeight == Weight.SJoker)
            {
                if (cards[1].CardWeight == Weight.LJoker)
                    return true;
                return false;
            }
            else if (cards[0].CardWeight == Weight.LJoker)
            {
                if (cards[1].CardWeight == Weight.SJoker)
                    return true;
                return false;
            }

            return false;
        }

        /// <summary>
        /// 获取出牌的牌型
        /// </summary>
        /// <param name="cards"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool GetCardsType(LandlordsCard[] cards, out CardsType type)
        {
            type = CardsType.None;
            bool isRule = false;
            switch (cards?.Length)
            {
                case 1:
                    isRule = true;
                    type = CardsType.Single;
                    break;
                case 2:
                    if (IsDouble(cards))
                    {
                        isRule = true;
                        type = CardsType.Double;
                    }
                    else if (IsJokerBoom(cards))
                    {
                        isRule = true;
                        type = CardsType.JokerBoom;
                    }
                    break;
                case 3:
                    if (IsOnlyThree(cards))
                    {
                        isRule = true;
                        type = CardsType.OnlyThree;
                    }
                    break;
                case 4:
                    if (IsBoom(cards))
                    {
                        isRule = true;
                        type = CardsType.Boom;
                    }
                    else if (IsThreeAndOne(cards))
                    {
                        isRule = true;
                        type = CardsType.ThreeAndOne;
                    }
                    break;
                case 5:
                    if (IsStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.Straight;
                    }
                    else if (IsThreeAndTwo(cards))
                    {
                        isRule = true;
                        type = CardsType.ThreeAndTwo;
                    }
                    break;
                case 6:
                    if (IsStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.Straight;
                    }
                    else if (IsTripleStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.TripleStraight;
                    }
                    else if (IsDoubleStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.DoubleStraight;
                    }
                    else if (IsBoomAndOne(cards))
                    {
                        isRule = true;
                        type = CardsType.BoomAndOne;
                    }
                    break;
                case 7:
                    if (IsStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.Straight;
                    }
                    break;
                case 8:
                    if (IsStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.Straight;
                    }
                    else if (IsDoubleStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.DoubleStraight;
                    }
                    else if (IsBoomAndTwo(cards))
                    {
                        isRule = true;
                        type = CardsType.BoomAndTwo;
                    }
                    else if (IsTripleStraightAndOne(cards))
                    {
                        isRule = true;
                        type = CardsType.TripleStraightAndOne;
                    }
                    break;
                case 9:
                    if (IsStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.Straight;
                    }
                    else if (IsOnlyThree(cards))
                    {
                        isRule = true;
                        type = CardsType.OnlyThree;
                    }
                    break;
                case 10:
                    if (IsStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.Straight;
                    }
                    else if (IsDoubleStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.DoubleStraight;
                    }
                    else if (IsTripleStraightAndTwo(cards))
                    {
                        isRule = true;
                        type = CardsType.TripleStraightAndTwo;
                    }
                    break;

                case 11:
                    if (IsStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.Straight;
                    }
                    break;
                case 12:
                    if (IsStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.Straight;
                    }
                    else if (IsDoubleStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.DoubleStraight;
                    }
                    else if (IsOnlyThree(cards))
                    {
                        isRule = true;
                        type = CardsType.OnlyThree;
                    }
                    else if (IsTripleStraightAndOne(cards))
                    {
                        isRule = true;
                        type = CardsType.TripleStraightAndOne;
                    }
                    break;
                case 13:
                    break;
                case 14:
                    if (IsDoubleStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.DoubleStraight;
                    }
                    break;
                case 15:
                    if (IsOnlyThree(cards))
                    {
                        isRule = true;
                        type = CardsType.OnlyThree;
                    }
                    else if (IsTripleStraightAndTwo(cards))
                    {
                        isRule = true;
                        type = CardsType.TripleStraightAndTwo;
                    }
                    break;
                case 16:
                    if (IsDoubleStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.DoubleStraight;
                    }
                    else if (IsTripleStraightAndOne(cards))
                    {
                        isRule = true;
                        type = CardsType.TripleStraightAndOne;
                    }
                    break;
                case 17:
                    break;
                case 18:
                    if (IsDoubleStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.DoubleStraight;
                    }
                    else if (IsOnlyThree(cards))
                    {
                        isRule = true;
                        type = CardsType.OnlyThree;
                    }
                    break;
                case 19:
                    break;

                case 20:
                    if (IsDoubleStraight(cards))
                    {
                        isRule = true;
                        type = CardsType.DoubleStraight;
                    }
                    else if (IsTripleStraightAndOne(cards))
                    {
                        isRule = true;
                        type = CardsType.TripleStraightAndOne;
                    }
                    else if (IsTripleStraightAndTwo(cards))
                    {
                        isRule = true;
                        type = CardsType.TripleStraightAndTwo;
                    }
                    break;
                default:
                    break;
            }
            return isRule;
        }

        public static List<LandlordsCard> GetActiveCard(List<LandlordsCard> cards)
        {
            List<LandlordsCard> temp = new List<LandlordsCard>();

            List<LandlordsCard> jokerBoomCards = new List<LandlordsCard>();
            List<LandlordsCard> boomCards = new List<LandlordsCard>();

            List<LandlordsCard> tripleCards = new List<LandlordsCard>();
            List<List<LandlordsCard>> tripleStraghtCards;

//            List<LandlordsCard> tripleStraghtCards = new List<LandlordsCard>();

            //拷贝一份
            List<LandlordsCard> copyCards = new List<LandlordsCard>(cards);

            //检索王炸
            if (copyCards.Count >= 2)
            {
                LandlordsCard[] groupCards = new LandlordsCard[2];
                groupCards[0] = copyCards[0];
                groupCards[1] = copyCards[1];

                if (IsJokerBoom(groupCards))
                {
                    jokerBoomCards.AddRange(groupCards);
                }
            }

            //检索炸弹
            for (int i = copyCards.Count - 1; i >= 3; i--)
            {
                LandlordsCard[] groupCards = new LandlordsCard[4];
                groupCards[0] = copyCards[i - 3];
                groupCards[1] = copyCards[i - 2];
                groupCards[2] = copyCards[i - 1];
                groupCards[3] = copyCards[i];

                if (IsBoom(groupCards))
                {
                    boomCards.AddRange(groupCards);
                }
            }

            //检索3张
            IGrouping<Weight, LandlordsCard>[] tempTripleCards = copyCards.GroupBy(x => x.CardWeight).Where(x => x.Count() == 3).ToArray();
            foreach (var item in tempTripleCards)
            {
                tripleCards.AddRange(item);
            }
            //在3张中检索3顺
            tripleStraghtCards = GetAllTripleStraght(tripleCards);
            foreach (var tripleStraghtCard in tripleStraghtCards)
            {
                foreach (var card in tripleStraghtCard)
                {
                    tripleCards.Remove(card);
                }
            }

        
            //排除3顺，炸弹的情况下，用剩下来的牌组成单顺
            copyCards.RemoveList(jokerBoomCards);
            copyCards.RemoveList(boomCards);
            copyCards.RemoveList(tripleStraghtCards);

            List<List<LandlordsCard>> allFiveStraght = FindAllFiveStraght(copyCards);
            copyCards.RemoveList(allFiveStraght);

            return null;
        }

        /// <summary>
        /// 从3张中获得3顺
        /// </summary>
        /// <param name="tripleCards"></param>
        /// <returns></returns>
        private static List<LandlordsCard> GetTripleStraght(List<LandlordsCard> tripleCards)
        {
            List<LandlordsCard> tempCards = new List<LandlordsCard>();

            for (int i = 0; i < tripleCards.Count - 3; i += 3)
            {
                int k = 1;
                for (int j = i; j < tripleCards.Count - 3; j += 3)
                {
                    if (tripleCards[i].CardWeight - tripleCards[j + 3].CardWeight == k)
                    {
                        tempCards.Add(tripleCards[j + 3]);
                        tempCards.Add(tripleCards[j + 4]);
                        tempCards.Add(tripleCards[j + 5]);
                    }

                    k++;
                }
                if (tempCards.Count > 0)
                {
                    tempCards.Add(tripleCards[i]);
                    tempCards.Add(tripleCards[i + 1]);
                    tempCards.Add(tripleCards[i + 2]);
                    break;
                }
            }

            SortCards(tempCards);
            return tempCards;
        }

        public static List<List<LandlordsCard>> GetAllTripleStraght(List<LandlordsCard> tripleCards)
        {
            List<List<LandlordsCard>> result = new List<List<LandlordsCard>>();
            List<LandlordsCard> copyCards = new List<LandlordsCard>(tripleCards);
            while (copyCards.Count >= 6)
            {
                List<LandlordsCard> landlordsCards = GetTripleStraght(copyCards);
                if (landlordsCards.Count == 0) break;
                foreach (var VARIABLEs in landlordsCards)
                {
                    copyCards.Remove(VARIABLEs);
                }
                result.Add(landlordsCards);
            }
            return result;
        }

        /// <summary>
        /// 提示出牌
        /// </summary>
        /// <param name="cards"></param>
        /// <param name="lastCards"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<LandlordsCard[]> GetPrompt(List<LandlordsCard> cards, List<LandlordsCard> lastCards, CardsType type)
        {
            List<LandlordsCard[]> result = new List<LandlordsCard[]>();
            List<LandlordsCard> copyCards = new List<LandlordsCard>(cards);

            IGrouping<Weight, LandlordsCard>[] boomCards = copyCards.GroupBy(x => x.CardWeight).Where(x => x.Count() == 4).ToArray();
            IGrouping<Weight, LandlordsCard>[] tripleCards = copyCards.GroupBy(x => x.CardWeight).Where(x => x.Count() == 3).ToArray();
            IGrouping<Weight, LandlordsCard>[] doubleCards = copyCards.GroupBy(x => x.CardWeight).Where(x => x.Count() == 2).ToArray();
            IGrouping<Weight, LandlordsCard>[] singleCards = copyCards.GroupBy(x => x.CardWeight).Where(x => x.Count() == 1).ToArray();

            //TODO 缺少牌桌上的牌
            LandlordsCard[] deskCards = lastCards.ToArray();
            IGrouping<Weight, LandlordsCard>[] lastTripleCards = deskCards.GroupBy(x => x.CardWeight).Where(x => x.Count() >= 3).ToArray();
            int weight = GetWeight(deskCards, type);

            if (type == CardsType.JokerBoom)
            {
                return result;
            }

            //检索王炸
            if (copyCards.Count >= 2)
            {
                LandlordsCard[] groupCards = new LandlordsCard[2];
                groupCards[0] = copyCards[0];
                groupCards[1] = copyCards[1];

                if (IsJokerBoom(groupCards))
                {
                    result.Add(groupCards);
                }
            }
            //检索炸弹
            for (int i = copyCards.Count - 1; i >= 3; i--)
            {
                LandlordsCard[] groupCards = new LandlordsCard[4];
                groupCards[0] = copyCards[i - 3];
                groupCards[1] = copyCards[i - 2];
                groupCards[2] = copyCards[i - 1];
                groupCards[3] = copyCards[i];

                if (IsBoom(groupCards) && GetWeight(groupCards, CardsType.Boom) > weight)
                {
                    result.Add(groupCards);
                }
            }

            //删除炸弹
//            foreach (var resultCards in result)
//            {
//                foreach (var resultCard in resultCards)
//                {
//                    copyCards.Remove(resultCard);
//                }
//            }

            switch (type)
            {
                //TODO 
                case CardsType.BoomAndOne:

                    for (int i = copyCards.Count - 1; i >= 3; i--)
                    {
                        LandlordsCard[] groupCards = new LandlordsCard[6];
                        groupCards[0] = copyCards[i - 3];
                        groupCards[1] = copyCards[i - 2];
                        groupCards[2] = copyCards[i - 1];
                        groupCards[3] = copyCards[i];

                        if (GetWeight(groupCards, CardsType.Boom) > weight)
                        {
                            copyCards.RemoveRange(i - 3, 3);
                            int i1 = RandomHelper.RandomNumber(0, copyCards.Count);
                            groupCards[4] = copyCards[i1];
                            copyCards.RemoveAt(i1);

                            int i2 = RandomHelper.RandomNumber(0, copyCards.Count);
                            groupCards[5] = copyCards[i2];
                            copyCards.RemoveAt(i2);
                            result.Add(groupCards);
                        }
                    }

                    break;
                //TODO 
                case CardsType.BoomAndTwo:
                    //没有炸弹
                    if (boomCards.Length == 0 || doubleCards.Length < 2) break;
                    foreach (var item in boomCards)
                    {
                        //4带2没有对方大
                        if (((int) item.Key * 4) <= weight)
                            continue;

                        List<LandlordsCard> temp = new List<LandlordsCard>(item);
                        temp.AddRange(doubleCards[0]);
                        temp.AddRange(doubleCards[1]);
                        result.Add(temp.ToArray());
                    }
                    break;
                //TODO 
                case CardsType.TripleStraightAndOne:
                    //找出3张以上的牌
                 
                    if (tripleCards.Length < lastTripleCards.Length)
                        break;
                    
                    for (int i = 0; i < tripleCards.Length - 1; i++)
                    {
                        if (tripleCards[i].Key - tripleCards[i + 1].Key == 1)
                        {
                            List<LandlordsCard> temp = new List<LandlordsCard>();

                            temp.AddRange(tripleCards[i]);
                            temp.AddRange(tripleCards[i + 1]);
                            if (singleCards.Length >= 2)
                            {
                                temp.AddRange(singleCards[0]);
                                temp.AddRange(singleCards[1]);
                            }
                            else
                            {
                                if (doubleCards.Length > 0)
                                {
                                    temp.AddRange(doubleCards[0]);
                                }
                            }
                            result.Add(temp.ToArray());
                        }
                    }
                    break;
                //TODO 
                case CardsType.TripleStraightAndTwo:
                    if (tripleCards.Length < lastTripleCards.Length)
                        break;

                    for (int i = 0; i < tripleCards.Length - 1; i++)
                    {
                        if (tripleCards[i].Key - tripleCards[i + 1].Key == 1)
                        {
                            List<LandlordsCard> temp = new List<LandlordsCard>();

                            temp.AddRange(tripleCards[i]);
                            temp.AddRange(tripleCards[i + 1]);
                            if (doubleCards.Length >= 2)
                            {
                                temp.AddRange(doubleCards[0]);
                                temp.AddRange(doubleCards[1]);
                            }
                            else if (doubleCards.Length == 1)
                            {
                                if (tripleCards.Length > 0)
                                {
                                    temp.AddRange(doubleCards[0]);
                                    List<LandlordsCard> tempCards = new List<LandlordsCard>();
                                    foreach (var items in tripleCards)
                                    {
                                        if (items.Key == tripleCards[i].Key || items.Key == tripleCards[i + 1].Key)
                                            continue;
                                        foreach (var item in items)
                                        {
                                            tempCards.Add(item);
                                        }
                                    }

                                    for (int j = 0; j < 2; j++)
                                    {
                                        temp.Add(tempCards[j]);
                                    }
                                }
                            }
                            if(temp.Count == lastCards.Count)
                                result.Add(temp.ToArray());
                        }
                    }

                    break;
                case CardsType.OnlyThree:
                    for (int i = copyCards.Count - 1; i >= 2; i--)
                    {
                        if (copyCards[i].CardWeight <= deskCards[deskCards.Length - 1].CardWeight)
                        {
                            continue;
                        }

                        LandlordsCard[] groupCards = new LandlordsCard[3];
                        groupCards[0] = copyCards[i - 2];
                        groupCards[1] = copyCards[i - 1];
                        groupCards[2] = copyCards[i];

                        if (IsOnlyThree(groupCards) && GetWeight(groupCards, type) > weight)
                        {
                            result.Add(groupCards);
                        }
                    }
                    break;
                case CardsType.ThreeAndOne:
                    if (copyCards.Count >= 4)
                    {
                        for (int i = copyCards.Count - 1; i >= 2; i--)
                        {
                            if (copyCards[i].CardWeight <= deskCards[deskCards.Length - 1].CardWeight)
                            {
                                continue;
                            }

                            List<LandlordsCard> other = new List<LandlordsCard>(copyCards);
                            other.RemoveRange(i - 2, 3);

                            LandlordsCard[] groupCards = new LandlordsCard[4];
                            groupCards[0] = copyCards[i - 2];
                            groupCards[1] = copyCards[i - 1];
                            groupCards[2] = copyCards[i];
                            groupCards[3] = other[RandomHelper.RandomNumber(0, other.Count)];

                            if (IsThreeAndOne(groupCards) && GetWeight(groupCards, type) > weight)
                            {
                                result.Add(groupCards);
                            }
                        }
                    }
                    break;
                case CardsType.ThreeAndTwo:
                    if (copyCards.Count >= 5)
                    {
                        for (int i = copyCards.Count - 1; i >= 2; i--)
                        {
                            if (copyCards[i].CardWeight <= deskCards[deskCards.Length - 1].CardWeight)
                            {
                                continue;
                            }

                            List<LandlordsCard> other = new List<LandlordsCard>(copyCards);
                            other.RemoveRange(i - 2, 3);

                            List<LandlordsCard[]> otherDouble = GetPrompt(other, lastCards, CardsType.Double);
                            if (otherDouble.Count > 0)
                            {
                                LandlordsCard[] randomDouble = otherDouble[RandomHelper.RandomNumber(0, otherDouble.Count)];
                                LandlordsCard[] groupCards = new LandlordsCard[5];
                                groupCards[0] = copyCards[i - 2];
                                groupCards[1] = copyCards[i - 1];
                                groupCards[2] = copyCards[i];
                                groupCards[3] = randomDouble[0];
                                groupCards[4] = randomDouble[1];

                                if (IsThreeAndTwo(groupCards) && GetWeight(groupCards, type) > weight)
                                {
                                    result.Add(groupCards);
                                }
                            }
                        }
                    }
                    break;
                case CardsType.Straight:
                    /*
                     * 7 6 5 4 3
                     * 8 7 6 5 4
                     * 
                     * */
                    if (copyCards.Count >= deskCards.Length)
                    {
                        for (int i = copyCards.Count - 1; i >= deskCards.Length - 1; i--)
                        {
                            if (copyCards[i].CardWeight <= deskCards[deskCards.Length - 1].CardWeight)
                            {
                                continue;
                            }

                            //是否全部搜索完成
                            bool isTrue = true;
                            LandlordsCard[] groupCards = new LandlordsCard[deskCards.Length];
                            for (int j = 0; j < deskCards.Length; j++)
                            {
                                //搜索连续权重牌
                                LandlordsCard findCard = copyCards.FirstOrDefault(card => (int)card.CardWeight == (int)copyCards[i].CardWeight + j);
                                if (findCard == null)
                                {
                                    isTrue = false;
                                    break;
                                }
                                groupCards[deskCards.Length - 1 - j] = findCard;
                            }

                            if (isTrue && IsStraight(groupCards) && GetWeight(groupCards, type) > weight)
                            {
                                result.Add(groupCards);
                            }
                        }
                    }
                    break;
                case CardsType.DoubleStraight:
                    /*
                     * 5 5 4 4 3 3
                     * 6 6 5 5 4 4
                     * 
                     * */
                    if (copyCards.Count >= deskCards.Length)
                    {
                        for (int i = copyCards.Count - 1; i >= deskCards.Length - 1; i--)
                        {
                            if (copyCards[i].CardWeight <= deskCards[deskCards.Length - 1].CardWeight)
                            {
                                continue;
                            }

                            //是否全部搜索完成
                            bool isTrue = true;
                            LandlordsCard[] groupCards = new LandlordsCard[deskCards.Length];
                            for (int j = 0; j < deskCards.Length; j += 2)
                            {
                                //搜索连续权重牌
                                LandlordsCard[] findCards = copyCards.Where(card => (int)card.CardWeight == (int)copyCards[i].CardWeight + (j / 2)).ToArray();
                                if (findCards.Length < 2)
                                {
                                    isTrue = false;
                                    break;
                                }
                                groupCards[deskCards.Length - 2 - j] = findCards[0];
                                groupCards[deskCards.Length - 1 - j] = findCards[1];
                            }

                            if (isTrue && IsDoubleStraight(groupCards) && GetWeight(groupCards, type) > weight)
                            {
                                result.Add(groupCards);
                            }
                        }
                    }
                    break;
                case CardsType.TripleStraight:
                    if (copyCards.Count >= deskCards.Length)
                    {
                        for (int i = copyCards.Count - 1; i >= deskCards.Length - 1; i--)
                        {
                            if (copyCards[i].CardWeight <= deskCards[deskCards.Length - 1].CardWeight)
                            {
                                continue;
                            }

                            //是否全部搜索完成
                            bool isTrue = true;
                            LandlordsCard[] groupCards = new LandlordsCard[deskCards.Length];
                            for (int j = 0; j < deskCards.Length; j += 3)
                            {
                                //搜索连续权重牌
                                LandlordsCard[] findCards = copyCards.Where(card => (int)card.CardWeight == (int)copyCards[i].CardWeight + (j / 3)).ToArray();
                                if (findCards.Length < 3)
                                {
                                    isTrue = false;
                                    break;
                                }
                                groupCards[deskCards.Length - 3 - j] = findCards[0];
                                groupCards[deskCards.Length - 2 - j] = findCards[1];
                                groupCards[deskCards.Length - 1 - j] = findCards[2];
                            }

                            if (isTrue && IsTripleStraight(groupCards) && GetWeight(groupCards, type) > weight)
                            {
                                result.Add(groupCards);
                            }
                        }
                    }
                    break;
                case CardsType.Double:
                    if (copyCards.Count >= 2)
                    {
                        for (int i = copyCards.Count - 1; i >= 1; i--)
                        {
                            LandlordsCard[] groupCards = new LandlordsCard[2];
                            groupCards[0] = copyCards[i - 1];
                            groupCards[1] = copyCards[i];

                            if (IsDouble(groupCards) && GetWeight(groupCards, type) > weight)
                            {
                                result.Add(groupCards);
                            }
                        }
                    }
                    break;
                case CardsType.Single:
                    if (copyCards.Count >= 1)
                    {
                        for (int i = copyCards.Count - 1; i >= 0; i--)
                        {
                            if (copyCards[i].CardWeight <= deskCards[deskCards.Length - 1].CardWeight)
                            {
                                continue;
                            }
                            LandlordsCard[] groupCards = new LandlordsCard[1];
                            groupCards[0] = copyCards[i];

                            if (IsSingle(groupCards) && GetWeight(groupCards, type) > weight)
                            {
                                result.Add(groupCards);
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
            return result;
        }

        /// <summary>
        /// 找到5张单顺
        /// </summary>
        /// <param name="landlordsCards"></param>
        /// <returns></returns>
        public static List<LandlordsCard> FindFiveStraght(List<LandlordsCard> landlordsCards)
        {
            HashSet<Weight> cardsWeight = new HashSet<Weight>();
            List<LandlordsCard> landlordsCard = new List<LandlordsCard>();

            for (int i = landlordsCards.Count - 1; i >= 1; i--)
            {
                if (landlordsCards[i].CardWeight + 1 == landlordsCards[i - 1].CardWeight ||
                    landlordsCards[i].CardWeight == landlordsCards[i - 1].CardWeight)
                {
                    if (cardsWeight.Add(landlordsCards[i].CardWeight))
                    {
                        landlordsCard.Add(landlordsCards[i]);
                    }

                    if (cardsWeight.Add(landlordsCards[i - 1].CardWeight))
                    {
                        landlordsCard.Add(landlordsCards[i - 1]);
                    }
                    if (landlordsCard.Count == 5)
                        break;
                }
            }

            if (landlordsCard.Count != 5)
            {
                return new List<LandlordsCard>();
            }
            return landlordsCard;
        }

        /// <summary>
        /// 找到所有的5张单顺
        /// </summary>
        /// <param name="landlordsCards"></param>
        /// <returns></returns>
        public static List<List<LandlordsCard>> FindAllFiveStraght(List<LandlordsCard> landlordsCards)
        {
            List<LandlordsCard> copyCards = new List<LandlordsCard>(landlordsCards);
            List<List<LandlordsCard>> result = new List<List<LandlordsCard>>();
            while (copyCards.Count >= 5)
            {
                List<LandlordsCard> findFiveStraght = FindFiveStraght(copyCards);
                if (findFiveStraght.Count == 0)
                    break;
                result.Add(findFiveStraght);
                copyCards.RemoveList(findFiveStraght);
            }
            return result;
        }
    }
}
