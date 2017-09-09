using Lidgren.Network;
using System.Linq;
using UnityEngine;

public enum PrefabIDs {
    STONE,
    TREE,
    ARROW   
}
public class NetworkObject : MonoBehaviour {

    private short ID;

    [SerializeField]
    private PrefabIDs prefabID;

    private Vector3 lastPosition;
    private Vector3 position;

    Server server;

    private void Awake () {
        server = GameObject.Find("Server").GetComponent<Server>();
    }

    private void Start () {
        ID = Server.GetFreeID();
        Server.netObjs.Add(this);
    }

    private void Update () {
        lastPosition = position;
        position = transform.position;
        if (position != transform.position) {
            foreach (ClientData recipent in server.clients.Values.ToList()) {
                NetOutgoingMessage response = server.server.CreateMessage();
                response.Write((byte) Server.PacketTypes.NETOBJ);
                response.Write(GetID);
                response.Write(transform.position.x);
                response.Write(transform.position.y);
                response.Write(transform.position.z);

                response.Write(transform.rotation.x);
                response.Write(transform.rotation.y);
                response.Write(transform.rotation.z);
                server.server.SendMessage(response, recipent.Connection, NetDeliveryMethod.UnreliableSequenced);
            }
        }
    }

    private void OnDestroy () {
        Server.netObjs.Remove(this);
    }


    public short GetID {
        get { return ID; }
    }

    public PrefabIDs GetPrefabID {
        get { return prefabID; }
    }
}
