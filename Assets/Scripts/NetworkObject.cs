using Lidgren.Network;
using System.Linq;
using UnityEngine;
using Utility;

public class NetworkObject : MonoBehaviour
{
    public PrefabTypes prefabType;
    
    public short ID { get; set; }

    public Vector3 LastPosition { get; protected set; }
    public Vector3 Position { get; protected set; }
    public Vector3 Rotation { get; protected set; }
    public bool isMoveable;

    private Server server;

    private void Awake()
    {
        server = GameObject.Find("Server").GetComponent<Server>();
    }

    private void Start()
    {
        ID = Server.GetFreeID();
        Debug.Log("PrefabID: " + ID);
        Server.netObjs.Add(ID, this);
        Position = transform.position;
        Rotation = transform.rotation.eulerAngles;
    }

    private void Update()
    {
        if (isMoveable)
            updatePosition();
    }

    private void updatePosition()
    {
        if (transform.position == LastPosition) return;
        LastPosition = transform.position;
        Position = transform.position;
        foreach (ClientData recipent in server.clients.Values.ToList())
        {
            NetOutgoingMessage response = server.server.CreateMessage();
            response.Write((byte) Server.PacketTypes.UPDATEPREFAB);
            response.Write(ID);
            response.Write(transform.position.x);
            response.Write(transform.position.y);
            response.Write(transform.position.z);

            response.Write(transform.rotation.x);
            response.Write(transform.rotation.y);
            response.Write(transform.rotation.z);
            server.server.SendMessage(response, recipent.Connection, NetDeliveryMethod.UnreliableSequenced);
        }
    }

    private void OnDestroy()
    {
        Server.netObjs.Remove(ID);
    }

    public int GetPrefabID
    {
        get { return (int) prefabType; }
    }
}