using UnityEngine;

public abstract class Building : NetworkObject {

    private string buildName;
    private float currentHitpoints;
    private float maxHitpoints;
    private string playerOwner;
    private bool isDestroyed;

    public string PlayerOwner {
        get {
            return playerOwner;
        }

        set {
            playerOwner = value;
        }
    }

    public float MaxHitpoints {
        get {
            return maxHitpoints;
        }

        set {
            maxHitpoints = value;
        }
    }

    public float CurrentHitpoints {
        get {
            return currentHitpoints;
        }

        set {
            currentHitpoints = value;
        }
    }

    public string BuildName {
        get {
            return buildName;
        }

        set {
            buildName = value;
        }
    }

    public bool IsDestroyed {
        get {
            return isDestroyed;
        }

        set {
            isDestroyed = value;
        }
    }

    public void Damage (float value) {
        if (!IsDestroyed) {
            if (currentHitpoints - value <= 0) {
                isDestroyed = true;
                currentHitpoints = 0;
            } else {
                currentHitpoints -= value;
            }
        }
    }

    public void Regenerate () {
        if (!isDestroyed) {
            currentHitpoints += 1 * Time.deltaTime;
        }
    }
}
