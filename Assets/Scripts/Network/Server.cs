using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Lidgren.Network;
using UnityEngine;
using Utility;
using Random = System.Random;

namespace Network
{
    public enum PacketTypes
    {
        //BASIC TYPES
        CONNECTED = 0,
        DISCONNECTED = 1,
        PLAYERLIST = 2,
        NEWCLIENT = 3,
        MOVE = 4,
        DAMAGE = 5,
        ANIMATION = 6,
        TEXTMESSAGE = 7,
        SPAWNPREFAB = 8,
        UPDATEPREFAB = 9,
        DESTROYPREFAB = 10,
        TEAMSELECT = 11,
        RESPAWN = 12,
        WEAPONCHANGE = 13,

        //Worldevents
        DROP = 14,
        PICKUP = 15,

        //INTERACTIONS
        TREECUTTING = 16
    }

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
        public bool isStarted;

        //TODO move somewhere else
        public Transform playerPrefab;

        public Transform redBase;
        public Transform blueBase;

        private PrefabManager _prefabManager;

        public float serverTime;

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

        private static bool ConnectoDB()
        {
            //TODO
            return true;
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

        //HELPER / UTLITY
        public static Vector3 CalculateDropLocation(Vector3 netObjLoc, int range)
        {
            Random ran = new Random();
            float x = netObjLoc.x + ran.Next(-range, range);
            float y = netObjLoc.y - 0.2f;
            float z = netObjLoc.z + ran.Next(-range, range);
            Debug.Log("X: " + x + "Y: " + y + "Z: " + z);
            return new Vector3(x, y, z);
        }

        //TODO check for interact range (Vector3 target, float range) -> problem with unity multithreading
        private static bool CanInteract(ClientData client, Vector3 targetPos)
        {
            if (client.IsDead) return false;

            return !(Vector3.Distance(client.Position, targetPos) > GameConstants.interactRange);
        }
    }
}