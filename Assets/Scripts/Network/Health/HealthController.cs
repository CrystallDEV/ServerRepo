using UnityEngine;
using Utility;

namespace Network.Health
{
    public class HealthController : MonoBehaviour
    {
        private static HealthController instance;

        public static HealthController getInstance()
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

        public void CalculatePlayerHitpoints(ClientData attacker, ClientData defender)
        {
            //TODO -> Test for different options, if the attack is possible etc. , maybe new Class / method
            if (Server.getInstance().clientsTransform[defender.ID] == null)
            {
                Debug.Log(attacker.ID + " tried to attack a transform that doesn't exist");
                return;
            }
            if (Vector3.Distance(attacker.Position, defender.Position) > 10)
            {
                Debug.Log(attacker.ID + " tried to hit another player(" + defender.ID + "), who is out of range");
                return;
            }
            if (attacker.Team == defender.Team)
            {
                Debug.Log("Player: " + attacker.ID + " cant hit player: " + defender.ID +
                          " because they are in the same team");
                return;
            }

            Debug.Log("Player " + attacker.ID + " damaged Player " + defender.ID + " for 10 DMG");
            if (defender.CurrentHitpoints - 10 < 0)
            {
                defender.IsDead = true;
                defender.CurrentHitpoints = 0;
                defender.DeathTime = GameConstants.deathTime;
                KillPlayer(defender);
                Debug.Log("Player: " + defender.ID + " has died.");
            }
            else
            {
                defender.CurrentHitpoints -= 10;
            }
        }

        public void KillPlayer(ClientData _client)
        {
            Transform player = Server.getInstance().clientsTransform[_client.ID];
            Server.getInstance().clientsTransform.Remove(_client.ID);
            Destroy(player.gameObject);
        }
    }
}