using System;
using System.Collections;
using System.Linq;
using Lidgren.Network;
using UnityEngine;
using Utility;

namespace Network
{
    internal partial class Server : MonoBehaviour
    {
        //or merge all 3 into one
        private void CalculatePlayerMovement()
        {
            foreach (ClientData _client in clients.Values.ToList())
            {
                if (_client == null || !clientsTransform.ContainsKey(_client.ID)) continue;

                Vector3 dir = Vector3.zero;
                switch (_client.MoveDir)
                {
                    case MoveDirs.UP:
                        dir = Vector3.forward;
                        break;
                    case MoveDirs.UPRIGHT:
                        dir = Vector3.forward + Vector3.right;
                        break;
                    case MoveDirs.RIGHT:
                        dir = Vector3.right;
                        break;
                    case MoveDirs.RIGHTDOWN:
                        dir = Vector3.back + Vector3.right;
                        break;
                    case MoveDirs.DOWN:
                        dir = Vector3.back;
                        break;
                    case MoveDirs.DOWNLEFT:
                        dir = Vector3.back + Vector3.left;
                        break;
                    case MoveDirs.LEFT:
                        dir = Vector3.left;
                        break;
                    case MoveDirs.LEFTUP:
                        dir = Vector3.left + Vector3.forward;
                        break;
                    case MoveDirs.NONE:
                        dir = Vector3.zero;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Transform player = clientsTransform[_client.ID];
                player.rotation = Quaternion.Euler(_client.Rotation);

                player.Translate(dir.normalized * _client.Speed * Time.deltaTime, Space.World);
                _client.Position = player.transform.position;

                if (_client.LastPosition == _client.Position) continue;
                if (_client.WantsPredict)
                {
                    _client.moveTime++;
                }
                _client.LastPosition = _client.Position;
                SendPlayerPosition(_client);
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
                    SendPlayerRespawn(_client);
                }
            }
        }

        private void SendPlayerPosition(ClientData _client)
        {
            NetOutgoingMessage response = server.CreateMessage();
            response.Write((byte) PacketTypes.MOVE);
            response.Write(_client.ID);
            response.Write(_client.Position.x);
            response.Write(_client.Position.y);
            response.Write(_client.Position.z);

            response.Write(_client.Rotation.x);
            response.Write(_client.Rotation.y);
            response.Write(_client.Rotation.z);
            response.Write(_client.moveTime);
            server.SendToAll(response, NetDeliveryMethod.UnreliableSequenced);
        }

        private void SendPlayerRespawn(ClientData _client)
        {
            NetOutgoingMessage response = server.CreateMessage();
            response.Write((byte) PacketTypes.RESPAWN);
            response.Write(_client.ID);
            response.Write(_client.Position.x);
            response.Write(_client.Position.y);
            response.Write(_client.Position.z);

            response.Write(_client.Rotation.x);
            response.Write(_client.Rotation.y);
            response.Write(_client.Rotation.z);
            server.SendToAll(response, NetDeliveryMethod.ReliableUnordered);
        }


        private IEnumerator KillPlayer(ClientData _client)
        {
            Transform player = clientsTransform[_client.ID];
            clientsTransform.Remove(_client.ID);
            DestroyNetObject(player);
            yield return null;
        }

        public IEnumerator Jump(ClientData _client)
        {
            //Transform player = clientsTransform[_client.ID];
            //TODO Jump
            yield return null;
        }


        private IEnumerator SpawnPlayer(ClientData _client)
        {
            if (_client.Team == 0) yield break;
            Transform player = Instantiate(playerPrefab);
            player.name = _client.ID.ToString();
            clientsTransform.Add(_client.ID, player);

            switch (_client.Team)
            {
                case 1:
                    player.position = redBase.position;
                    break;
                case 2:
                    player.position = blueBase.position;
                    break;
            }

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

        private IEnumerator DropItem(PrefabTypes type, Vector3 pos)
        {
            GameObject drop = Instantiate(_prefabManager.getPrefabByID((int) type));
            drop.transform.position = pos;
            yield return null;
        }


        //data 1 = attacker, data 2 = attacked
        private IEnumerator CalculatePlayerHitpoints(ClientData attacker, ClientData defender)
        {
            //TODO -> Test for different options, if the attack is possible etc. , maybe new Class / method
            if (clientsTransform[defender.ID] == null)
            {
                Debug.Log(attacker.ID + " tried to attack a transform that doesn't exist");
                yield break;
            }
            if (Vector3.Distance(attacker.Position, defender.Position) > 10)
            {
                Debug.Log(attacker.ID + " tried to hit another player(" + defender.ID + "), who is out of range");
                yield break;
            }
            if (attacker.Team == defender.Team)
            {
                Debug.Log("Player: " + attacker.ID + " cant hit player: " + defender.ID +
                          " because they are in the same team");
                yield break;
            }

            Debug.Log("Player " + attacker.ID + " damaged Player " + defender.ID + " for 10 DMG");
            if (defender.CurrentHitpoints - 10 < 0)
            {
                defender.IsDead = true;
                defender.CurrentHitpoints = 0;
                defender.DeathTime = GameConstants.deathTime;
                StartCoroutine(KillPlayer(defender));
                Debug.Log("Player: " + defender.ID + " has died.");
            }
            else
            {
                defender.CurrentHitpoints -= 10;
            }
        }

        private void CutTree(ClientData client, NetworkObject netObj)
        {
            //TODO
        }
    }
}