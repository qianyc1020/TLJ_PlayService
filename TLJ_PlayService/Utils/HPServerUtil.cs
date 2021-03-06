﻿using HPSocketCS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class HPServerUtil
{
    //TcpPackServer m_tcpServer = new TcpPackServer();
    TcpServer m_tcpServer = new TcpServer();

    // 数据包尾部标识
    char m_packEndFlag = (char)1;
    string m_endStr = "";

    int m_curPlayerCount = 0;

    public HPServerUtil()
    {
        // 设置服务器事件
        m_tcpServer.OnPrepareListen += new TcpServerEvent.OnPrepareListenEventHandler(OnPrepareListen);
        m_tcpServer.OnAccept += new TcpServerEvent.OnAcceptEventHandler(OnAccept);
        m_tcpServer.OnSend += new TcpServerEvent.OnSendEventHandler(OnSend);
        // 两个数据到达事件的一种
        //server.OnPointerDataReceive += new TcpServerEvent.OnPointerDataReceiveEventHandler(OnPointerDataReceive);
        // 两个数据到达事件的一种
        m_tcpServer.OnReceive += new TcpServerEvent.OnReceiveEventHandler(OnReceive);

        m_tcpServer.OnClose += new TcpServerEvent.OnCloseEventHandler(OnClose);
        m_tcpServer.OnShutdown += new TcpServerEvent.OnShutdownEventHandler(OnShutdown);

        //// 设置包头标识,与对端设置保证一致性
        //m_tcpServer.PackHeaderFlag = 0xff;
        //// 设置最大封包大小
        //m_tcpServer.MaxPackSize = 0x1000;
    }

    public int getOnlinePlayerCount()
    {
        //return PlayLogic_Relax.getInstance().getPlayerCount();
        return m_curPlayerCount;
    }

    public int getRoomCount()
    {
        return PlayLogic_Relax.getInstance().getRoomCount();
    }

    // 启动服务
    public void startTCPService()
    {
        try
        {
            //m_tcpServer.IpAddress = NetConfig.s_playService_ip;
            m_tcpServer.IpAddress = "0.0.0.0";
            m_tcpServer.Port = (ushort)NetConfig.s_playService_port;

            // 启动服务
            if (m_tcpServer.Start())
            {
                LogUtil.getInstance().addDebugLog("TCP服务启动成功");

            }
            else
            {
                LogUtil.getInstance().addDebugLog("TCP服务启动失败");
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addDebugLog("TCP服务启动异常:" + ex.Message);
        }
    }

    // 停止服务
    public void stopTCPService()
    {
        if (m_tcpServer.Stop())
        {
            LogUtil.getInstance().writeLogToLocalNow("TCP服务停止成功");
        }
        else
        {
            LogUtil.getInstance().writeLogToLocalNow("TCP服务停止失败");
        }
    }

    // 发送消息
    public void sendMessage(IntPtr connId, string text)
    {
        // 增加数据包尾部标识
        //        byte[] bytes = new byte[1024];
        //        bytes = Encoding.UTF8.GetBytes(text + m_packEndFlag);
        SendData(connId, text);

        //        byte[] data = SendData(text);
        //        if (m_tcpServer.Send(connId, data, data.Length))
        //        {
        //            LogUtil.getInstance().addDebugLog("发送消息给客户端：" + text);
        //        }
        //        else
        //        {
        //            Debug.WriteLine("发送给客户端失败:" + text);
        //        }
    }

    private void SendData(IntPtr connId, string data)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        byte[] len = BitConverter.GetBytes((ushort)bytes.Length);
        byte[] flag = BitConverter.GetBytes(m_packEndFlag);
        byte[] sendBytes = CombineBytes(flag, CombineBytes(len, bytes));

        //LogUtil.getInstance().addDebugLog($"发送数据长度：{sendBytes.Length},body:{bytes.Length},flag:{flag.Length},data:{data}");

        if (m_tcpServer.Send(connId, sendBytes, sendBytes.Length))
        {
            // 日志
            {
                PlayerData playerData = GameUtil.getPlayerDataByConnId(connId);
                if (playerData != null)
                {
                    RoomData room = GameUtil.getRoomByUid(playerData.m_uid);
                    if (room != null)
                    {
                        LogUtil.getInstance().writeRoomLog(room, "发送消息给客户端：" + data);
                    }
                    else
                    {
                        LogUtil.getInstance().addDebugLog("发送消息给客户端：" + data);
                    }
                }
                else
                {
                    DDZ_PlayerData ddz_playerData = DDZ_GameUtil.getPlayerDataByConnId(connId);
                    if (ddz_playerData != null)
                    {
                        DDZ_RoomData room = DDZ_GameUtil.getRoomByUid(ddz_playerData.m_uid);
                        if (room != null)
                        {
                            LogUtil.getInstance().writeRoomLog(room, "发送消息给客户端：" + data);
                        }
                        else
                        {
                            LogUtil.getInstance().addDebugLog("发送消息给客户端：" + data);
                        }
                    }
                    else
                    {
                        LogUtil.getInstance().addDebugLog("发送消息给客户端：" + data);
                    }
                }
            }
        }
        else
        {
            Debug.WriteLine("发送给客户端失败:" + data);
        }
    }

    private byte[] CombineBytes(byte[] data1, byte[] data2)
    {
        byte[] data = new byte[data1.Length + data2.Length];
        Buffer.BlockCopy(data1, 0, data, 0, data1.Length); //这种方法仅适用于字节数组
        Buffer.BlockCopy(data2, 0, data, data1.Length, data2.Length);
        return data;
    }

    //// 发送消息
    //public void sendMessage(IntPtr connId, string text)
    //{
    //    // 增加数据包尾部标识
    //    byte[] bytes = new byte[1024];
    //    bytes = Encoding.UTF8.GetBytes(text + m_packEndFlag);

    //    if (m_tcpServer.Send(connId, bytes, bytes.Length))
    //    {
    //        // 日志
    //        {
    //            PlayerData playerData = GameUtil.getPlayerDataByConnId(connId);
    //            if (playerData != null)
    //            {
    //                RoomData room = GameUtil.getRoomByUid(playerData.m_uid);
    //                if (room != null)
    //                {
    //                    LogUtil.getInstance().writeRoomLog(room, "发送消息给客户端：" + text);
    //                }
    //                else
    //                {
    //                    LogUtil.getInstance().addDebugLog("发送消息给客户端：" + text);
    //                }
    //            }
    //            else
    //            {
    //                LogUtil.getInstance().addDebugLog("发送消息给客户端：" + text);
    //            }
    //        }
    //    }
    //    else
    //    {
    //        Debug.WriteLine("发送给客户端失败:" + text);
    //    }
    //}

    HandleResult OnPrepareListen(IntPtr soListen)
    {
        return HandleResult.Ok;
    }

    // 客户进入了
    HandleResult OnAccept(IntPtr connId, IntPtr pClient)
    {
        ++m_curPlayerCount;

        // 获取客户端ip和端口
        string ip = string.Empty;
        ushort port = 0;
        if (m_tcpServer.GetRemoteAddress(connId, ref ip, ref port))
        {
            LogUtil.getInstance().addDebugLog("有客户端连接--connId=" + (int)connId + "--ip=" + ip.ToString() + "--port=" + port);
        }
        else
        {
            LogUtil.getInstance().addDebugLog("获取客户端ip地址出错");
        }

        return HandleResult.Ok;
    }

    HandleResult OnSend(IntPtr connId, byte[] bytes)
    {
        //string text = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

        //LogUtil.getInstance().addDebugLog("发送消息给客户端：" + text);

        return HandleResult.Ok;
    }


    //HandleResult OnPointerDataReceive(IntPtr connId, IntPtr pData, int length)
    //{
    //    // 数据到达了
    //    try
    //    {
    //        if (m_tcpServer.Send(connId, pData, length))
    //        {
    //            return HandleResult.Ok;
    //        }

    //        return HandleResult.Error;
    //    }
    //    catch (Exception)
    //    {
    //        return HandleResult.Ignore;
    //    }
    //}

    HandleResult OnReceive(IntPtr connId, byte[] bytes)
    {
        try
        {
            string text = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

            // 日志
            {
                PlayerData playerData = GameUtil.getPlayerDataByConnId(connId);
                if (playerData != null)
                {
                    RoomData room = GameUtil.getRoomByUid(playerData.m_uid);
                    if (room != null)
                    {
                        LogUtil.getInstance().writeRoomLog(room, "收到客户端原始消息：" + text);
                    }
                    else
                    {
                        LogUtil.getInstance().addDebugLog("收到客户端原始消息：" + text);
                    }
                }
                else
                {
                    LogUtil.getInstance().addDebugLog("收到客户端原始消息：" + text);
                }
            }

            {
                text = m_endStr + text;
                text = text.Replace("\r\n", "");

                List<string> list = new List<string>();
                bool b = CommonUtil.splitStrIsPerfect(text, list, m_packEndFlag);

                if (b)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        ReceiveObj obj = new ReceiveObj(connId, list[i]);
                        //Thread thread = new Thread(doAskCilentReq);
                        //thread.Start(obj);

                        Task t = new Task(() => { doAskCilentReq(obj); });
                        t.Start();
                    }

                    //text = "";
                    m_endStr = "";
                }
                else
                {
                    for (int i = 0; i < list.Count - 1; i++)
                    {
                        ReceiveObj obj = new ReceiveObj(connId, list[i]);
                        //Thread thread = new Thread(doAskCilentReq);
                        //thread.Start(obj);

                        Task t = new Task(() => { doAskCilentReq(obj); });
                        t.Start();
                    }

                    m_endStr = list[list.Count - 1];
                }
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error("OnReceive:" + ex.Message);
        }

        return HandleResult.Ok;
    }

    HandleResult OnClose(IntPtr connId, SocketOperation enOperation, int errorCode)
    {
        --m_curPlayerCount;

        // 日志
        {
            PlayerData playerData = GameUtil.getPlayerDataByConnId(connId);
            if (playerData != null)
            {
                RoomData room = GameUtil.getRoomByUid(playerData.m_uid);
                if (room != null)
                {
                    // 游戏在线统计
                    Request_OnlineStatistics.doRequest(playerData.m_uid, room.getRoomId(), room.m_gameRoomType, playerData.m_isAI, (int)Request_OnlineStatistics.OnlineStatisticsType.OnlineStatisticsType_exit);

                    LogUtil.getInstance().writeRoomLog(room, "与客户端断开:" + connId);
                }
                else
                {
                    LogUtil.getInstance().addDebugLog("与客户端断开:" + connId);
                }
            }
            else
            {
                LogUtil.getInstance().addDebugLog("与客户端断开:" + connId);
            }
        }

        //Thread thread = new Thread(hasPlayerExit);
        //thread.Start(connId);

        Task t = new Task(() => { hasPlayerExit(connId); });
        t.Start();

        return HandleResult.Ok;
    }

    // 服务关闭
    HandleResult OnShutdown()
    {
        //调用关闭的地方已经记录日志了，这里不需要重复记录
        //Debug.WriteLine("OnShutdown");
        //LogUtil.getInstance().addDebugLog("TCP服务关闭成功");

        return HandleResult.Ok;
    }

    // 处理客户端的请求
    void doAskCilentReq(object obj)
    {
        ReceiveObj receiveObj = (ReceiveObj)obj;
        string text = receiveObj.m_data;
        
        // 日志
        {
            PlayerData playerData = GameUtil.getPlayerDataByConnId(receiveObj.m_connId);
            if (playerData != null)
            {
                RoomData room = GameUtil.getRoomByUid(playerData.m_uid);
                if (room != null)
                {
                    LogUtil.getInstance().writeRoomLog(room, "收到客户端消息：" + text);
                }
                else
                {
                    LogUtil.getInstance().addDebugLog("收到客户端消息：" + text);
                }
            }
            else
            {
                LogUtil.getInstance().addDebugLog("收到客户端消息：" + text);
            }
        }

        JObject jo;
        try
        {
            jo = JObject.Parse(text);
        }
        catch (JsonReaderException ex)
        {
            // 传过来的数据不是json格式的，一律不理
            // 日志
            {
                PlayerData playerData = GameUtil.getPlayerDataByConnId(receiveObj.m_connId);
                if (playerData != null)
                {
                    RoomData room = GameUtil.getRoomByUid(playerData.m_uid);
                    if (room != null)
                    {
                        LogUtil.getInstance().writeRoomLog(room, "客户端传来非json数据：" + text);
                    }
                    else
                    {
                        LogUtil.getInstance().addDebugLog("客户端传来非json数据：" + text);
                    }
                }
                else
                {
                    LogUtil.getInstance().addDebugLog("客户端传来非json数据：" + text);
                }
            }

            m_endStr = "";

            return;
        }

        if (jo.GetValue("tag") != null)
        {
            string tag = jo.GetValue("tag").ToString();

            // 休闲场相关
            if (tag.CompareTo(TLJCommon.Consts.Tag_XiuXianChang) == 0)
            {
                PlayLogic_Relax.getInstance().OnReceive(receiveObj.m_connId, text);
            }
            // 比赛场相关
            else if (tag.CompareTo(TLJCommon.Consts.Tag_JingJiChang) == 0)
            {
                PlayLogic_PVP.getInstance().OnReceive(receiveObj.m_connId, text);
            }
            // 获取pvp场次信息
            else if (tag.CompareTo(TLJCommon.Consts.Tag_GetPVPGameRoom) == 0)
            {
                NetRespond_GetPVPGameRoom.doAskCilentReq_GetPVPGameRoom(receiveObj.m_connId, text);
            }
            // 请求服务器在线玩家信息接口
            else if (tag.CompareTo(TLJCommon.Consts.Tag_OnlineInfo) == 0)
            {
                NetRespond_OnlineInfo.doAskCilentReq_OnlineInfo(receiveObj.m_connId, text);
            }
            // 使用buff
            else if (tag.CompareTo(TLJCommon.Consts.Tag_UseBuff) == 0)
            {
                NetRespond_UseBuff.doAskCilentReq_UseBuff(receiveObj.m_connId, text);
            }
            // 是否已经加入游戏
            else if (tag.CompareTo(TLJCommon.Consts.Tag_IsJoinGame) == 0)
            {
                NetRespond_IsJoinGame.doAskCilentReq_IsJoinGame(receiveObj.m_connId, text);
            }
            // 请求恢复房间
            else if (tag.CompareTo(TLJCommon.Consts.Tag_RetryJoinGame) == 0)
            {
                NetRespond_RetryJoinGame.doAskCilentReq_RetryJoinGame(receiveObj.m_connId, text);
            }
            // 自定义牌型
            else if (tag.CompareTo(TLJCommon.Consts.Tag_DebugSetPoker) == 0)
            {
                NetRespond_DebugSetPoker.doAskCilentReq_DebugSetPoker(receiveObj.m_connId, text);
            }
            // 心跳
            else if (tag.CompareTo(TLJCommon.Consts.Tag_HeartBeat_Play) == 0)
            {
                NetRespond_HeartBeat_Play.doAskCilentReq_HeartBeat_Play(receiveObj.m_connId, text);
            }
            // 解散房间
            else if (tag.CompareTo(TLJCommon.Consts.Tag_BreakRoom) == 0)
            {
                NetRespond_BreakRoom.doAskCilentReq_BreakRoom(receiveObj.m_connId, text);
            }
            // 斗地主相关
            else if (tag.CompareTo(TLJCommon.Consts.Tag_DouDiZhu_Game) == 0)
            {
                PlayLogic_DDZ.getInstance().OnReceive(receiveObj.m_connId, text);
            }
            // 未知Tag
            else
            {
                LogUtil.getInstance().addDebugLog("未知Tag：" + tag);
            }
        }
        else
        {
            // 传过来的数据没有tag字段的，一律不理
            // 日志
            {
                PlayerData playerData = GameUtil.getPlayerDataByConnId(receiveObj.m_connId);
                if (playerData != null)
                {
                    RoomData room = GameUtil.getRoomByUid(playerData.m_uid);
                    if (room != null)
                    {
                        LogUtil.getInstance().writeRoomLog(room, "客户端传来的数据没有Tag：" + text);
                    }
                    else
                    {
                        LogUtil.getInstance().addDebugLog("客户端传来的数据没有Tag：" + text);
                    }
                }
                else
                {
                    LogUtil.getInstance().addDebugLog("客户端传来的数据没有Tag：" + text);
                }
            }
            return;
        }
    }

    void hasPlayerExit(object obj)
    {
        IntPtr connId = (IntPtr)obj;

        PlayerData playerData = GameUtil.getPlayerDataByConnId(connId);
        if (playerData == null)
        {
            LogUtil.getInstance().addDebugLog("踢出玩家失败，找不到IntPtr：" + connId + "对应的玩家");
            return;
        }

        if (PlayLogic_Relax.getInstance().doTaskPlayerCloseConn(playerData.m_uid))
        {
            // 日志
            {
                if (playerData != null)
                {
                    RoomData room = GameUtil.getRoomByUid(playerData.m_uid);
                    if (room != null)
                    {
                        LogUtil.getInstance().writeRoomLog(room, "踢出玩家成功：" + connId);
                    }
                    else
                    {
                        LogUtil.getInstance().addDebugLog("踢出玩家成功：" + connId);
                    }
                }
                else
                {
                    LogUtil.getInstance().addDebugLog("踢出玩家成功：" + connId);
                }
            }
        }
        else if (PlayLogic_PVP.getInstance().doTaskPlayerCloseConn(playerData.m_uid))
        {
            // 日志
            {
                if (playerData != null)
                {
                    RoomData room = GameUtil.getRoomByUid(playerData.m_uid);
                    if (room != null)
                    {
                        LogUtil.getInstance().writeRoomLog(room, "踢出玩家成功：" + connId);
                    }
                    else
                    {
                        LogUtil.getInstance().addDebugLog("踢出玩家成功：" + connId);
                    }
                }
                else
                {
                    LogUtil.getInstance().addDebugLog("踢出玩家成功：" + connId);
                }
            }
        }
        else
        {
            LogUtil.getInstance().addDebugLog("踢出玩家失败，找不到该玩家：" + connId);
        }
    }
}

class ReceiveObj
{
    public IntPtr m_connId;
    public string m_data = "";

    public ReceiveObj(IntPtr connId, string data)
    {
        m_connId = connId;
        m_data = data;
    }
};
