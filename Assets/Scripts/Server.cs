using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading;
using UnityEngine;
using Utility;
using Random = System.Random;

internal partial class Server
{
    private NetPeerConfiguration config;
    public NetServer server;

    public Dictionary<IPEndPoint, ClientData> clients;
    public Dictionary<short, Transform> clientsTransform;
    public static Dictionary<int, NetworkObject> netObjs = new Dictionary<int, NetworkObject>();
    private int objectCount;

    private Thread serverThread;
    //  private Thread gameThread;

    public bool debugMode;
    private bool isStarted;

    //Database stuff
    private string _constr = "URI=file:NPCMaster.db";

    private IDbConnection _dbc;
    private IDbCommand _dbcm;
    private IDataReader _dbr;

    //TODO move somewhere else
    public Transform playerPrefab;

    public Transform redBase;
    public Transform blueBase;

    private PrefabManager _prefabManager;

    public enum PacketTypes
    {
        //BASIC TYPES
        CONNECTED,
        DISCONNECTED,
        PLAYERLIST,
        NEWCLIENT,
        MOVE,
        DAMAGE,
        ANIMATION,
        TEXTMESSAGE,
        SPAWNPREFAB,
        UPDATEPREFAB,
        DESTROYPREFAB,
        TEAMSELECT,
        RESPAWN,
        WEAPONCHANGE,

        //Worldevents
        DROP,
        PICKUP,

        //INTERACTIONS
        TREECUTTING
    }

    private void Start()
    {
        Debug.Log("Setting up server ...");

        clients = new Dictionary<IPEndPoint, ClientData>();
        clientsTransform = new Dictionary<short, Transform>();
        try
        {
            config = new NetPeerConfiguration("CrystallStudios");
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.Port = 57798;
            config.UseMessageRecycling = true;

            //config.SimulatedRandomLatency = 0.6f;
            //config.SimulatedLoss = 0.05f;

            server = new NetServer(config);
            server.Start();
        }
        catch (Exception ex)
        {
            Debug.Log("Error during startup: " + ex.Message);
            DeadLine();
        }

        _prefabManager = GameObject.Find("PrefabManager").GetComponent<PrefabManager>();

        if (ConnectoDB())
        {
            serverThread = new Thread(WorkMessages);
            serverThread.Start();

            //gameThread = new Thread(WorkGameData);
            //gameThread.Start();
            isStarted = true;
            Debug.Log("Server started successfully");
            return;
        }
        if (!isStarted)
            Debug.Log("Error while starting the server");
    }

    private void Update()
    {
        if (!isStarted) return;

        CalculatePlayerMovement();
        CalculatePlayerDeathTime();
    }

    public void DeadLine()
    {
        serverThread.Abort();
        serverThread.Join();
        //gameThread.Abort();
        //gameThread.Join();
        server.Shutdown("Server shutdown.");
        Debug.Log("Server shutdown complete!");
    }

    public static short GetFreeID()
    {
        List<int> usedIds = (from netOBJ in netObjs select netOBJ.Key).ToList();
        if (usedIds.Count == 0) return 0;

        for (short id = 0; id <= usedIds.Count; id++)
            if (!usedIds.Contains(id))
                return id;

        return -1;
    }

    private static bool ConnectoDB()
    {
        return false;
    }

    //HELPER / UTLITY
    public static Vector3 CalculateDropLocation(Vector3 netObjLoc, int range)
    {
        Random ran = new Random();
        float x = netObjLoc.x + ran.Next(-range, range);

        float z = netObjLoc.z + ran.Next(-range, range);
        Debug.Log("X: " + x + "Z: " + z);
        return new Vector3(x, netObjLoc.y, z);
    }

    //TODO check for interact range (Vector3 target, float range) -> problem with unity multithreading
    private static bool CanInteract(ClientData client, Vector3 targetPos)
    {
        if (client.IsDead) return false;

        return !(Vector3.Distance(client.Position, targetPos) > GameConstants.interactRange);
    }
}