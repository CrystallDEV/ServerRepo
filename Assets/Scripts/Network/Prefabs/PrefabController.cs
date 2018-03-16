using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Network.Prefabs
{
    public class PrefabController : MonoBehaviour
    {
        public GameObject tree;
        public GameObject stone;
        public GameObject arrow;

        //items
        public GameObject Wood;

        public readonly Dictionary<int, GameObject> prefabs = new Dictionary<int, GameObject>();
        private static PrefabController instance;

        public static PrefabController getInstance()
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

        private void Start()
        {
            prefabs.Add((int) PrefabTypes.TREE, tree);
        }

        public GameObject getPrefabById(int id)
        {
            return prefabs[id];
        }
        
        public GameObject getPrefabByType(PrefabTypes type)
        {
            return prefabs[(int) type];
        }
        
        public void SpawnPrefab(PrefabTypes type, Vector3 pos, Quaternion rot)
        {
            GameObject prefab = Instantiate(getPrefabByType(type));
            prefab.transform.position = pos;
            prefab.transform.rotation = rot;
        }

        public void SpawnPrefab(int prefabID, Vector3 pos, Quaternion rot)
        {
            GameObject prefab = Instantiate(getPrefabById(prefabID));
            prefab.transform.position = pos;
            prefab.transform.rotation = rot;
        }
    }
}