using UnityEngine;

public abstract class HitZone<Owner> : MonoBehaviour
    where Owner : MonoBehaviour
{
    protected float damage;
    protected Owner owner;

    protected virtual void Awake()
    {
        owner = GetComponentInParent<Owner>();
    }
    protected virtual void OnTriggerEnter(Collider other)
    {
        GameObject otherObject = other.gameObject;

        if (IsSelf(otherObject))
        {
            // Ignore self triggers
            return;
        }
        
        Debug.Log($"{GetType().Name} hit: {otherObject.name}");

        DealDamage(otherObject);
    }

    protected abstract void DealDamage(GameObject other);
    protected abstract bool IsSelf(GameObject other);

    public void SetDamage(float damage)
    {
        this.damage = damage;
    }
}
