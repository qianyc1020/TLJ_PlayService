using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class AIDataManage
{
    static int s_allAICount = 0;

    public static int getOneAIIndex()
    {
        ++s_allAICount;

        if (s_allAICount == 99999)
        {
            s_allAICount = 0;
        }

        return s_allAICount;
    }
}