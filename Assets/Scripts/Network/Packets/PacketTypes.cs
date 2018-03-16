namespace Network.Packets
{
    public enum PacketTypes
    {
        //BASIC TYPES
        CONNECTED = 0,
        DISCONNECTED = 1,

        PLAYERLIST = 2,
        NEW_CLIENT_CONNECTED = 3,

        MOVE = 4,
        DAMAGE = 5,
        ANIMATION = 6,
        TEXT_MESSAGE = 7,

        SPAWN_NETWORK_OBJECT = 8,
        UPDATE_NETWORK_OBJECT = 9,
        DESTROY_NETWORK_OBJECT = 10,

        TEAMSELECT = 11,
        RESPAWN_PLAYER = 12,
        WEAPONCHANGE = 13,

        //Worldevents
        DROP = 14,
        PICKUP = 15,

        //INTERACTIONS
        TREECUTTING = 16
    }
}