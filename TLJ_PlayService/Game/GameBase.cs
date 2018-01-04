using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class GameBase
{
    public abstract List<RoomData> getRoomList();
    public abstract void gameOver(RoomData room);
    public abstract bool doTaskPlayerCloseConn(string uid);
    public abstract string getTag();
}
