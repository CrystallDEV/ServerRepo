using System.Collections.Generic;
using UnityEngine;

namespace Network.Teams
{
    public class Blue : Team
    {
        public Blue(int id)
        {
            this.id = id;
            maxPlayers = 10;
            spawn = new Vector3(10, 1, 10);
            name = "Blue Team";
            players = new List<ClientData>();
        }
    }
}