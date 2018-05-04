namespace Network.Ressources
{
    public class Ressource : NetworkObject
    {
        public RessourceType type;
        public float respawnTime;
        public float size;

        private Ressource(RessourceType type, float size, float respawnTime)
        {
            this.type = type;
            this.size = size;
            this.respawnTime = respawnTime;
            isMoveable = false;
        }
    }
}