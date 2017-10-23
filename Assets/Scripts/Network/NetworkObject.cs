using Lidgren.Network;
using System.Linq;
using UnityEditor;
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

        if (!server.isStarted) return;

        var response = server.server.CreateMessage();
        response.Write((byte) PacketTypes.SPAWNPREFAB);
        response.Write(ID);
        response.Write(GetPrefabId);
        response.Write(Position.x);
        response.Write(Position.y);
        response.Write(Position.z);

        response.Write(Rotation.x);
        response.Write(Rotation.y);
        response.Write(Rotation.z);

        server.server.SendToAll(response, NetDeliveryMethod.ReliableUnordered);
    }


    private void Update()
    {
        if (isMoveable)
            UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (transform.position == LastPosition) return;
        LastPosition = transform.position;
        Position = transform.position;

        NetOutgoingMessage response = server.server.CreateMessage();
        response.Write((byte) PacketTypes.UPDATEPREFAB);
        response.Write(ID);
        response.Write(transform.position.x);
        response.Write(transform.position.y);
        response.Write(transform.position.z);

        response.Write(transform.rotation.x);
        response.Write(transform.rotation.y);
        response.Write(transform.rotation.z);
        server.server.SendToAll(response, NetDeliveryMethod.UnreliableSequenced);
    }

    private void OnDestroy()
    {
        Server.netObjs.Remove(ID);
        var response = server.server.CreateMessage();
        response.Write((byte) PacketTypes.DESTROYPREFAB);
        response.Write(ID);
        server.server.SendToAll(response, NetDeliveryMethod.ReliableUnordered);
    }

    public int GetPrefabId
    {
        get { return (int) prefabType; }
    }
}