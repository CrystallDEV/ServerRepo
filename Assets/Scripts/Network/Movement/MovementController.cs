using System;
using Network.Packets;
using UnityEngine;

namespace Network.Movement
{
    public class MovementController : MonoBehaviour
    {
        private static MovementController instance;

        public static MovementController getInstance()
        {
            return instance;
        }

        private void Awake()
        {
            //Check if instance already exists
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(gameObject);

            DontDestroyOnLoad(gameObject);
        }

        public void Jump(ClientData _client)
        {
            Transform player = Server.getInstance().clientsTransform[_client.ID];
            Vector3 vel = player.GetComponent<Rigidbody>().velocity;
            player.GetComponent<Rigidbody>().velocity = new Vector3(vel.x, 100, vel.z);
        }

        public void UpdatePlayerPosition(ClientData _client)
        {
            Vector3 dir;
            switch (_client.MoveDir)
            {
                case MoveDirs.UP:
                    dir = Vector3.forward;
                    break;
                case MoveDirs.UPRIGHT:
                    dir = Vector3.forward + Vector3.right;
                    break;
                case MoveDirs.RIGHT:
                    dir = Vector3.right;
                    break;
                case MoveDirs.RIGHTDOWN:
                    dir = Vector3.back + Vector3.right;
                    break;
                case MoveDirs.DOWN:
                    dir = Vector3.back;
                    break;
                case MoveDirs.DOWNLEFT:
                    dir = Vector3.back + Vector3.left;
                    break;
                case MoveDirs.LEFT:
                    dir = Vector3.left;
                    break;
                case MoveDirs.LEFTUP:
                    dir = Vector3.left + Vector3.forward;
                    break;
                case MoveDirs.NONE:
                    dir = Vector3.zero;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Transform player = Server.getInstance().clientsTransform[_client.ID];
            player.rotation = Quaternion.Euler(_client.Rotation);

            player.Translate(dir.normalized * _client.Speed * Time.deltaTime, Space.World);
            _client.Position = player.transform.position;

            if (_client.LastPosition == _client.Position) return;
            if (_client.WantsPredict)
            {
                _client.moveTime++;
            }

            _client.LastPosition = _client.Position;
            PacketController.getInstance().SendPlayerPosition(_client);
        }
    }
}