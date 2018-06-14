using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using Network.Animation;
using Network.Health;
using Network.Movement;
using Network.Packets;
using Network.Prefabs;
using Network.Teams;
using Network.Weapons;
using UnityEditor.PackageManager;
using UnityEngine;
using Utility;

namespace Network
{
    internal partial class Server
    {
        private void ReadMessages()
        {
            NetIncomingMessage message;

            if ((message = server.ReadMessage()) == null)
                return;

            switch (message.MessageType)
            {
                case NetIncomingMessageType.VerboseDebugMessage:
                case NetIncomingMessageType.DebugMessage:
                    Debug.Log(message.ReadString());
                    break;

                case NetIncomingMessageType.WarningMessage:
                    Debug.Log(message.ReadString());
                    break;

                case NetIncomingMessageType.ErrorMessage:
                    Debug.Log(message.ReadString());
                    break;

                case NetIncomingMessageType.DiscoveryRequest:
                    //Answer discoveryrequest from client and allow to connect
                    if (!clients.ContainsKey(message.SenderEndPoint))
                    {
                        NetOutgoingMessage response = server.CreateMessage("CrystallStudios-GameServer");
                        server.SendDiscoveryResponse(response, message.SenderEndPoint);

                        Debug.Log(message.SenderEndPoint + " has discovered the server!");
                    }

                    break;

                case NetIncomingMessageType.ConnectionApproval:
                    //Accept new clients that are trying to connect
                    message.SenderConnection.Approve();
                    Debug.Log(message.SenderEndPoint + " approved.");
                    break;

                case NetIncomingMessageType.Data:
                    byte type = message.ReadByte();
                    ProcessMessage(type, message);
                    break;

                case NetIncomingMessageType.StatusChanged:
                    NetConnectionStatus state = (NetConnectionStatus) message.ReadByte();
                    if (state == NetConnectionStatus.Disconnected || state == NetConnectionStatus.Disconnecting)
                    {
                        RemoveClient(message.SenderEndPoint);
                    }
                    else if (state == NetConnectionStatus.Connected)
                    {
                        AddClient(message.SenderConnection, message.SenderEndPoint);
                    }

                    break;

                default:
                    Debug.Log("Unhandled Messagetype: " + message.MessageType);
                    break;
            }
        }

        private void ProcessMessage(byte id, NetIncomingMessage message)
        {
            //Protokoll : [Byte], [Value], [Value], ...
            ClientData _client = clients[message.SenderEndPoint];
            switch (id)
            {
                case 0x0: //positionupdate
                    _client.MoveDir = (MoveDirs) message.ReadInt32();
                    _client.Rotation = new Vector3(message.ReadFloat(), message.ReadFloat(), message.ReadFloat());
                    if (_client.WantsPredict)
                    {
                        float movetime = message.ReadFloat();
                        if (movetime <= _client.moveTime)
                        {
                            if (debugMode)
                                Debug.Log("Client movetime mismatch! Client(" + _client.ID + "):" + movetime + "  -- " +
                                          _client.moveTime);
                        }
                    }

                    break;

                case 0x1: //gamestate
                    List<ClientData> _clients = (from client in clients
                        where client.Value.ID != clients[message.SenderEndPoint].ID
                        select client.Value).ToList();
                    foreach (ClientData c in _clients)
                    {
                        if (c.Connection == null) continue;
                        PacketController.getInstance().SendPlayerList(c, clients[message.SenderEndPoint].Connection);
                    }

                    foreach (var netObject in netObjs.Values)
                    {
                        PacketController.getInstance()
                            .SendNetworkObjectSpawn(netObject, clients[message.SenderEndPoint].Connection);
                    }

                    Debug.Log("Sent PLAYERLIST to " + clients[message.SenderEndPoint].ID);
                    /*Takes me one step closer to the
                    edge and I'm about to */
                    break;

                case 0x3: //statsUpdate
                    short defenderId = message.ReadInt16();
                    ClientData defender =
                        (from client in clients where client.Value.ID == defenderId select client.Value).ToList()[0];

                    ClientData attacker = clients[message.SenderEndPoint];
                    HealthController.getInstance().CalculatePlayerHitpoints(attacker, defender);

                    foreach (ClientData client in clients.Values)
                    {
                        PacketController.getInstance().SendPlayerHPUpdate(defender, client.Connection);
                    }

                    break;

                case 0x4: //update animation
                    ClientData animPlayer = clients[message.SenderEndPoint];
                    animPlayer.AnimationState = (AnimationStates) message.ReadInt32();
                    List<ClientData> _others = Utils.GetOtherClients(animPlayer, clients);

                    foreach (ClientData c in _others)
                    {
                        PacketController.getInstance().SendPlayerAnimationState(animPlayer, c.Connection);
                    }

                    break;

                case 0x5: //chat event
                    //TODO add chat controller and check message for different parameter
                    //TODO add differenct chat channels
                    //TODO make it possible to write a personal message to other players
                    //TODO add commands
                    foreach (ClientData rec in clients.Values)
                    {
                        PacketController.getInstance().SendChatMessage(clients[message.SenderEndPoint], rec.Connection,
                            message.ReadString());
                    }

                    break;

                case 0x6: //team join
                    int teamId = message.ReadInt32();
                    if (TeamController.getInstance().AddToTeam(clients[message.SenderEndPoint], teamId))
                        GameServerCycle.getInstance().SpawnPlayer(clients[message.SenderEndPoint]);
                    foreach (var receiver in clients.Values)
                    {
                        PacketController.getInstance()
                            .SendPlayerTeamJoin(clients[message.SenderEndPoint], receiver.Connection);
                    }

                    break;

                case 0x7: //farming
                    short interactEntityID = message.ReadInt16();
                    Vector3 targetPos = new Vector3(message.ReadFloat(), message.ReadFloat(), message.ReadFloat());

                    if (netObjs.ContainsKey(interactEntityID) &&
                        Utils.CanInteract(clients[message.SenderEndPoint], targetPos))
                    {
                        //TODO add multiple ressources and dont return on another prefab as tree
                        //TODO get a list of interactable types / mineable types
                        //TODO add a interact function to every type of entity and call the function of the networkobject
                        if (netObjs[interactEntityID].prefabType != PrefabTypes.TREE) return;

                        Vector3 dropLocation = Utils.CalculateDropLocation(netObjs[interactEntityID].Position,
                            (int) GameConstants.dropRange);

                        GameServerCycle.getInstance().DestroyNetObject(netObjs[interactEntityID]);
                        PrefabController.getInstance().SpawnPrefab(PrefabTypes.WOOD, dropLocation);
                    }

                    break;

                case 0x8: //addItemRequest / pickup Item
                    //TODO add picker 
                    //short pickerID = message.ReadInt16();
                    short netId = message.ReadInt16();

                    if (netObjs.ContainsKey(netId) &&
                        Utils.CanInteract(clients[message.SenderEndPoint], netObjs[netId].Position))
                    {
                        //TODO move into own class as an interact event and check what happens if a player interacts with an item (pickup, destroy etc.)
                        //GameServerCycle.getInstance().DestroyNetObject(netObjs[netId].gameObject);
                        //var response = server.CreateMessage();
                        //response.Write((byte) PacketTypes.PICKUP);
                        //response.Write(netId);
                        //server.SendToAll(response, NetDeliveryMethod.ReliableUnordered);
                        //TODO check add inventory attribute to the clientdata
                    }

                   
                    break;

                case 0x9: //weapon change event
                    _client.WeaponState = (WeaponStates) message.ReadInt32();

                    if (debugMode)
                        Debug.Log("Changed weapon of " + _client.ID + " to " + _client.WeaponState);

                    PacketController.getInstance().SendWeaponChange(_client);
                    break;

                case 0x10: //spawn 
//                    netId = message.ReadInt16();
                    break;

                case 0x11: //attack request (spells, projectiles)

                    PrefabController.getInstance().SpawnPrefab(PrefabTypes.ARROW, _client.Position);
                    break;
            }
        }
    }
}