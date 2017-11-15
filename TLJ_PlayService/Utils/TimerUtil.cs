using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class TimerUtil
{
    public delegate void TimerCallback(object obj);
    TimerCallback m_timerCallback = null;

    Timer m_timer = null;
    
    public void startTimer(int time, TimerType timerType)
    {
        // 如果有之前的定时事件还没结束，则先把前面的强制结束
        // 如果不强制结束，则会多次回调，比如连续调用3次，就会执行3次“TimerMethod”
        if (m_timer != null)
        {
            stopTimer();
        }

        m_timer = new Timer(TimerMethod, timerType, time, 0);
    }

    public void stopTimer()
    {
        if (m_timer != null)
        {
            m_timer.Dispose();
        }
    }

    public void setTimerCallBack(TimerCallback timeCallBack)
    {
        m_timerCallback = timeCallBack;
    }

    void TimerMethod(object obj)
    {
        m_timer = null;

        if (m_timerCallback != null)
        {
            m_timerCallback(obj);
        }
    }
}
