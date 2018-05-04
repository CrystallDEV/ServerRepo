using UnityEngine;

namespace Network.Inventory
{
    public class InventoryController : MonoBehaviour
    {
        private static InventoryController instance;

        public static InventoryController getInstance()
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

        private void addItem()
        {
            
        }

        private void removeItem()
        {
            
        }

        private void hasSpace()
        {
            
        }
    }
}