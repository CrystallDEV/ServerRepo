using System.Collections.Generic;
using UnityEngine;

namespace Network.Teams
{
    public class Spectator : Team
    {
        public Spectator(int id)
        {
            this.id = id;
            maxPlayers = 999;
            spawn = new Vector3(10, 20, 10);
            name = "Spectator";
            players = new List<ClientData>();
        }
    }
}