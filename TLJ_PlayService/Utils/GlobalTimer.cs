using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TLJ_PlayService
{
    class GlobalTimer
    {
        static GlobalTimer s_instance = null;

        Timer m_timer;

        public static GlobalTimer getInstance()
        {
            if (s_instance == null)
            {
                s_instance = new GlobalTimer();
            }

            return s_instance;
        }

        public void start()
        {
            m_timer = new Timer(onTimer, "", 1000, 1000);
        }

        static void onTimer(object data)
        {
            int year = CommonUtil.getCurYear();
            int month = CommonUtil.getCurMonth();
            int day = CommonUtil.getCurDay();
            int hour = CommonUtil.getCurHour();
            int min = CommonUtil.getCurMinute();
            int sec = CommonUtil.getCurSecond();

            if ((hour == 19) || (hour == 20) || (hour == 21) || (hour == 22))
            {
                if ((min == 0) && (sec == 0))
                {
                    PlayLogic_Relax.getInstance().sendBaoXiang();
                    LogUtil.getInstance().addDebugLog("GlobalTimer：掉落宝箱------" + hour + ":" + min + ":" + sec);
                }
            }
            
            if ((day == 1) && (hour == 0) && (min == 0) && (sec == 0))
            {
                Request_RefreshAllData.doRequest();
                LogUtil.getInstance().addDebugLog("GlobalTimer：到月初了，刷新配置表");
            }
        }
    }
}
