using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Lidgren.Network;
using Network.Packets;
using UnityEngine;

namespace Network
{
    internal partial class Server : MonoBehaviour
    {
        private NetPeerConfiguration config;
        public NetServer server;

        public Dictionary<int, NetworkObject> netObjs = new Dictionary<int, NetworkObject>();
        private int objectCount;

        private Thread serverThread;

        public bool debugMode;
        public bool isStarted;

        //TODO move somewhere else
        public Transform playerPrefab;

        private static Server instance;
        public float serverTime;

        public Dictionary<IPEndPoint, ClientData> clients;
        public Dictionary<short, Transform> clientsTransform;

        public static Server getInstance()
        {
            return instance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private void Awake()
        {
            //Check if instance already exists
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(gameObject);

            DontDestroyOnLoad(gameObject);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private void Start()
        {
            Debug.Log("Setting up server ...");

            try
            {
                config = new NetPeerConfiguration("CrystallStudios");
                config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
                config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
                config.Port = 57798;
                config.UseMessageRecycling = true;
                config.AutoFlushSendQueue = true;

                clients = new Dictionary<IPEndPoint, ClientData>();
                clientsTransform = new Dictionary<short, Transform>();

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
            ReadMessages();
        }

        private void DeadLine()
        {
            if (isStarted)
            {
//                serverThread.Abort();
//                serverThread.Join();
                isStarted = false;
                server.Shutdown("Server shutdown.");
                Debug.Log("Server shutdown complete!");
            }
        }

        private void OnDisable()
        {
            DeadLine();
        }


        /// <summary>
        /// Adds a new client to the list 
        /// </summary>
        /// <param name="ipEndPoint"></param>
        public void AddClient(NetConnection _connection, IPEndPoint _ipEndPoint)
        {
            if (!clients.ContainsKey(_ipEndPoint))
            {
                //TODO get the username of the player that is logging in and fetch the data
                ClientData newClient = new ClientData(_connection, ClientData.GetFreeID(clients));
                clients.Add(_ipEndPoint, newClient);

                foreach (var client in clients.Values)
                {
                    if (client.Equals(newClient)) continue;
                    //Tell all clients, a new client connected
                    PacketController.getInstance().SendNewClientConnected(newClient);
                }

                PacketController.getInstance().SendClientConnectAnswer(newClient);

                if (debugMode)
                    Debug.Log("Created client with id '" + newClient.ID + "'!");
            }
        }

        /// <summary>
        /// Removes a client from the list and also removes the transform if one exists
        /// </summary>
        /// <param name="ipEndPoint"></param>
        public void RemoveClient(IPEndPoint ipEndPoint)
        {
            if (clients.ContainsKey(ipEndPoint))
            {
                ClientData _client = clients[ipEndPoint];
                //inform all players, that a client disconnected
                PacketController.getInstance().SendClientDisconnect(clients[ipEndPoint]);
                if (clientsTransform.ContainsKey(_client.ID))
                {
                    GameServerCycle.getInstance()
                        .DestroyNetObject(clientsTransform[clients[ipEndPoint].ID]);
                }

                clientsTransform.Remove(clients[ipEndPoint].ID);
                clients.Remove(ipEndPoint);
            }
        }
    }
}