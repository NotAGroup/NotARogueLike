using UnityEngine;
using UnityEngine.Events;

public class DestroyableObject : MonoBehaviour
{
    private float currentHealth;

    [Header("Health")]
    public float maxHealth = 50.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0.0f)
        {
            currentHealth = 0.0f;

            if (TryGetComponent<Rewards>(out Rewards rewards))
            {
                rewards.Drop();
            }

            Destroy(this.gameObject);
        }
    }
}
