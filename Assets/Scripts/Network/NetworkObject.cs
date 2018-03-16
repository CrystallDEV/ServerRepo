using Network.Packets;
using UnityEngine;
using Utility;

namespace Network
{
    public class NetworkObject : MonoBehaviour
    {
        public PrefabTypes prefabType;

        public short ID { get; set; }

        public Vector3 LastPosition { get; protected set; }
        public Vector3 Position { get; protected set; }
        public Vector3 Rotation { get; protected set; }
        public bool isMoveable;


        private void Start()
        {
            ID = Utils.GetFreeID();
            Debug.Log("PrefabID: " + ID);
            Server.getInstance().netObjs.Add(ID, this);
            Position = transform.position;
            Rotation = transform.rotation.eulerAngles;

            if (!Server.getInstance().isStarted) return;
            PacketController.getInstance().SendNetworkObjectSpawn(this);
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

            PacketController.getInstance().SendNetworkObjectPosition(this);
        }

        private void OnDestroy()
        {
            Server.getInstance().netObjs.Remove(ID);
            PacketController.getInstance().SendNetworkObjectDestroy(this);
        }

        public int GetPrefabId
        {
            get { return (int) prefabType; }
        }
    }
}