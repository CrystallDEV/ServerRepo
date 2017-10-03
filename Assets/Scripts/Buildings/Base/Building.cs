using UnityEngine;

namespace Buildings.Base
{
    public abstract class Building : NetworkObject
    {
        public string PlayerOwner { get; set; }
        public float MaxHitpoints { get; set; }
        public float CurrentHitpoints { get; set; }
        public string BuildName { get; set; }
        public bool IsDestroyed { get; set; }
    
        public void Damage(float value)
        {
            if (IsDestroyed) return;
        
            if (CurrentHitpoints - value <= 0)
            {
                IsDestroyed = true;
                CurrentHitpoints = 0;
            }
            else
            {
                CurrentHitpoints -= value;
            }
        }

        public void Regenerate()
        {
            if (!IsDestroyed)
            {
                CurrentHitpoints += 1 * Time.deltaTime;
            }
        }
    }
}