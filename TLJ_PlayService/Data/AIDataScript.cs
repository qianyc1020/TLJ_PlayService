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

                    //LogUtil.getInstance().addDebugLog("AIDataScript.initJson---" + "增加机器人：" + temp.m_uid);
                    TLJ_PlayService.PlayService.log.Error("AIDataScript.initJson---" + "增加机器人：" + temp.m_uid);

                    //// 限制机器人数量
                    //if (m_dataList.Count >= 2)
                    //{
                    //    return;
                    //}
                }

                TLJ_PlayService.PlayService.log.Error("AIDataScript.initJson---" + "机器人总数量：" + m_dataList.Count);
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error("AIDataScript.initJson----" + ex.Message);
        }
    }

    public string getOneAI()
    {
        lock (m_dataList)
        {
            string uid = "";
            int restAICount = 0;

            {
                for (int i = 0; i < m_dataList.Count; i++)
                {
                    if (!m_dataList[i].m_isUsed)
                    {
                        ++restAICount;
                    }
                }

                if (restAICount == 0)
                {
                    return uid;
                }
            }

            if (restAICount >= (m_dataList.Count / 2))
            {
                while (true)
                {
                    int r = RandomUtil.getRandom(0, m_dataList.Count - 1);
                    if (!m_dataList[r].m_isUsed)
                    {
                        uid = m_dataList[r].m_uid;

                        RoomData room = GameUtil.getRoomByUid(uid);
                        if (room == null)
                        {
                            m_dataList[r].m_isUsed = true;
                            
                            {
                                int num = 0;
                                for (int i = 0; i < m_dataList.Count; i++)
                                {
                                    if (!m_dataList[i].m_isUsed)
                                    {
                                        ++num;
                                    }
                                }
                                TLJ_PlayService.PlayService.log.Error("AIDataScript.getOneAI()----借出去机器人：" + uid + "    剩余机器人：" + num);
                            }

                            return uid;
                        }
                        else
                        {
                            TLJ_PlayService.PlayService.log.Error("AIDataScript.getOneAI()----该机器人没有被使用，却在房间中：" + room.getRoomId() + "   " + uid);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_dataList.Count; i++)
                {
                    if (!m_dataList[i].m_isUsed)
                    {
                        uid = m_dataList[i].m_uid;

                        RoomData room = GameUtil.getRoomByUid(uid);
                        if (room == null)
                        {
                            m_dataList[i].m_isUsed = true;
                            
                            {
                                int num = 0;
                                for (int j = 0; j < m_dataList.Count; j++)
                                {
                                    if (!m_dataList[j].m_isUsed)
                                    {
                                        ++num;
                                    }
                                }
                                TLJ_PlayService.PlayService.log.Error("AIDataScript.getOneAI()----借出去机器人：" + uid + "    剩余机器人：" + num);
                            }

                            return uid;
                        }
                        else
                        {
                            TLJ_PlayService.PlayService.log.Error("AIDataScript.getOneAI()----该机器人没有被使用，却在房间中：" + room.getRoomId() + "   " + uid);
                        }
                    }
                }
            }

            return uid;
        }
    }

    public void backOneAI(string uid)
    {
        lock (m_dataList)
        {
            for (int i = 0; i < m_dataList.Count; i++)
            {
                if (m_dataList[i].m_uid.CompareTo(uid) == 0)
                {
                    m_dataList[i].m_isUsed = false;
                    
                    {
                        int num = 0;
                        for (int j = 0; j < m_dataList.Count; j++)
                        {
                            if (!m_dataList[j].m_isUsed)
                            {
                                ++num;
                            }
                        }
                        TLJ_PlayService.PlayService.log.Error("AIDataScript.getOneAI()----还回来机器人：" + uid + "    剩余机器人：" + num);
                    }

                    break;
                }
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