using Unity.VisualScripting;
using UnityEngine;

public class BossWeakSpot : MonoBehaviour, IDamageable
{
    public int health = 200;
    public bool isDestroyed = false;
    Component owner;

    public void RegisterOwner(Component b)
    {
        owner = b;
    }

    public void TakeDamage(int amount)
    {
        if (isDestroyed) return;
        health -= amount;
        if (health <= 0)
        {
            isDestroyed = true;
            // disable visuals / collider
            

            if (owner != null)
            {
                // notify owner; support typed method or SendMessage fallback
                var mi = owner.GetType().GetMethod("WeakSpotDestroyed");
                if (mi != null) mi.Invoke(owner, new object[] { this });
                else owner.SendMessage("WeakSpotDestroyed", this, SendMessageOptions.DontRequireReceiver);
                gameObject.SetActive(false);
            }
        }
    }
}
