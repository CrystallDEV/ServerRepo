using Buildings.Base;

public class Headquarter : Building {

    public Headquarter () {
        BuildName = "Headquarter";
        MaxHitpoints = 1000;
        CurrentHitpoints = MaxHitpoints;
        PlayerOwner = "";
    }

}
