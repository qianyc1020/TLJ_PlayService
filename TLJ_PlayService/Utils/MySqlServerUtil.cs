using HPSocketCS;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

public class MySqlServerUtil
{
    TcpPackClient m_tcpClient = new TcpPackClient();

    bool m_isConnecting = false;

    public MySqlServerUtil()
    {
        // 设置client事件
        m_tcpClient.OnPrepareConnect += new TcpClientEvent.OnPrepareConnectEventHandler(OnPrepareConnect);
        m_tcpClient.OnConnect += new TcpClientEvent.OnConnectEventHandler(OnConnect);
        m_tcpClient.OnSend += new TcpClientEvent.OnSendEventHandler(OnSend);
        m_tcpClient.OnReceive += new TcpClientEvent.OnReceiveEventHandler(OnReceive);
        m_tcpClient.OnClose += new TcpClientEvent.OnCloseEventHandler(OnClose);

        // 设置包头标识,与对端设置保证一致性
        m_tcpClient.PackHeaderFlag = 0xff;
        // 设置最大封包大小
        m_tcpClient.MaxPackSize = 0x1000;
    }

    public void start()
    {
        Thread thread = new Thread(connectinInThread);
        thread.Start();
    }

    void connectinInThread()
    {
        while (!m_tcpClient.Connect(NetConfig.s_mySqlService_ip, (ushort)NetConfig.s_mySqlService_port, false))
        {
            // 连接数据库服务器失败的话会一直尝试连接
            LogUtil.getInstance().addDebugLog("连接数据库服务器失败");
            Thread.Sleep(1000);
        }

        m_isConnecting = true;
        LogUtil.getInstance().addDebugLog("连接数据库服务器成功");

        return;
    }

    public void stop()
    {
        if (m_tcpClient.Stop())
        {
            LogUtil.getInstance().writeLogToLocalNow("与数据库服务器断开成功");
            m_isConnecting = false;
        }
        else
        {
            LogUtil.getInstance().writeLogToLocalNow("与数据库服务器断开失败");
        }
    }

    public bool sendMseeage(string str)
    {
        if (!m_isConnecting)
        {
            return false;
        }

        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);

            // 发送
            if (m_tcpClient.Send(bytes, bytes.Length))
            {
                LogUtil.getInstance().addDebugLog("发送给数据库服务器成功:" + str);
                return true;
            }
            else
            {
                LogUtil.getInstance().addDebugLog("发送给数据库服务器失败:" + str);
                return false;
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addDebugLog("发送给数据库服务器异常:" + ex.Message);
            return false;
        }
    }

    HandleResult OnPrepareConnect(TcpClient sender, IntPtr socket)
    {
        return HandleResult.Ok;
    }

    HandleResult OnConnect(TcpClient sender)
    {
        return HandleResult.Ok;
    }

    HandleResult OnSend(TcpClient sender, byte[] bytes)
    {
        //string str = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        //LogUtil.getInstance().addDebugLog("发送给数据库服务器:" + str);

        return HandleResult.Ok;
    }

    HandleResult OnReceive(TcpClient sender, byte[] bytes)
    {
        string str = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        LogUtil.getInstance().addDebugLog("收到数据库服务器消息:" + str);

        JObject jo = JObject.Parse(str);
        string tag = jo.GetValue("tag").ToString();
        int connId = Convert.ToInt32(jo.GetValue("connId"));

        // 请求登录接口
        if (tag.CompareTo(TLJCommon.Consts.Tag_Login) == 0)
        {
            NetRespond_Login.onMySqlRespond(connId, str);
        }
        // 请求快速注册接口
        else if (tag.CompareTo(TLJCommon.Consts.Tag_QuickRegister) == 0)
        {
            NetRespond_QuickRegister.onMySqlRespond(connId,str);
        }


        return HandleResult.Ok;
    }

    HandleResult OnClose(TcpClient sender, SocketOperation enOperation, int errorCode)
    {
        LogUtil.getInstance().addDebugLog("与数据库服务器断开");

        m_isConnecting = false;

        // 尝试重新连接数据库服务器
        start();

        return HandleResult.Ok;
    }
}
