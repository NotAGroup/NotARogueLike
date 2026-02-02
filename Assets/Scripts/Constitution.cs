using UnityEngine;
using System;

[RequireComponent(typeof(PlayerStats)), DisallowMultipleComponent]
public class Constitution : MonoBehaviour
{
    private ConstitutionState state;
    private PlayerStats stats;

    public float Health { get => state.health; set => state.health = value; }
    public float Stamina { get => state.stamina; set => state.stamina = value; }
    public float Mana { get => state.mana; set => state.mana = value; }

    void Start()
    {
        stats = GetComponent<PlayerStats>();

        state = RunData.Instance.constitution;
    }

    public void Reset() 
    {
        state.health = stats.maxHealth;
        state.mana = stats.maxMana;
        state.stamina = stats.maxStamina;
    }

    public void RegenerateHealth()
    {
        if (state.health < stats.maxHealth)
        {
            float health = state.health + stats.healthRegRate * Time.deltaTime;
            state.health = Mathf.Min(health, stats.maxHealth);
        }
    }

    public void RegenerateMana()
    {
        if (state.mana < stats.maxMana)
        {
            float mana = state.mana + stats.manaRegRate * Time.deltaTime;
            state.mana = Mathf.Min(mana, stats.maxMana);
        }
    }

    public void RegenerateStamina(bool idle)
    {
        if (state.stamina < stats.maxStamina)
        {
            float rate = stats.staminaRegRate;

            if (idle)
            {
                // Increase regeneration rate when idle
                rate *= 1.5f;
            }

            float stamina = state.stamina + rate * Time.deltaTime;
            state.stamina = Mathf.Min(stamina, stats.maxStamina);
        }
    }

    public void TakeDamage(float damage)
    {
        state.health -= damage;
        if (state.health < 0f)
            state.health = 0f;
    }
}


[Serializable]
public class ConstitutionState {
    public float health;
    public float stamina;
    public float mana;
}

