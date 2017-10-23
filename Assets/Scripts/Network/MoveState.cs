using UnityEngine;

namespace Utility
{
    public class MoveState
    {
        public Vector3 pos;
        public Vector3 rot;

        public MoveState(Vector3 pos, Vector3 rot)
        {
            this.pos = pos;
            this.rot = rot;
        }
    }
}