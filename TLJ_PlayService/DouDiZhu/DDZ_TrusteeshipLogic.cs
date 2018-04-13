using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CrazyLandlords.Helper;
using TLJCommon;

public class DDZ_TrusteeshipLogic
{
    static string m_logFlag = "DDZ_TrusteeshipLogic";

    // 托管:出牌
    public static void trusteeshipLogic_OutPoker(DDZ_GameBase gameBase, DDZ_RoomData room, DDZ_PlayerData playerData)
    {
        TLJ_PlayService.PlayService.log.Warn("1111111111");
        try
        {
            // 轮到自己出牌
            {
                if (playerData.getPokerList().Count > 0)
                {
                    JObject backData = new JObject();
                    TLJ_PlayService.PlayService.log.Warn("2222222222222222222");
                    backData.Add("tag", room.m_tag);
                    backData.Add("uid", playerData.m_uid);
                    backData.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_PlayerOutPoker);
                    {
                        TLJ_PlayService.PlayService.log.Warn("3333333333333333333333");
                        List<TLJCommon.PokerInfo> listPoker = LandlordsCardsHelper.GetTrusteeshipPoker(room, playerData);
                        TLJ_PlayService.PlayService.log.Warn("444444444444444444444444444");
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

                        if (listPoker.Count > 0)
                        {
                            backData.Add("hasOutPoker", true);
                        }
                        else
                        {
                            backData.Add("hasOutPoker", false);
                        }
                    }

                    //LogUtil.getInstance().addDebugLog(m_logFlag + "----" + "托管出牌：" + playerData.m_uid + "  " + backData.ToString());
                    DDZ_GameLogic.doTask_ReceivePlayerOutPoker(gameBase, playerData.m_connId, backData.ToString());
                }
                else
                {
                    TLJ_PlayService.PlayService.log.Warn("55555555555555555555555");
                }
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":trusteeshipLogic_OutPoker：" + ex);
        }
    }

   

    // 托管:抢地主
    public static void trusteeshipLogic_QiangDiZhu(DDZ_GameBase gameBase, DDZ_RoomData room, DDZ_PlayerData playerData)
    {
        try
        {
            LogUtil.getInstance().writeRoomLog(room, ":托管：帮" + playerData.m_uid + "抢地主");

            JObject data = new JObject();

            data["tag"] = room.m_tag;
            data["uid"] = playerData.m_uid;
            data["playAction"] = (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_QiangDiZhu;
            data["fen"] = 0;

            DDZ_GameLogic.doTask_QiangDiZhu(gameBase, playerData.m_connId, data.ToString());
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ".trusteeshipLogic_QiangDiZhu: " + ex);
        }
    }

    // 托管:加棒
    public static void trusteeshipLogic_JiaBang(DDZ_GameBase gameBase, DDZ_RoomData room, DDZ_PlayerData playerData)
    {
        try
        {
            LogUtil.getInstance().writeRoomLog(room, ":托管：帮" + playerData.m_uid + "加棒");

            JObject data = new JObject();

            data["tag"] = room.m_tag;
            data["uid"] = playerData.m_uid;
            data["playAction"] = (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_JiaBang;
            data["isJiaBang"] = 1;

            DDZ_GameLogic.doTask_JiaBang(gameBase, playerData.m_connId, data.ToString());
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ".trusteeshipLogic_JiaBang: " + ex);
        }
    }
}
