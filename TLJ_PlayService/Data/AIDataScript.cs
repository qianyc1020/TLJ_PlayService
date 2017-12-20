using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class AIDataScript
{
    static AIDataScript s_instance = null;

    List<AIData> m_dataList = new List<AIData>();

    public static AIDataScript getInstance()
    {
        if (s_instance == null)
        {
            s_instance = new AIDataScript();
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

        try
        {
            JObject jo = JObject.Parse(json);
            if ((int)jo.GetValue("code") == (int)TLJCommon.Consts.Code.Code_OK)
            {
                JArray room_list = (JArray)JsonConvert.DeserializeObject(jo.GetValue("aiList").ToString());

                for (int i = 0; i < room_list.Count; i++)
                {
                    AIData temp = new AIData(room_list[i]["uid"].ToString());

                    m_dataList.Add(temp);

                    LogUtil.getInstance().addDebugLog("AIDataScript.initJson---" + "增加机器人：" + temp.m_uid);
                }
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error("AIDataScript.initJson----" + ex.Message);
        }
    }

    public string getOneAI()
    {
        string uid = "";

        {
            bool hasNoUsed = false;
            for (int i = 0; i < m_dataList.Count; i++)
            {
                if (!m_dataList[i].m_isUsed)
                {
                    hasNoUsed = true;
                    break;
                }
            }

            if (!hasNoUsed)
            {
                return uid;
            }
        }

        while (true)
        {
            int r = RandomUtil.getRandom(0, m_dataList.Count - 1);
            if (!m_dataList[r].m_isUsed)
            {
                uid = m_dataList[r].m_uid;
                m_dataList[r].m_isUsed = true;

                return uid;
            }
        }

        //for (int i = 0; i < m_dataList.Count; i++)
        //{
        //    if (!m_dataList[i].m_isUsed)
        //    {
        //        uid = m_dataList[i].m_uid;
        //        m_dataList[i].m_isUsed = true;

        //        break;
        //    }
        //}

        return uid;
    }

    public void backOneAI(string uid)
    {
        for (int i = 0; i < m_dataList.Count; i++)
        {
            if (m_dataList[i].m_uid.CompareTo(uid) == 0)
            {
                m_dataList[i].m_isUsed = false;
                break;
            }
        }
    }
}

class AIData
{
    public string m_uid = "";
    public bool m_isUsed = false;

    public AIData(string uid)
    {
        m_uid = uid;
    }
}