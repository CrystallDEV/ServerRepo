using System.Collections;
using System.Linq;
using Network.Health;
using Network.Movement;
using Network.Packets;
using Network.Prefabs;
using UnityEngine;
using Utility;

namespace Network
{
    internal partial class Server : MonoBehaviour
    {
        private void CalculatePlayerMovement()
        {
            foreach (ClientData _client in clients.Values.ToList())
            {
                if (_client == null || !clientsTransform.ContainsKey(_client.ID)) continue;
                MovementController.getInstance().UpdatePlayerPosition(_client);
            }
        }

        private void CalculatePlayerDeathTime()
        {
            foreach (ClientData _client in clients.Values.ToList())
            {
                if (_client == null || !_client.IsDead) continue;

                if (_client.DeathTime > 0)
                {
                    _client.DeathTime -= Time.deltaTime;
                }
                else
                {
                    clients[_client.Connection.RemoteEndPoint] = new ClientData(_client.Connection, _client.ID);
                    StartCoroutine(SpawnPlayer(_client));
                    PacketController.getInstance().SendPlayerRespawn(_client);
                }
            }
        }

        private IEnumerator SpawnPrefab(PrefabTypes type, Vector3 dropLocation)
        {
            PrefabController.getInstance().SpawnPrefab(type, dropLocation, Quaternion.Euler(Vector3.zero));
            yield return null;
        }


        private IEnumerator KillPlayer(ClientData _client)
        {
            HealthController.getInstance().KillPlayer(_client);
            yield return null;
        }


        private IEnumerator SpawnPlayer(ClientData _client)
        {
            //TODO spawn player at team spawn location (new field)
            //TODO get player by new function GetTeamFromPlayer(Team team) / GetTeamFromPlayer(int id)
            if (!_client.HasTeam()) yield break;
            Transform player = Instantiate(playerPrefab);
            player.name = _client.ID.ToString();
            clientsTransform.Add(_client.ID, player);

            player.position = _client.Team.spawn;
            yield return null;
        }

        private IEnumerator DestroyNetObject(Transform toDestroy)
        {
            Destroy(toDestroy.gameObject);
            yield return null;
        }

        private IEnumerator DestroyNetObject(GameObject toDestroy)
        {
            Destroy(toDestroy);
            yield return null;
        }

        private IEnumerator DestroyNetObject(NetworkObject toDestroy)
        {
            Destroy(toDestroy.gameObject);
            yield return null;
        }


        private void CutTree(ClientData client, NetworkObject netObj)
        {
            //TODO
        }
    }
}