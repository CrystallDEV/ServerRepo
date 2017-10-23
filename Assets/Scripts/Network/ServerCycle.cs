using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lidgren.Network;
using UnityEngine;
using Utility;

internal partial class Server
{
    //        Server|
    //-------
    //0x00: Client connected (wird an alle Clients versendet)
    //0x01: Client disconnected (wird an alle Clients versendet)
    //0x02: Client-Information für connectenden Client: Teilt ihm seine ID mit
    //0x03: Client hat Spieler x gehitted
    //0x04: Client hat Item X benutzt
    //0x05: Client hat zu Waffe X gewechselt
    //0x06: Client hat seine Todeszeit nachgefragt, wenn lebt -> sende respawn

    //0x10: Positionsänderung (wird an alle Clients versendet)
    //0x11: Liste aller Clients (ID und Position) für einen neu-verbundenen Client
    //0x12: Akualisiere GameData (GameTime)
    //0x13: Sende Spieler X wurde getroffen
    //0x14: Sende Spieler X hat Item Y benutzt
    //0x15: Sende Spieler X hat zu Waffe Y gewechselt
    //0x16: RespawnPlayerz

    public void WorkMessages()
    {
        //Message-Cycle: Dauerhaft Messages verarbeiten, bis das Programm beendet wird bzw. ein Fehler auftritt

        while (serverThread.ThreadState != ThreadState.AbortRequested)
        {
            ReadMessages();
        }
    }

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
                //Ein Client will connecten -> Erlauben und antworten
                if (!clients.ContainsKey(message.SenderEndPoint))
                {
                    response = server.CreateMessage("CrystallStudios-GameServer");
                    server.SendDiscoveryResponse(response, message.SenderEndPoint);

                    Debug.Log(message.SenderEndPoint + " has discovered the server!");
                }
                break;
            case NetIncomingMessageType.ConnectionApproval:
                //Clients die Connecten wollen dies erlauben
                message.SenderConnection.Approve();
                Debug.Log(message.SenderEndPoint + " approved.");
                break;
            case NetIncomingMessageType.Data:
                //Sämtliche Daten die Clients senden annehmen
                byte type = message.ReadByte();
                ProcessMessage(type, message);
                break;
            case NetIncomingMessageType.StatusChanged:
                //Falls ein Client connected / disconnected
                NetConnectionStatus state = (NetConnectionStatus) message.ReadByte();
                if (state == NetConnectionStatus.Disconnected || state == NetConnectionStatus.Disconnecting)
                {
                    //player leaves the server
                    if (!clients.ContainsKey(message.SenderEndPoint))
                        break;
                    Debug.Log("removing player ...");
                    if (clientsTransform[clients[message.SenderEndPoint].ID] != null)
                    {
                        clientsTransform.Remove(clients[message.SenderEndPoint].ID);
                        UnityMainThreadDispatcher.Instance()
                            .Enqueue(DestroyNetObject(clientsTransform[clients[message.SenderEndPoint].ID]));
                    }
                    clients.Remove(message.SenderEndPoint);
                    Debug.Log("removed player informing other players...");
                    foreach (var client in clients)
                    {
                        if (client.Key.Equals(message.SenderEndPoint) || client.Value.Connection.Equals(null)) continue;

                        response = server.CreateMessage();
                        response.Write((byte) PacketTypes.DISCONNECTED); //0x01: Ein Client hat disconnected
                        response.Write(clients[message.SenderEndPoint].ID);
                        server.SendMessage(response, client.Value.Connection, NetDeliveryMethod.ReliableUnordered);
                    }
                    Debug.Log(message.SenderEndPoint + " disconnected!");
                }
                else if (state == NetConnectionStatus.Connected)
                {
                    //new players connects to the server
                    if (clients.ContainsKey(message.SenderEndPoint)) break;
                    ClientData newClient = new ClientData(message.SenderConnection, ClientData.GetFreeID(clients));
                    clients.Add(message.SenderEndPoint, newClient);
                    Debug.Log("Created client with id '" + newClient.ID + "'!");

                    response = server.CreateMessage();
                    response.Write((byte) PacketTypes
                        .CONNECTED); //0x02: Clientinformation um neuen Clienten seine ID mitzuteilen
                    response.Write(newClient.ID);
                    response.Write((short) clients.Count); //Anzahl aktueller Clients senden
                    server.SendMessage(response, message.SenderConnection, NetDeliveryMethod.ReliableUnordered);

                    if (debugMode)
                        Debug.Log("Sent 0x02 to " + newClient.ID);

                    foreach (var client in clients)
                    {
                        if (client.Key.Equals(message.SenderEndPoint)) continue;

                        //Alle clients informieren, dass ein neuer connected
                        response = server.CreateMessage();
                        response.Write((byte) PacketTypes.NEWCLIENT); //0x00: Neuer Client connected
                        response.Write(newClient.ID); //Seine ID mitteilen
                        response.Write(newClient.Position.x);
                        response.Write(newClient.Position.y);
                        response.Write(newClient.Position.z);

                        response.Write(newClient.Rotation.x);
                        response.Write(newClient.Rotation.y);
                        response.Write(newClient.Rotation.z);
                        server.SendMessage(response, client.Value.Connection, NetDeliveryMethod.ReliableUnordered);
                    }

                    if (debugMode)
                        Debug.Log("Sent 0x00 to all");

                    Debug.Log(message.SenderEndPoint + " connected!");
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
                float moveTime = message.ReadFloat();
                if (_client.moveTime != moveTime)
                {
                    Debug.Log("movetime mismatch from client " + _client.UserName + ":" + _client.ID + ". Client movetime: " + moveTime + " - server movetime: " + _client.moveTime);
                    //TODO propably update client movetime so the client matches the server movetime
                }
                break;

            //TODO RENAME TO GAMESTATE, since it now updates the gamestate?
            case 0x1: //Client fordert eine komplette Liste aller Clients mit deren ID und Position
                List<ClientData> _clients = (from client in clients
                    where (client).Value.ID != clients[message.SenderEndPoint].ID
                    select client.Value).ToList();
                foreach (ClientData client in _clients)
                {
                    if (client.Connection == null) continue;

                    response = server.CreateMessage();
                    response.Write((byte) PacketTypes.PLAYERLIST);
                    response.Write(client.ID);
                    response.Write(client.Position.x);
                    response.Write(client.Position.y);
                    response.Write(client.Position.z);

                    response.Write(client.Rotation.x);
                    response.Write(client.Rotation.y);
                    response.Write(client.Rotation.z);

                    response.Write(client.Team);

                    server.SendMessage(response, clients[message.SenderEndPoint].Connection,
                        NetDeliveryMethod.ReliableUnordered);
                }

                foreach (var netObject in netObjs.Values)
                {
                    response = server.CreateMessage();
                    response.Write((byte) PacketTypes.SPAWNPREFAB);
                    response.Write(netObject.ID);
                    response.Write(netObject.GetPrefabId);
                    response.Write(netObject.Position.x);
                    response.Write(netObject.Position.y);
                    response.Write(netObject.Position.z);

                    response.Write(netObject.Rotation.x);
                    response.Write(netObject.Rotation.y);
                    response.Write(netObject.Rotation.z);

                    server.SendMessage(response, clients[message.SenderEndPoint].Connection,
                        NetDeliveryMethod.ReliableUnordered);
                }

                if (debugMode)
                    Debug.Log("Sent PLAYERLIST to " + clients[message.SenderEndPoint].ID);
                break;

            case 0x3: //statsUpdate
                short defenderID = message.ReadInt16();
                ClientData defender = (from client in clients where (client).Value.ID == defenderID select client.Value)
                    .ToList()[0];
                ClientData attacker = clients[message.SenderEndPoint];
                UnityMainThreadDispatcher.Instance().Enqueue(CalculatePlayerHitpoints(attacker, defender));

                foreach (ClientData client in clients.Values)
                {
                    response = server.CreateMessage();
                    response.Write((byte) PacketTypes.DAMAGE);
                    response.Write(defenderID);
                    response.Write(defender.CurrentHitpoints);

                    server.SendMessage(response, client.Connection, NetDeliveryMethod.ReliableOrdered);
                }
                break;

            case 0x4: //update animation
                ClientData animPlayer = clients[message.SenderEndPoint];
                animPlayer.AnimationState = (AnimationStates) message.ReadInt32();

                List<ClientData> _others = (from client in clients
                    where (client).Value.ID != clients[message.SenderEndPoint].ID
                    select client.Value).ToList();
                foreach (ClientData c in _others)
                {
                    response = server.CreateMessage();
                    response.Write((byte) PacketTypes.ANIMATION);
                    response.Write(animPlayer.ID);
                    response.Write((int) animPlayer.AnimationState);
                    server.SendMessage(response, c.Connection, NetDeliveryMethod.ReliableOrdered);
                }
                break;

            case 0x5: //chat event
                List<ClientData> recipents = (from client in clients
                    where (client).Value.ID != clients[message.SenderEndPoint].ID
                    select client.Value).ToList();
                foreach (ClientData rec in recipents)
                {
                    response = server.CreateMessage();
                    response.Write((byte) PacketTypes.TEXTMESSAGE);
                    response.Write(clients[message.SenderEndPoint].ID);
                    response.Write(message.ReadString());

                    server.SendMessage(response, rec.Connection, NetDeliveryMethod.ReliableOrdered);
                }
                break;

            case 0x6: //team join
                short selectorID = clients[message.SenderEndPoint].ID;
                clients[message.SenderEndPoint].Team = message.ReadInt32();
                UnityMainThreadDispatcher.Instance().Enqueue(SpawnPlayer(clients[message.SenderEndPoint]));
                //TODO CHECK IF PLAYER CAN JOIN TEAM
                //TODO ADD TEAM SIZE (arraylist team1,team2)
                response = server.CreateMessage();
                response.Write((byte) PacketTypes.TEAMSELECT);
                response.Write(selectorID);
                response.Write(clients[message.SenderEndPoint].Team);
                server.SendToAll(response, NetDeliveryMethod.ReliableOrdered);
                break;

            case 0x7: //cutTree / farming
                short interactEntityID = message.ReadInt16();

                Vector3 targetPos = new Vector3(message.ReadFloat(), message.ReadFloat(), message.ReadFloat());

                if (netObjs.ContainsKey(interactEntityID) &&
                    CanInteract(clients[message.SenderEndPoint], targetPos))
                {
                    //TODO add multiple ressources and dont return on another prefab as tree
                    if (netObjs[interactEntityID].prefabType != PrefabTypes.TREE) return;

                    Vector3 dropLocation =
                        CalculateDropLocation(netObjs[interactEntityID].Position, (int) GameConstants.dropRange);
                    short ID = GetFreeID();
                    UnityMainThreadDispatcher.Instance().Enqueue(DestroyNetObject(netObjs[interactEntityID]));
                    UnityMainThreadDispatcher.Instance().Enqueue(DropItem(PrefabTypes.TREE, dropLocation));

                    response = server.CreateMessage();
                    response.Write((byte) PacketTypes.SPAWNPREFAB);
                    response.Write(ID);
                    response.Write((int) PrefabTypes.WOOD);
                    response.Write(dropLocation.x);
                    response.Write(dropLocation.y);
                    response.Write(dropLocation.z);

                    response.Write(Vector3.zero.z);
                    response.Write(Vector3.zero.z);
                    response.Write(Vector3.zero.z);

                    server.SendToAll(response, NetDeliveryMethod.ReliableUnordered);
                }
                break;
            //TODO 
            case 0x8: //addItemRequest / pickup Item
                //short pickerID = message.ReadInt16();
                short netId = message.ReadInt16();

                if (netObjs.ContainsKey(netId) && CanInteract(clients[message.SenderEndPoint], netObjs[netId].Position))
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(DestroyNetObject(netObjs[netId].gameObject));

                    response = server.CreateMessage();
                    response.Write((byte) PacketTypes.PICKUP);
                    response.Write(netId);
                    server.SendToAll(response, NetDeliveryMethod.ReliableUnordered);
                    //TODO check 
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

            case 0x10: //TODO attack (animation + particles)
                netId = message.ReadInt16();

                break;
        }
    }
}