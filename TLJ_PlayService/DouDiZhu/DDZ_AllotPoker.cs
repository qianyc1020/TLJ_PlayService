using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// 给玩家发牌
class DDZ_AllotPoker
{
    public static List<List<TLJCommon.PokerInfo>> AllotPokerToPlayer(string gameRoomType)
    {
        List<TLJCommon.PokerInfo> pokerHeapList = new List<TLJCommon.PokerInfo>();
        List<List<TLJCommon.PokerInfo>> list = new List<List<TLJCommon.PokerInfo>>();

        if (gameRoomType.CompareTo(TLJCommon.Consts.GameRoomType_DDZ_Normal) == 0)
        {
            {
                for (int i = 2; i <= 14; i++)
                {
                    pokerHeapList.Add(new TLJCommon.PokerInfo(i, TLJCommon.Consts.PokerType.PokerType_FangKuai));

                    pokerHeapList.Add(new TLJCommon.PokerInfo(i, TLJCommon.Consts.PokerType.PokerType_HeiTao));

                    pokerHeapList.Add(new TLJCommon.PokerInfo(i, TLJCommon.Consts.PokerType.PokerType_HongTao));

                    pokerHeapList.Add(new TLJCommon.PokerInfo(i, TLJCommon.Consts.PokerType.PokerType_MeiHua));


                }

                for (int i = 15; i <= 16; i++)
                {
                    pokerHeapList.Add(new TLJCommon.PokerInfo(i, TLJCommon.Consts.PokerType.PokerType_Wang));
                }
            }

            for (int i = 0; i < 4; i++)
            {
                List<TLJCommon.PokerInfo> temp = new List<TLJCommon.PokerInfo>();
                list.Add(temp);

                if (i <= 2)
                {
                    for (int j = 0; j < 17; j++)
                    {
                        int r = RandomUtil.getRandom(0, pokerHeapList.Count - 1);
                        temp.Add(pokerHeapList[r]);
                        pokerHeapList.RemoveAt(r);
                    }
                }
                else
                {
                    for (int j = 0; j < pokerHeapList.Count; j++)
                    {
                        temp.Add(pokerHeapList[j]);
                    }
                }
            }
        }

        return list;
    }

    public static List<List<TLJCommon.PokerInfo>> AllotPokerToPlayerByDebug()
    {
        List<List<TLJCommon.PokerInfo>> list = new List<List<TLJCommon.PokerInfo>>();

        for (int i = 0; i < 5; i++)
        {
            List<TLJCommon.PokerInfo> temp = new List<TLJCommon.PokerInfo>();

            {
                string path = "C: \\Users\\Administrator\\Desktop\\player1.txt";
                switch (i)
                {
                    case 0:
                        {
                            path = "C: \\Users\\Administrator\\Desktop\\player1.txt";
                        }
                        break;

                    case 1:
                        {
                            path = "C: \\Users\\Administrator\\Desktop\\player2.txt";
                        }
                        break;

                    case 2:
                        {
                            path = "C: \\Users\\Administrator\\Desktop\\player3.txt";
                        }
                        break;

                    case 3:
                        {
                            path = "C: \\Users\\Administrator\\Desktop\\player4.txt";
                        }
                        break;

                    case 4:
                        {
                            path = "C: \\Users\\Administrator\\Desktop\\dipai.txt";
                        }
                        break;
                }
                // 读取文件
                StreamReader sr = new StreamReader(path, System.Text.Encoding.GetEncoding("utf-8"));
                string text = sr.ReadToEnd().ToString();
                sr.Close();

                string str = text;
                str = str.Replace("\r\n", ";");

                List<string> listStr = new List<string>();
                CommonUtil.splitStr(str, listStr, ';');

                for (int j = 0; j < listStr.Count; j++)
                {
                    List<string> listStr2 = new List<string>();
                    CommonUtil.splitStr(listStr[j], listStr2, ':');
                    temp.Add(new TLJCommon.PokerInfo(int.Parse(listStr2[0]), (TLJCommon.Consts.PokerType)(int.Parse(listStr2[1]))));
                }
            }

            list.Add(temp);
        }

        return list;
    }
}