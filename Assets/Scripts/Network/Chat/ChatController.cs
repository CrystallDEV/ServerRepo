using System;
using UnityEngine;

namespace Network
{
    public class ChatController : MonoBehaviour
    {
        private static ChatController instance;

        public static ChatController getInstance()
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

        public void CheckChatMessage()
        {
            //TODO check message for invalid characters / swear words etc.
        }

        public void SendChatMessage(string message)
        {
            //TODO send the message to all other players 
        }

        public void ExecuteCommand()
        {
            //TODO add the possibility to add commands to the chat
        }
    }
}