using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using UnityEngine;

partial class Server : MonoBehaviour {
    private NetPeerConfiguration config;
    public NetServer server;

    public Dictionary<IPEndPoint, ClientData> clients;
    public Dictionary<short, Transform> clientsTransform;
    public static List<NetworkObject> netObjs = new List<NetworkObject>();

    private Thread serverThread;
    //  private Thread gameThread;

    private bool debugMode = false;
    private bool isStarted = false;

    public Transform playerPrefab;

    public Transform redBase;
    public Transform blueBase;

    public enum PacketTypes {
        CONNECTED,
        DISCONNECTED,
        PLAYERLIST,
        NEWCLIENT,
        MOVE,
        DAMAGE,
        ANIMATION,
        TEXTMESSAGE,
        NETOBJ,
        SPAWNPREFAB,
        TEAMSELECT,
    }

    private void Start () {
        Debug.Log("Setting up server ...");

        clients = new Dictionary<IPEndPoint, ClientData>();
        clientsTransform = new Dictionary<short, Transform>();
        try {
            config = new NetPeerConfiguration("CrystallStudios");
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.Port = 57798;
            config.UseMessageRecycling = true;

            //config.SimulatedRandomLatency = 0.6f;
            //config.SimulatedLoss = 0.05f;

            server = new NetServer(config);
            server.Start();
        } catch (Exception ex) {
            Debug.Log("Error during startup: " + ex.Message);
            DeadLine();
        }

        serverThread = new Thread(WorkMessages);
        serverThread.Start(server);

        //gameThread = new Thread(WorkGameData);
        //gameThread.Start();
        isStarted = true;
        Debug.Log("Server started successfully");

    }

    private void Update () {
        if (isStarted) {
            CalculatePlayerMovement();
        }
    }

    public void DeadLine () {
        serverThread.Abort();
        serverThread.Join();
        //gameThread.Abort();
        //gameThread.Join();
        server.Shutdown("Server shutdown.");
        Debug.Log("Server shutdown complete!");
    }

    public static short GetFreeID () {
        List<short> usedIds = (from netOBJ in netObjs select netOBJ.GetID).ToList();
        if (usedIds.Count == 0) return 0;

        for (short id = 0; id <= usedIds.Count; id++)
            if (!usedIds.Contains(id))
                return id;

        return -1;
    }
}