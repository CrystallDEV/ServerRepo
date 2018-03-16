using System.Collections.Generic;
using UnityEngine;

namespace Network.Teams
{
    public class Red : Team
    {
        public Red(int id)
        {
            this.id = id;
            maxPlayers = 10;
            spawn = new Vector3(-10, 1, -10);
            name = "Read Team";
            players = new List<ClientData>();
        }
    }
}