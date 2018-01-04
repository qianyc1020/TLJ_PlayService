using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class RoomManager
{
    static Obj_RoomID s_obj_RoomID = new Obj_RoomID();

    public static int getOneRoomID()
    {
        lock (s_obj_RoomID)
        {
            ++s_obj_RoomID.m_roomID;

            return s_obj_RoomID.m_roomID;
        }
    }
}

class Obj_RoomID
{
    public int m_roomID = 0;
}