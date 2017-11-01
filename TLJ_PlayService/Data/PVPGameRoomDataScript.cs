using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class PVPGameRoomDataScript
{
    static PVPGameRoomDataScript s_instance = null;

    List<PVPGameRoomData> m_dataList = new List<PVPGameRoomData>();

    public static PVPGameRoomDataScript getInstance()
    {
        if (s_instance == null)
        {
            s_instance = new PVPGameRoomDataScript();
        }

        return s_instance;
    }

    public static void clear()
    {
        s_instance = null;
    }

    public void initJson(string json)
    {
        m_dataList.Clear();

        {
            JObject jo = JObject.Parse(json);
            JArray room_list = (JArray)JsonConvert.DeserializeObject(jo.GetValue("room_list").ToString());
            
            for (int i = 0; i < room_list.Count; i++)
            {
                PVPGameRoomData temp = new PVPGameRoomData();

                temp.id = (int)room_list[i]["id"];
                temp.gameroomtype = (string)room_list[i]["gameroomtype"];
                temp.gameroomname = (string)room_list[i]["gameroomname"];
                temp.kaisairenshu = (int)room_list[i]["kaisairenshu"];
                temp.baomingfei = (string)room_list[i]["baomingfei"];
                temp.reward = (string)room_list[i]["reward"];

                m_dataList.Add(temp);
            }
        }
    }

    public List<PVPGameRoomData> getDataList()
    {
        return m_dataList;
    }

    public PVPGameRoomData getDataById(int id)
    {
        PVPGameRoomData temp = null;
        for (int i = 0; i < m_dataList.Count; i++)
        {
            if (m_dataList[i].id == id)
            {
                temp = m_dataList[i];
                break;
            }
        }

        return temp;
    }

    public PVPGameRoomData getDataByRoomType(string gameRoomType)
    {
        PVPGameRoomData temp = null;
        for (int i = 0; i < m_dataList.Count; i++)
        {
            if (m_dataList[i].gameroomtype.CompareTo(gameRoomType) == 0)
            {
                temp = m_dataList[i];
                break;
            }
        }

        return temp;
    }
}