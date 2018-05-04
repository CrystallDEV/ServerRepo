using Lidgren.Network;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Network.Packets
{
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

        /// <summary>
        /// Sends an answer to a player, that is trying to connect
        /// </summary>
        /// <param name="_client"></param>
        public void SendClientConnectAnswer(ClientData _client)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Sent player connection answer to (" + _client.ID + ")");

            NetOutgoingMessage response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.CONNECTED);
            response.Write(_client.ID);
            response.Write((short) Server.getInstance().clients.Count); //Anzahl aktueller Clients senden
            Server.getInstance().server.SendMessage(response, _client.Connection, NetDeliveryMethod.ReliableUnordered);
        }

        /// <summary>
        /// Sends a message to all players, that a certain player disconnected
        /// </summary>
        /// <param name="_client"></param>
        public void SendClientDisconnect(ClientData _client)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Player disconnected: " + _client.UserName + "(" + _client.ID + ")");

            NetOutgoingMessage response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.DISCONNECTED); //0x01: Ein Client hat disconnected
            response.Write(_client.ID);
            Server.getInstance().server.SendToAll(response, NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        /// Sends a message to all players, that a new player connected
        /// </summary>
        /// <param name="_client"></param>
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

        /// <summary>
        /// Sends a network chat message to all player
        /// </summary>
        /// <param name="_client"></param>
        /// <param name="receiver"></param>
        /// <param name="message"></param>
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

        /// <summary>
        /// Sends an hp update to a player
        /// </summary>
        /// <param name="_client"></param>
        /// <param name="receiver"></param>
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

        /// <summary>
        /// Notifies the players, that a new player joined a team
        /// </summary>
        /// <param name="_client"></param>
        /// <param name="receiver"></param>
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

        /// <summary>
        /// Sends an update of a players postition to all other players
        /// </summary>
        /// <param name="_client"></param>
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

        /// <summary>
        /// Sends an update of a players animation state to all other players
        /// </summary>
        /// <param name="_client"></param>
        /// <param name="receiver"></param>
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

        /// <summary>
        /// Notifies all players, that a player respawned
        /// </summary>
        /// <param name="_client"></param>
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

        /// <summary>
        /// Sends a new player a list of the current players
        /// </summary>
        /// <param name="_client"></param>
        /// <param name="receiver"></param>
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

        /// <summary>
        /// Updates a position of a network object
        /// </summary>
        /// <param name="obj"></param>
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

        /// <summary>
        /// Notifies all player, that a network object got destroyed
        /// </summary>
        /// <param name="obj"></param>
        public void SendNetworkObjectDestroy(NetworkObject obj)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Destroy network object");

            NetOutgoingMessage response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.DESTROY_NETWORK_OBJECT);
            response.Write(obj.ID);
            Server.getInstance().server.SendToAll(response, NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        /// Updates the weapon state of a player
        /// </summary>
        /// <param name="_client"></param>
        public void SendWeaponChange(ClientData _client)
        {
            if (Server.getInstance().debugMode)
                Debug.Log("Update Weapon State (" + _client.ID + ")");

            NetOutgoingMessage response = Server.getInstance().server.CreateMessage();
            response.Write((byte) PacketTypes.WEAPONCHANGE);
            response.Write(_client.ID);
            response.Write((int) _client.WeaponState);
            Server.getInstance().server.SendToAll(response, NetDeliveryMethod.ReliableUnordered);
        }
    }

    #endregion
}