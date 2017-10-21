using UnityEngine;

namespace Utility
{
    public class MoveState
    {
        public MoveDirs moveDir;
        public Vector3 pos;
        public Vector3 rot;

        public MoveState(MoveDirs moveDir, Vector3 pos, Vector3 rot)
        {
            this.moveDir = moveDir;
            this.pos = pos;
            this.rot = rot;
        }
    }
}