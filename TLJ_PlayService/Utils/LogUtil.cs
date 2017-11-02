using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class LogUtil
{
    static LogUtil s_logUtil = null;

    static string m_rootFolderPath = "C:/";
    Timer m_timer;
    bool m_isStartRecordLog = false;

    static bool m_canWriteDebugLog = true;
    static bool m_canWriteErrorLog = true;

    static List<string> m_waitRecoedDebugLogList = new List<string>();
    static List<string> m_waitRecoedErrorLogList = new List<string>();

    public static LogUtil getInstance()
    {
        if (s_logUtil == null)
        {
            s_logUtil = new LogUtil();
        }

        return s_logUtil;
    }

    public void start(string rootFolderPath)
    {
        if (!m_isStartRecordLog)
        {
            m_isStartRecordLog = true;

            // 检测、创建Log所在文件夹
            {
                m_rootFolderPath = rootFolderPath;

                if (!Directory.Exists(m_rootFolderPath))
                {
                    Directory.CreateDirectory(m_rootFolderPath);
                }
            }

            //Thread t = new Thread(checkLogList);
            //t.Start();

            Task t = new Task(() => { checkLogList(); });
            t.Start();

            Debug.WriteLine("日志记录开启成功");
        }
        else
        {
            Debug.WriteLine("日志记录开启失败");
        }
    }

    public void stop()
    {
        if (m_isStartRecordLog)
        {
            m_isStartRecordLog = false;
            m_timer.Dispose();

            Debug.WriteLine("日志记录关闭成功");
        }
        else
        {
            Debug.WriteLine("日志记录关闭失败");
        }
    }

    // 添加Debug日志，\r\n为换行
    public void addDebugLog(string data)
    {
        if (!m_isStartRecordLog)
        {
            Debug.WriteLine("插入日志失败，日志记录未开启");

            return;
        }

        // 把字符串中的换行符去掉
        data = data.Replace("\r\n", "");

        lock (m_waitRecoedDebugLogList)
        {
            m_waitRecoedDebugLogList.Add(getCurTime() + "----" + data);
        }
    }

    // 添加Error日志，\r\n为换行
    public void addErrorLog(string data)
    {
        if (!m_isStartRecordLog)
        {
            Debug.WriteLine("插入日志失败，日志记录未开启");

            return;
        }

        // 把字符串中的换行符去掉
        data = data.Replace("\r\n", "");

        lock (m_waitRecoedErrorLogList)
        {
            m_waitRecoedErrorLogList.Add(getCurTime() + "----" + data);
        }
    }

    // 立刻写到日志文本里面，不要常用
    public void writeLogToLocalNow(string data)
    {
        if (!m_canWriteDebugLog)
        {
            return;
        }

        // 把字符串中的换行符去掉
        data = data.Replace("\r\n", "");

        data = getCurTime() + "----" + data;

        m_canWriteDebugLog = false;

        StreamWriter sw = null;
        try
        {
            string path = m_rootFolderPath + "/" + getCurYearMonthDay() + "-DebugLog.txt";
            if (!File.Exists(path))
            {
                File.Create(path).Close();
            }

            sw = new StreamWriter(path, true);

            sw.WriteLine(data);

            //清空缓冲区
            sw.Flush();

            //关闭流
            sw.Close();

            m_canWriteDebugLog = true;
        }
        catch (IOException e)
        {
            Console.Write(e);
        }
        finally
        {
            sw.Close();
        }
    }

    void checkLogList()
    {
        m_timer = new Timer(onTimer, "", 100, 10);
    }

    static void onTimer(object data)
    {
        if (m_waitRecoedDebugLogList.Count > 0)
        {
            writeDebLogToLocal();
        }

        if (m_waitRecoedErrorLogList.Count > 0)
        {
            writeErrorLogToLocal();
        }
    }

    static void writeDebLogToLocal()
    {
        if (!m_canWriteDebugLog)
        {
            return;
        }

        m_canWriteDebugLog = false;

        StreamWriter sw = null;
        try
        {
            string path = m_rootFolderPath + "/" + getCurYearMonthDay() + "-DebugLog.txt";
            if (!File.Exists(path))
            {
                File.Create(path).Close();
            }

            sw = new StreamWriter(path, true);

            //开始写入
            while (m_waitRecoedDebugLogList.Count > 0)
            {
                sw.WriteLine(m_waitRecoedDebugLogList[0]);
                m_waitRecoedDebugLogList.RemoveAt(0);
            }
            //for (int i = 0; i < m_waitRecoedDebugLogList.Count; i++)
            //{
            //    sw.WriteLine(m_waitRecoedDebugLogList[i]);
            //    m_waitRecoedDebugLogList.RemoveAt(i);
            //}

            //清空缓冲区
            sw.Flush();

            //关闭流
            sw.Close();

            m_canWriteDebugLog = true;
        }
        catch (IOException e)
        {
            Console.Write(e);
        }
        finally
        {
            sw.Close();
        }
    }

    static void writeErrorLogToLocal()
    {
        if (!m_canWriteErrorLog)
        {
            return;
        }

        m_canWriteErrorLog = false;

        StreamWriter sw = null;
        try
        {
            string path = m_rootFolderPath + "/" + getCurYearMonthDay() + "-ErrorLog.txt";
            if (!File.Exists(path))
            {
                File.Create(path).Close();
            }

            sw = new StreamWriter(path, true);

            //开始写入
            while (m_waitRecoedErrorLogList.Count > 0)
            {
                sw.WriteLine(m_waitRecoedErrorLogList[0]);
                m_waitRecoedErrorLogList.RemoveAt(0);
            }
            //for (int i = 0; i < m_waitRecoedErrorLogList.Count; i++)
            //{
            //    sw.WriteLine(m_waitRecoedErrorLogList[i]);
            //    m_waitRecoedErrorLogList.RemoveAt(i);
            //}

            //清空缓冲区
            sw.Flush();

            //关闭流
            sw.Close();

            m_canWriteErrorLog = true;
        }
        catch (IOException e)
        {
            Console.Write(e);
        }
        finally
        {
            sw.Close();
        }
    }

    // 格式2017/7/12 15:05:03
    static string getCurTime()
    {
        return DateTime.Now.ToString();
    }

    // 格式2017-7-1
    static string getCurYearMonthDay()
    {
        return DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day;
    }
}
