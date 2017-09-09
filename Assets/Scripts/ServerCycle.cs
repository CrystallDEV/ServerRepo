using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lidgren.Network;
using UnityEngine;
using System.Collections;

partial class Server : MonoBehaviour {
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

    public void WorkMessages (object param) {
        NetServer server = (NetServer) param;

        //Message-Cycle: Dauerhaft Messages verarbeiten, bis das Programm beendet wird bzw. ein Fehler auftritt

        while (serverThread.ThreadState != ThreadState.AbortRequested) {
            ReadMessages();
            SendMessages();
        }
    }

    private void ReadMessages () {
        NetIncomingMessage message;
        NetOutgoingMessage response;

        if ((message = server.ReadMessage()) == null)
            return;
        switch (message.MessageType) {
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
                if (!clients.ContainsKey(message.SenderEndPoint)) {
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
                if (state == NetConnectionStatus.Disconnected || state == NetConnectionStatus.Disconnecting) { //player leaves the server
                    if (!clients.ContainsKey(message.SenderEndPoint))
                        break;
                    foreach (var client in clients) {
                        if (client.Key != message.SenderEndPoint && client.Value.Connection != null) {
                            response = server.CreateMessage();
                            response.Write((byte) PacketTypes.DISCONNECTED); //0x01: Ein Client hat disconnected
                            response.Write(clients[message.SenderEndPoint].ID);
                            server.SendMessage(response, client.Value.Connection, NetDeliveryMethod.ReliableUnordered);
                        }
                    }

                    //Allen anderen dies mitteilen
                    UnityMainThreadDispatcher.Instance().Enqueue(DespawnPlayer(clientsTransform[clients[message.SenderEndPoint].ID].gameObject));
                    clientsTransform.Remove(clients[message.SenderEndPoint].ID);
                    clients.Remove(message.SenderEndPoint);
                    Debug.Log(message.SenderEndPoint + " disconnected!");

                } else if (state == NetConnectionStatus.Connected) { //new players connects to the server
                    if (clients.ContainsKey(message.SenderEndPoint)) break;
                    ClientData newClient = new ClientData(message.SenderConnection, ClientData.GetFreeID(clients));
                    clients.Add(message.SenderEndPoint, newClient);
                    Debug.Log("Created client with id '" + newClient.ID + "'!");

                    response = server.CreateMessage();
                    response.Write((byte) PacketTypes.CONNECTED); //0x02: Clientinformation um neuen Clienten seine ID mitzuteilen
                    response.Write(newClient.ID);
                    response.Write((short) clients.Count); //Anzahl aktueller Clients senden
                    server.SendMessage(response, message.SenderConnection, NetDeliveryMethod.ReliableUnordered);

                    if (debugMode)
                        Debug.Log("Sent 0x02 to " + newClient.ID);

                    foreach (var client in clients) {
                        if (client.Key != message.SenderEndPoint) {
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

    private void SendMessages () {
        SendPlayerPosition();
    }

    private void ProcessMessage (byte id, NetIncomingMessage message) {
        //Protokoll : [Byte], [Value], [Value]
        NetOutgoingMessage response;
        switch (id) {
            case 0x0: //0x0 steht für Positions-Informationen eines Clienten
                short senderID = clients[message.SenderEndPoint].ID;
                clients[message.SenderEndPoint].MoveDir = (MoveDirs) message.ReadInt32();
                clients[message.SenderEndPoint].Rotation = new Vector3(message.ReadFloat(), message.ReadFloat(), message.ReadFloat());
                break;

            case 0x1:                    //Client fordert eine komplette Liste aller Clients mit deren ID und Position
                List<ClientData> _clients = (from client in clients where (client).Value.ID != clients[message.SenderEndPoint].ID select client.Value).ToList();
                foreach (ClientData client in _clients) {
                    if (client.Connection != null && clientsTransform.ContainsKey(client.ID)) {
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

                        server.SendMessage(response, clients[message.SenderEndPoint].Connection, NetDeliveryMethod.ReliableUnordered);
                    }
                }
                if (debugMode)
                    Debug.Log("Sent PLAYERLIST to " + clients[message.SenderEndPoint].ID);
                break;

            case 0x3: //statsUpdate
                short defenderID = message.ReadInt16();
                ClientData defender = (from client in clients where (client).Value.ID == defenderID select client.Value).ToList()[0];
                ClientData attacker = clients[message.SenderEndPoint];
                CalculatePlayerHitpoints(attacker, defender);

                foreach (ClientData client in clients.Values) {
                    response = server.CreateMessage();
                    response.Write((byte) PacketTypes.DAMAGE);
                    response.Write(defenderID);
                    response.Write(defender.CurrentHitpoints);

                    server.SendMessage(response, client.Connection, NetDeliveryMethod.ReliableOrdered);
                }
                break;

            case 0x4:
                ClientData animPlayer = clients[message.SenderEndPoint];
                animPlayer.animationState = (AnimationStates) message.ReadInt32();

                List<ClientData> _others = (from client in clients where (client).Value.ID != clients[message.SenderEndPoint].ID select client.Value).ToList();
                foreach (ClientData c in _others) {
                    response = server.CreateMessage();
                    response.Write((byte) PacketTypes.ANIMATION);
                    response.Write(animPlayer.ID);
                    response.Write((int) animPlayer.animationState);
                    server.SendMessage(response, c.Connection, NetDeliveryMethod.ReliableOrdered);
                }
                break;

            case 0x5: //chat event
                List<ClientData> recipents = (from client in clients where (client).Value.ID != clients[message.SenderEndPoint].ID select client.Value).ToList();
                foreach (ClientData rec in recipents) {
                    response = server.CreateMessage();
                    response.Write((byte) PacketTypes.TEXTMESSAGE);
                    response.Write(clients[message.SenderEndPoint].ID);
                    response.Write(message.ReadString());

                    server.SendMessage(response, rec.Connection, NetDeliveryMethod.ReliableOrdered);
                }
                break;

            case 0x6:
                short selectorID = clients[message.SenderEndPoint].ID;
                clients[message.SenderEndPoint].Team = message.ReadInt32();
                //Add task for mainthread to spawn
                UnityMainThreadDispatcher.Instance().Enqueue(SpawnPlayer(clients[message.SenderEndPoint]));
                //TODO CHECK IF PLAYER CAN JOIN TEAM
                foreach (ClientData _client in clients.Values) {
                    response = server.CreateMessage();
                    response.Write((byte) PacketTypes.TEAMSELECT);
                    response.Write(selectorID);
                    response.Write(clients[message.SenderEndPoint].Team);
                    server.SendMessage(response, _client.Connection, NetDeliveryMethod.ReliableOrdered);
                }
                break;

        }
    }



    private void SendPlayerPosition () {
        foreach (ClientData _client in clients.Values) {
            foreach (ClientData allClients in clients.Values) {
                NetOutgoingMessage response = server.CreateMessage();
                response.Write((byte) PacketTypes.MOVE);
                response.Write(_client.ID);
                response.Write(_client.Position.x);
                response.Write(_client.Position.y);
                response.Write(_client.Position.z);

                response.Write(_client.Rotation.x);
                response.Write(_client.Rotation.y);
                response.Write(_client.Rotation.z);
                server.SendMessage(response, allClients.Connection, NetDeliveryMethod.UnreliableSequenced);
            }
        }
    }
}
