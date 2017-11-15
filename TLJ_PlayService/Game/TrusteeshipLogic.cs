using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

public class TrusteeshipLogic
{
    static string m_logFlag = "TrusteeshipLogic";

    // 托管:出牌
    public static void trusteeshipLogic_OutPoker(GameBase gameBase, RoomData room, PlayerData playerData)
    {
        try
        {
            // 轮到自己出牌
            {
                if (playerData.getPokerList().Count > 0)
                {
                    JObject backData = new JObject();

                    backData.Add("tag", room.m_tag);
                    backData.Add("uid", playerData.m_uid);
                    backData.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_PlayerOutPoker);

                    // 自己出的牌
                    {
                        // 任意出
                        if (playerData.m_uid.CompareTo(room.m_curRoundFirstPlayer.m_uid) == 0)
                        {
                            List<TLJCommon.PokerInfo> listPoker = PlayRuleUtil.GetPokerWhenFirst(playerData.getPokerList(), room.m_levelPokerNum, room.m_masterPokerType);
                            JArray jarray = new JArray();
                            for (int i = 0; i < listPoker.Count; i++)
                            {
                                int num = listPoker[i].m_num;
                                int pokerType = (int)listPoker[i].m_pokerType;

                                {
                                    JObject temp = new JObject();
                                    temp.Add("num", num);
                                    temp.Add("pokerType", pokerType);
                                    jarray.Add(temp);
                                }
                            }
                            backData.Add("pokerList", jarray);
                        }
                        // 跟牌
                        else
                        {
                            List<TLJCommon.PokerInfo> listPoker = PlayRuleUtil.GetPokerWhenTuoGuan(room.m_curRoundFirstPlayer.m_curOutPokerList, playerData.getPokerList(), room.m_levelPokerNum, room.m_masterPokerType);
                            JArray jarray = new JArray();
                            for (int i = 0; i < listPoker.Count; i++)
                            {
                                int num = listPoker[i].m_num;
                                int pokerType = (int)listPoker[i].m_pokerType;

                                {
                                    JObject temp = new JObject();
                                    temp.Add("num", num);
                                    temp.Add("pokerType", pokerType);
                                    jarray.Add(temp);
                                }
                            }
                            backData.Add("pokerList", jarray);
                        }
                    }

                    //LogUtil.getInstance().addDebugLog(m_logFlag + "----" + "托管出牌：" + playerData.m_uid + "  " + backData.ToString());
                    GameLogic.doTask_ReceivePlayerOutPoker(gameBase, playerData.m_connId, backData.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":trusteeshipLogic异常1：" + ex.Message);
        }
    }

    // 托管:埋底
    public static void trusteeshipLogic_MaiDi(GameBase gameBase, RoomData room, PlayerData playerData)
    {
        try
        {
            LogUtil.getInstance().addDebugLog("帮玩家埋底:" + playerData.m_uid);

            if (room.m_curMaiDiPlayer.m_uid.CompareTo(playerData.m_uid) == 0)
            {
                // 停止倒计时
                playerData.m_timerUtil.stopTimer();

                JObject data = new JObject();
                data.Add("tag", room.m_tag);
                data.Add("uid", playerData.m_uid);
                data.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_MaiDi);

                List<TLJCommon.PokerInfo> myOutPokerList = new List<TLJCommon.PokerInfo>();

                if (playerData.getPokerList().Count < 8)
                {
                    LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":托管埋底:" + "数量不足");
                    return;
                }

                // 自己出的牌
                {
                    JArray ja = new JArray();
                    for (int k = 1; k <= 8; k++)
                    {
                        JObject jo = new JObject();
                        jo.Add("num", playerData.getPokerList()[playerData.getPokerList().Count - k].m_num);
                        jo.Add("pokerType", (int)playerData.getPokerList()[playerData.getPokerList().Count - k].m_pokerType);

                        ja.Add(jo);
                    }

                    data.Add("diPokerList", ja);
                }

                //LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":托管：帮" + playerData.m_uid + "埋底:" + data.ToString());
                GameLogic.doTask_MaiDi(gameBase, playerData.m_connId, data.ToString());
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ".trusteeshipLogic_MaiDi: " + ex.Message);
        }
    }

    // 托管:抄底
    public static void trusteeshipLogic_ChaoDi(GameBase gameBase, RoomData room, PlayerData playerData)
    {
        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":托管：帮" + playerData.m_uid + "抄底");

        JObject data = new JObject();

        data["tag"] = room.m_tag;
        data["uid"] = playerData.m_uid;
        data["playAction"] = (int)TLJCommon.Consts.PlayAction.PlayAction_PlayerChaoDi;
        data["hasPoker"] = 0;

        GameLogic.doTask_PlayerChaoDi(gameBase,playerData.m_connId, data.ToString());
    }
}
