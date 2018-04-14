using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class DDZ_GameBase
{
    public abstract List<DDZ_RoomData> getRoomList();
    public abstract void gameOver(DDZ_RoomData room);
    public abstract bool doTaskPlayerCloseConn(string uid);
    public abstract string getTag();
}
