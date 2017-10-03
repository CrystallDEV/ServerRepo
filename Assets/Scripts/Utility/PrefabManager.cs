using System.Collections.Generic;
using UnityEngine;

namespace Utility
{
    public enum PrefabTypes
    {
        STONE,
        TREE,
        ARROW,

        //items
        WOOD
    }

    public class PrefabManager : MonoBehaviour
    {
        public GameObject tree;
        public GameObject stone;
        public GameObject arrow;

        //items
        public GameObject Wood;


        public readonly Dictionary<int, GameObject> prefabs = new Dictionary<int, GameObject>();

        private void Start()
        {
            prefabs.Add((int) PrefabTypes.TREE, tree);
        }

        public GameObject getPrefabByID(int id)
        {
            return prefabs[id];
        }

        public void SpawnPrefab(short ID, int prefabID, Vector3 pos, Quaternion rot)
        {
            GameObject prefab = Instantiate(getPrefabByID(prefabID));
            prefab.transform.position = pos;
            prefab.transform.rotation = rot;
        }

        public GameObject GetPrefabByID(int ID)
        {
            return prefabs[ID];
        }
    }
}