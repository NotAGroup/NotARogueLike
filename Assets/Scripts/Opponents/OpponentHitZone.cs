using UnityEngine;

public class OpponentHitZone : HitZone<Opponent>
{
    protected override void DealDamage(GameObject other)
    {
        if (other.TryGetComponent<Player>(out Player player))
        {
            Debug.Log("Zone dealing " + damage + " damage to " + name);
            if (!owner.playerGotHit)
            {
                player.TakeDamage(damage);
                owner.playerGotHit = true;
                if(owner.GetType() == typeof(MeleeSkeleton))
                {
                    MeleeSkeleton meleeSkeleton = (MeleeSkeleton)owner;
                    meleeSkeleton.audioSource.Play();
                }
            }
        }
    }

    protected override bool IsSelf(GameObject other)
    {
        string name = other.name;
        string tag = other.tag;

        return name == "Opponent" || tag == "Opponent";
    }
}
