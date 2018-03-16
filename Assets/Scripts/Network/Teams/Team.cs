using System.Collections.Generic;
using UnityEngine;

namespace Network.Teams
{
    /// <summary>
    /// Represents a team that player can join
    /// </summary>
    public class Team
    {
        public int id;
        public int maxPlayers;
        public int points;
        public string name;
        public Vector3 spawn;

        public List<ClientData> players;

        public bool AddPlayer(ClientData _client)
        {
            if (!players.Contains(_client))
            {
                players.Add(_client);
                return true;
            }
            return false;
        }

        public bool RemovePlayer(ClientData _client)
        {
            if (players.Contains(_client))
            {
                players.Remove(_client);
                return true;
            }
            return false;
        }
    }
}