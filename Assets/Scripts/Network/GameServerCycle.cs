using Network.Movement;
using Network.Packets;
using UnityEngine;

namespace Network
{
    public class GameServerCycle : MonoBehaviour
    {
        private static GameServerCycle instance;

        public GameObject playerPrefab;

        public static GameServerCycle getInstance()
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

        private void Update()
        {
            if (!Server.getInstance().isStarted) return;
            CalculatePlayerMovement();
            CalculatePlayerDeathTime();
        }

        public void CalculatePlayerMovement()
        {
            foreach (ClientData _client in Server.getInstance().clients.Values)
            {
                if (_client == null || !Server.getInstance().clientsTransform.ContainsKey(_client.ID)) continue;
                MovementController.getInstance().UpdatePlayerPosition(_client);
            }
        }

        public void CalculatePlayerDeathTime()
        {
            foreach (ClientData _client in Server.getInstance().clients.Values)
            {
                if (_client == null || !_client.IsDead) continue;

                if (_client.DeathTime > 0)
                {
                    _client.DeathTime -= Time.deltaTime;
                }
                else
                {
                    Server.getInstance().clients[_client.Connection.RemoteEndPoint] =
                        new ClientData(_client.Connection, _client.ID);
                    SpawnPlayer(_client);
                    PacketController.getInstance().SendPlayerRespawn(_client);
                }
            }
        }

        public void SpawnPlayer(ClientData _client)
        {
            if (!_client.HasTeam()) return;
            GameObject player = Instantiate(playerPrefab);
            player.name = _client.ID.ToString();
            Server.getInstance().clientsTransform.Add(_client.ID, player.transform);
            player.transform.position = _client.Team.spawn;
        }

        public void DestroyNetObject(Transform toDestroy)
        {
            Destroy(toDestroy.gameObject);
        }

        public void DestroyNetObject(GameObject toDestroy)
        {
            Destroy(toDestroy);
        }

        public void DestroyNetObject(NetworkObject toDestroy)
        {
            Destroy(toDestroy.gameObject);
        }
    }
}