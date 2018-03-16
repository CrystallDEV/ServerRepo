using System.Collections.Generic;
using System.Linq;
using System.Net;
using Network;
using UnityEngine;
using Random = System.Random;

namespace Utility
{
    public class Utils
    {
        /// <summary>
        /// Tries to find a free ID for a NetworkObject
        /// </summary>
        /// <returns></returns>
        public static short GetFreeID()
        {
            List<int> usedIds = (from netOBJ in Server.getInstance().netObjs select netOBJ.Key).ToList();
            if (usedIds.Count == 0) return 0;

            for (short id = 0; id <= usedIds.Count; id++)
                if (!usedIds.Contains(id))
                    return id;

            return -1;
        }

        /// <summary>
        /// Determines if a client can interact with an object
        /// </summary>
        /// <param name="client"></param>
        /// <param name="targetPos"></param>
        /// <returns></returns>
        public static bool CanInteract(ClientData client, Vector3 targetPos)
        {
            if (client.IsDead) return false;

            return !(Vector3.Distance(client.Position, targetPos) > GameConstants.interactRange);
        }

        /// <summary>
        /// calculates a random drop location near an object
        /// </summary>
        /// <param name="netObjLoc"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static Vector3 CalculateDropLocation(Vector3 netObjLoc, int range)
        {
            Random ran = new Random();
            float x = netObjLoc.x + ran.Next(-range, range);
            float y = netObjLoc.y - 0.2f;
            float z = netObjLoc.z + ran.Next(-range, range);
            Debug.Log("Random drop location: (X: " + x + "Y: " + y + "Z: " + z + ")");
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Returns a list of clients that does not contain the given client
        /// </summary>
        /// <param name="_client"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        public static List<ClientData> GetOtherClients(ClientData _client, Dictionary<IPEndPoint, ClientData> others)
        {
            return (from client in others where client.Value.ID != _client.ID select client.Value).ToList();
        }
    }
}