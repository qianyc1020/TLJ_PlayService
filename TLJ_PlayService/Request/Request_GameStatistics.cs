using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;

class Request_GameStatistics
{
    public static void doRequest(RoomData room,List<string> winnerList)
    {
        try
        {
            JObject respondJO = new JObject();

            respondJO.Add("tag", TLJCommon.Consts.Tag_GameStatistics);
            respondJO.Add("roomid", room.getRoomId());
            respondJO.Add("gameroomname", GameUtil.getRoomName(room.m_gameRoomType));
            respondJO.Add("player1_uid", room.getPlayerDataList()[0].m_uid);
            respondJO.Add("player2_uid", room.getPlayerDataList()[1].m_uid);
            respondJO.Add("player3_uid", room.getPlayerDataList()[2].m_uid);
            respondJO.Add("player4_uid", room.getPlayerDataList()[3].m_uid);
            respondJO.Add("zhuangjia_uid", room.m_zhuangjiaPlayerData.m_uid);
            respondJO.Add("winner1_uid", winnerList[0]);
            respondJO.Add("winner2_uid", winnerList[1]);
            respondJO.Add("cur_pvp_round", room.m_rounds_pvp);

            string reward = "";
            for (int i = 0; i < room.getPlayerDataList().Count; i++)
            {
                reward += (room.getPlayerDataList()[i].m_uid + ":" + room.getPlayerDataList()[i].m_pvpReward + "#");
            }
            respondJO.Add("pvp_reward", reward);

            // 传给数据库服务器
            {
                if (!PlayService.m_mySqlServerUtil.sendMseeage(respondJO.ToString()))
                {
                    // 连接不上数据库服务器
                }
            }
        }
        catch (Exception ex)
        {
            // 客户端参数错误
            TLJ_PlayService.PlayService.log.Error("Request_GameStatistics----" + ex.Message);
        }
    }
}