using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Lidgren.Network;
using UnityEngine;

namespace Network
{
    internal partial class Server
    {
        private NetPeerConfiguration config;
        public NetServer server;

        public Dictionary<IPEndPoint, ClientData> clients;
        public Dictionary<short, Transform> clientsTransform;
        public Dictionary<int, NetworkObject> netObjs = new Dictionary<int, NetworkObject>();
        private int objectCount;

        private Thread serverThread;
        //  private Thread gameThread;

        public bool debugMode;
        public bool isStarted;

        //TODO move somewhere else
        public Transform playerPrefab;

        public Transform redBase;
        public Transform blueBase;

        private static Server instance;
        public float serverTime;

        public static Server getInstance()
        {
            return instance;
        }

        private void Awake()
        {
            //Check if instance already exists
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(gameObject);

            DontDestroyOnLoad(gameObject);
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
                Debug.LogError("Error during startup: " + ex.Message);
                DeadLine();
            }

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
                Debug.LogError("Error while starting the server");
        }

        private static bool ConnectoDB()
        {
            //TODO add database and try to connect to it, cancel if it fails
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
    }
}