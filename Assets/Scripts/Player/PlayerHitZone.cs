using System.ComponentModel;
using UnityEngine;

public class PlayerHitZone : HitZone<Player>
{
    protected override void DealDamage(GameObject other)
    {
        if (other.TryGetComponent<DestroyableObject>(out DestroyableObject destroyableObject))
        {
            Debug.Log("Zone dealing " + damage + " damage to " + other.gameObject.name);
            destroyableObject.TakeDamage(damage);
        }

        if (other.TryGetComponent<Opponent>(out Opponent opponent))
        {
            Debug.Log("Zone dealing " + damage + " damage to " + name);
            if (!owner.GetOpponentGotHit())
            {
                opponent.TakeDamage(damage);
                owner.SetOpponentGotHit(true);
            }
        }
    }

    protected override bool IsSelf(GameObject other)
    {
        string name = other.name;
        string tag = other.tag;

        return name == "Player" || tag == "Player";
    }
}
