using System.Collections.Generic;
using System.Linq;
using System.Net;
using Lidgren.Network;
using UnityEngine;
using Utility;

public enum MoveDirs
{
    UP,
    UPRIGHT,
    RIGHT,
    RIGHTDOWN,
    DOWN,
    DOWNLEFT,
    LEFT,
    LEFTUP,
    NONE
}

public enum AnimationStates
{
    IDLE,
    WALK,
    ATTACK
}

public enum WeaponStates
{
    none,
    MELEE,
    BOW,
    UTILITY
}

public class ClientData
{
    public ClientData(NetConnection connection, short id)
    {
        Connection = connection;
        ID = id;
        ResetData();
    }

    public ClientData(short id)
    {
        ID = id;
        ResetData();
    }

    public short ID { get; private set; }
    public string UserName { get; set; }
    public NetConnection Connection { get; protected set; }

    public Vector3 Position { get; set; }
    public Vector3 LastPosition { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 LastRoation { get; set; }

    public float CurrentHitpoints { get; set; }
    public float MaxHitpoints { get; set; }
    public float Speed { get; set; }

    public float DeathTime { get; set; }
    public int Team { get; set; }

    public AnimationStates AnimationState { get; set; }
    public MoveDirs MoveDir { get; set; }

    public float moveTime { get; set; }

    public WeaponStates WeaponState { get; set; }

    public bool IsDead { get; set; }
    public bool IsAiming { get; set; }
    public bool canAttack { get; set; }
    public bool canJump { get; set; }

    public bool WantsPredict { get; set; }


    public static short GetFreeID(IDictionary<IPEndPoint, ClientData> clients)
    {
        List<short> usedIds = (from client in clients select client.Value.ID).ToList();
        if (usedIds.Count == 0) return 0;

        for (short id = 0; id <= usedIds.Count; id++)
            if (!usedIds.Contains(id))
                return id;
        return -1;
    }

    public bool HasTeam()
    {
        return Team != 0;
    }

    public void ResetData()
    {
        MaxHitpoints = 50;
        CurrentHitpoints = MaxHitpoints;
        Speed = 6.0f;
        MoveDir = MoveDirs.NONE;
        WeaponState = WeaponStates.none;
        IsDead = false;
        WantsPredict = false;
    }
}