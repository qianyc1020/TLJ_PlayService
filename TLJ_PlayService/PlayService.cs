using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using log4net;

namespace TLJ_PlayService
{
    public partial class PlayService : ServiceBase
    {
        public static HPServerUtil m_serverUtil;
        public static MySqlServerUtil m_mySqlServerUtil;
        public static string TAG = "PlayService";
        public static ILog log;
        public PlayService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            LogUtil.getInstance().start("C:\\Users\\Administrator\\Desktop\\TLJ_PlayServiceLog");
            log4net.Config.XmlConfigurator.Configure();
            log = LogManager.GetLogger(TAG);
            log.Info("日志开启");
            if (!NetConfig.init())
            {
                return;
            }

            m_serverUtil = new HPServerUtil();
            m_mySqlServerUtil = new MySqlServerUtil();

            LogUtil.getInstance().addDebugLog("服务开启");

            // 开启TCP服务组件与客户端通信
            m_serverUtil.startTCPService();

            // 开启TCP客户端组件与数据库服务器通信
            m_mySqlServerUtil.start();
        }

        protected override void OnStop()
        {
            m_serverUtil.stopTCPService();
            m_mySqlServerUtil.stop();

            LogUtil.getInstance().writeLogToLocalNow("服务关闭");
        }
    }
}
