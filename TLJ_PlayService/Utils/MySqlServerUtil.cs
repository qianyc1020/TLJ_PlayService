using HPSocketCS;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        //Thread thread = new Thread(connectinInThread);
        //thread.Start();

        Task t = new Task(() => { connectinInThread(); });
        t.Start();
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
        
        // 数据清空
        {
            PVPGameRoomDataScript.clear();
        }

        {
            // 拉取机器人列表
            Request_GetAIList.doRequest();
        }

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
        try
        {
            string str = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            LogUtil.getInstance().addDebugLog("收到数据库服务器消息:" + str);

            JObject jo = JObject.Parse(str);
            string tag = jo.GetValue("tag").ToString();

            // 获取游戏内玩家信息
            if (tag.CompareTo(TLJCommon.Consts.Tag_UserInfo_Game) == 0)
            {
                int connId = Convert.ToInt32(jo.GetValue("connId"));
                int isClientReq = (int)jo.GetValue("isClientReq");

                // 客户端请求的
                if (isClientReq == 1)
                {
                    NetRespond_UserInfo_Game.onMySqlRespond(connId, str);
                }
                // 服务端请求的
                else
                {
                    Request_UserInfo_Game.onMySqlRespond(str);
                }
            }
            // 获取pvp场次信息
            else if (tag.CompareTo(TLJCommon.Consts.Tag_GetPVPGameRoom) == 0)
            {
                int connId = Convert.ToInt32(jo.GetValue("connId"));
                NetRespond_GetPVPGameRoom.onMySqlRespond(connId, str);
            }
            // 拉取机器人列表
            else if (tag.CompareTo(TLJCommon.Consts.Tag_GetAIList) == 0)
            {
                Request_GetAIList.onMySqlRespond(str);
            }
            // 使用buff
            else if (tag.CompareTo(TLJCommon.Consts.Tag_UseBuff) == 0)
            {
                int connId = Convert.ToInt32(jo.GetValue("connId"));
                NetRespond_UseBuff.onMySqlRespond(connId, str);
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("MySqlServerUtil.OnReceive----异常：" + ex.Message);
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
