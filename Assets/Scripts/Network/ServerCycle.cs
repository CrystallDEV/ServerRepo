using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lidgren.Network;
using Network.Animation;
using Network.Health;
using Network.Movement;
using Network.Packets;
using Network.Teams;
using Network.Weapons;
using UnityEngine;
using Utility;

namespace Network
{
    internal partial class Server
    {
        private void ReadMessages()
        {
            NetIncomingMessage message;
            NetOutgoingMessage response;

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
                        response = server.CreateMessage("CrystallStudios-GameServer");
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
                        if (!clients.ContainsKey(message.SenderEndPoint))
                            break;
                        if (clientsTransform.ContainsKey(clients[message.SenderEndPoint].ID))
                        {
                            GameServerCycle.getInstance()
                                .DestroyNetObject(clientsTransform[clients[message.SenderEndPoint].ID]);
                            clientsTransform.Remove(clients[message.SenderEndPoint].ID);
                        }


                        foreach (var client in clients)
                        {
                            if (client.Key.Equals(message.SenderEndPoint) || client.Value.Connection.Equals(null))
                                continue;
                            PacketController.getInstance().SendClientDisconnect(clients[message.SenderEndPoint],
                                client.Value.Connection);
                        }

                        clients.Remove(message.SenderEndPoint);
                    }
                    else if (state == NetConnectionStatus.Connected)
                    {
                        //new player connects to the server
                        if (clients.ContainsKey(message.SenderEndPoint)) break;
                        ClientData newClient = new ClientData(message.SenderConnection, ClientData.GetFreeID(clients));
                        clients.Add(message.SenderEndPoint, newClient);

                        response = server.CreateMessage();
                        response.Write((byte) PacketTypes.CONNECTED);
                        response.Write(newClient.ID);
                        response.Write((short) clients.Count); //Anzahl aktueller Clients senden
                        server.SendMessage(response, message.SenderConnection, NetDeliveryMethod.ReliableUnordered);

                        foreach (var client in clients.Values)
                        {
                            if (client.Equals(newClient)) continue;

                            //Tell all clients, a new client connected
                            PacketController.getInstance().SendNewClientConnected(newClient);
                        }

                        Debug.Log("Created client with id '" + newClient.ID + "'!");
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
            NetOutgoingMessage response;
            switch (id)
            {
                case 0x0: //positionupdate
                    ClientData _client = clients[message.SenderEndPoint];
                    _client.MoveDir = (MoveDirs) message.ReadInt32();
                    _client.Rotation = new Vector3(message.ReadFloat(), message.ReadFloat(), message.ReadFloat());
                    if (_client.WantsPredict)
                    {
                        float movetime = message.ReadFloat();
                        if (movetime <= _client.moveTime)
                        {
                            Debug.Log("Client movetime mismatch! Client(" + _client.ID + "):" + movetime + "  -- " +
                                      _client.moveTime);
                        }
                    }

                    break;

                case 0x1: //gamestate
                    List<ClientData> _clients = (from client in clients
                        where client.Value.ID != clients[message.SenderEndPoint].ID
                        select client.Value).ToList();
                    foreach (ClientData client in _clients)
                    {
                        if (client.Connection == null) continue;
                        PacketController.getInstance()
                            .SendPlayerList(client, clients[message.SenderEndPoint].Connection);
                    }

                    foreach (var netObject in netObjs.Values)
                    {
                        PacketController.getInstance()
                            .SendNetworkObjectSpawn(netObject, clients[message.SenderEndPoint].Connection);
                    }

                    Debug.Log("Sent PLAYERLIST to " + clients[message.SenderEndPoint].ID);
                    break;

                case 0x3: //statsUpdate
                    short defenderId = message.ReadInt16();
                    ClientData defender =
                        (from client in clients where client.Value.ID == defenderId select client.Value).ToList()[0];

                    ClientData attacker = clients[message.SenderEndPoint];
                    HealthController.getInstance().CalculatePlayerHitpoints(attacker, defender);

                    foreach (ClientData client in clients.Values)
                        PacketController.getInstance().SendPlayerHPUpdate(defender, client.Connection);
                    break;

                case 0x4: //update animation
                    ClientData animPlayer = clients[message.SenderEndPoint];
                    animPlayer.AnimationState = (AnimationStates) message.ReadInt32();
                    List<ClientData> _others = Utils.GetOtherClients(animPlayer, clients);

                    foreach (ClientData c in _others)
                        PacketController.getInstance().SendPlayerAnimationState(animPlayer, c.Connection);
                    break;

                case 0x5: //chat event
                    //TODO add chat controller and check message for different parameter
                    //TODO add differenct chat channels
                    //TODO make it possible to write a personal message to other players
                    //TODO add commands
                    foreach (ClientData rec in clients.Values)
                        PacketController.getInstance()
                            .SendChatMessage(clients[message.SenderEndPoint], rec.Connection, message.ReadString());
                    break;

                case 0x6: //team join+
                    Debug.Log("Received team join request");
                    int teamId = message.ReadInt32();
                    if (TeamController.getInstance().AddToTeam(clients[message.SenderEndPoint], teamId))
                        GameServerCycle.getInstance()
                            .SpawnPlayer(clients[message.SenderEndPoint]);
                    foreach (var receiver in clients.Values)
                    {
                        PacketController.getInstance()
                            .SendPlayerTeamJoin(clients[message.SenderEndPoint], receiver.Connection);
                    }

                    break;

                case 0x7: //cutTree / farming
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

                        GameServerCycle.getInstance()
                            .DestroyNetObject(netObjs[interactEntityID]);

                        GameServerCycle.getInstance()
                            .SpawnPrefab(PrefabTypes.WOOD, dropLocation);
                    }

                    break;
                case 0x8: //addItemRequest / pickup Item
                    //TODO add picker 
                    //short pickerID = message.ReadInt16();
                    short netId = message.ReadInt16();

                    if (netObjs.ContainsKey(netId) &&
                        Utils.CanInteract(clients[message.SenderEndPoint], netObjs[netId].Position))
                    {
                        GameServerCycle.getInstance()
                            .DestroyNetObject(netObjs[netId].gameObject);
                        response = server.CreateMessage();
                        response.Write((byte) PacketTypes.PICKUP);
                        response.Write(netId);
                        server.SendToAll(response, NetDeliveryMethod.ReliableUnordered);
                        //TODO check add inventory attribute to the clientdata
                    }

                    break;

                case 0x9: //weapon change event
                    clients[message.SenderEndPoint].WeaponState = (WeaponStates) message.ReadInt16();
                    response = server.CreateMessage();
                    response.Write((byte) PacketTypes.WEAPONCHANGE);
                    response.Write(clients[message.SenderEndPoint].ID);
                    response.Write((short) clients[message.SenderEndPoint].WeaponState);
                    server.SendToAll(response, NetDeliveryMethod.ReliableUnordered);
                    break;

                case 0x10: //spawn 
//                    netId = message.ReadInt16();

                    break;
            }
        }
    }
}