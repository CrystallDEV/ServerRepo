using UnityEngine;

public class GameData {
    public GameData () {
        RoundTime = 0;
        TickRate = 0.01667f;
    }

    public float RoundTime { get; set; }
    public float TickRate { get; set; }
    //TODO: Make them configurable in any way (for example in the console etc.)
    public Vector3 TeamPos1 = new Vector3(-40, 5, 0);
    public Vector3 TeamPos2 = new Vector3(40, 5, 0);
}
