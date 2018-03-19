﻿using Lidgren.Network;
using UnityEngine;

namespace Network.Packets
{
    //        Server| Packets
    //-------
    //0x00: Client connected 
    //0x01: Client disconnected
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
    //0x16: RespawnPlayer

    public class PacketController : MonoBehaviour
    {
        private static PacketController instance;

        private void Awake()
        {
            //Check if instance already exists
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(gameObject);

            DontDestroyOnLoad(gameObject);
        }

        public static PacketController getInstance()
        {
            return instance;
        }

        public void SendClientConnect(ClientData _client, NetConnection receiver)
        {
        }

        public void SendClientDisconnect(ClientData _client, NetConnection receiver)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Player disconnected: " + _client.UserName + "(" + _client.ID + ")");

            NetOutgoingMessage response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.DISCONNECTED); //0x01: Ein Client hat disconnected
            response.Write(_client.ID);
            Server.getInstance().server.SendMessage(response, receiver, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendNewClientConnected(ClientData _client)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Player connected");

            NetOutgoingMessage response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.NEW_CLIENT_CONNECTED);
            response.Write(_client.ID); //Seine ID mitteilen
            response.Write(_client.Position.x);
            response.Write(_client.Position.y);
            response.Write(_client.Position.z);

            response.Write(_client.Rotation.x);
            response.Write(_client.Rotation.y);
            response.Write(_client.Rotation.z);
            Server.getInstance().server.SendToAll(response, _client.Connection, NetDeliveryMethod.ReliableOrdered, 1);
        }

        public void SendChatMessage(ClientData _client, NetConnection receiver, string message)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Text message sent");

            NetOutgoingMessage response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.TEXT_MESSAGE);
            response.Write(_client.ID);
            response.Write(message);
            Server.getInstance().server.SendMessage(response, receiver, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendPlayerHPUpdate(ClientData _client, NetConnection receiver)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Damage message");

            NetOutgoingMessage response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.DAMAGE);
            response.Write(_client.ID);
            response.Write(_client.CurrentHitpoints);
            Server.getInstance().server.SendMessage(response, receiver, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendPlayerTeamJoin(ClientData _client, NetConnection receiver)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Team select message");

            NetOutgoingMessage response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.TEAMSELECT);
            response.Write(_client.ID);

            if (!_client.HasTeam())
                response.Write(-1);
            else
                response.Write(_client.Team.id);

            Server.getInstance().server.SendMessage(response, receiver, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendPlayerPosition(ClientData _client)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Move message");

            NetOutgoingMessage response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.MOVE);
            response.Write(_client.ID);
            response.Write(_client.Position.x);
            response.Write(_client.Position.y);
            response.Write(_client.Position.z);

            response.Write(_client.Rotation.x);
            response.Write(_client.Rotation.y);
            response.Write(_client.Rotation.z);
            response.Write(_client.moveTime);
            Server.getInstance().server.SendToAll(response, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendPlayerAnimationState(ClientData _client, NetConnection receiver)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Animation message sent");

            NetOutgoingMessage response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.ANIMATION);
            response.Write(_client.ID);
            response.Write((int) _client.AnimationState);
            Server.getInstance().server.SendMessage(response, receiver, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendPlayerRespawn(ClientData _client)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Player respawn message");

            NetOutgoingMessage response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.RESPAWN_PLAYER);
            response.Write(_client.ID);
            response.Write(_client.Position.x);
            response.Write(_client.Position.y);
            response.Write(_client.Position.z);

            response.Write(_client.Rotation.x);
            response.Write(_client.Rotation.y);
            response.Write(_client.Rotation.z);
            Server.getInstance().server.SendToAll(response, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendPlayerList(ClientData _client, NetConnection receiver)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Player list message");

            NetOutgoingMessage response = Server.getInstance().server.CreateMessage();
            Server server = Server.getInstance();

            response.Write((byte) PacketTypes.PLAYERLIST);
            response.Write(_client.ID);
            response.Write(_client.Position.x);
            response.Write(_client.Position.y);
            response.Write(_client.Position.z);

            response.Write(_client.Rotation.x);
            response.Write(_client.Rotation.y);
            response.Write(_client.Rotation.z);

            if (_client.HasTeam())
                response.Write(_client.Team.id);
            else
                response.Write(-1);

            server.server.SendMessage(response, receiver, NetDeliveryMethod.ReliableOrdered);
        }

        #region networkObject

        /// <summary>
        /// sends a network spawn message to the given client
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="receiver"></param>
        public void SendNetworkObjectSpawn(NetworkObject obj, NetConnection receiver)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Spawn network message");

            var response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.SPAWN_NETWORK_OBJECT);
            response.Write(obj.ID);
            response.Write(obj.GetPrefabId);
            response.Write(obj.Position.x);
            response.Write(obj.Position.y);
            response.Write(obj.Position.z);

            response.Write(obj.Rotation.x);
            response.Write(obj.Rotation.y);
            response.Write(obj.Rotation.z);
            Server.getInstance().server.SendMessage(response, receiver, NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        /// Sends a network spawn message to all clients
        /// </summary>
        /// <param name="obj"></param>
        public void SendNetworkObjectSpawn(NetworkObject obj)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Spawn network message");

            var response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.SPAWN_NETWORK_OBJECT);
            response.Write(obj.ID);
            response.Write(obj.GetPrefabId);
            response.Write(obj.Position.x);
            response.Write(obj.Position.y);
            response.Write(obj.Position.z);

            response.Write(obj.Rotation.x);
            response.Write(obj.Rotation.y);
            response.Write(obj.Rotation.z);
            Server.getInstance().server.SendToAll(response, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendNetworkObjectPosition(NetworkObject obj)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Update network object");

            NetOutgoingMessage response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.UPDATE_NETWORK_OBJECT);
            response.Write(obj.ID);
            response.Write(obj.Position.x);
            response.Write(obj.Position.y);
            response.Write(obj.Position.z);

            response.Write(obj.Rotation.x);
            response.Write(obj.Rotation.y);
            response.Write(obj.Rotation.z);
            Server.getInstance().server.SendToAll(response, NetDeliveryMethod.UnreliableSequenced);
        }

        public void SendNetworkObjectDestroy(NetworkObject obj)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Destroy network object");

            NetOutgoingMessage response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.DESTROY_NETWORK_OBJECT);
            response.Write(obj.ID);
            Server.getInstance().server.SendToAll(response, NetDeliveryMethod.ReliableOrdered);
        }
    }

    #endregion
}