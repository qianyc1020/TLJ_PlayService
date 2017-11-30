using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class OtherConfig
{
    // 日志路径
    public static string s_logPath;

    public static bool init()
    {
        try
        {
            // 读取文件
            {
                StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "OtherConfig.json");
                string str = sr.ReadToEnd().ToString();
                sr.Close();

                JObject jo = JObject.Parse(str);

                // 日志路径
                s_logPath = jo.GetValue("LogPath").ToString();
            }

            return true;
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().writeLogToLocalNow("读取OtherConfig文件出错：" + ex.Message);

            return false;
        }
    }
}
