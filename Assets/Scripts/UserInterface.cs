using UnityEngine;
using UnityEngine.UI;

public class UserInterface : MonoBehaviour
{
    // Components
    private Slider healthBar;
    private Slider manaBar;
    private Slider staminaBar;

    private Constitution playerConstitution;
    private PlayerStats playerStats;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerConstitution = GameObject.Find("Player").GetComponent<Constitution>();
        playerStats = GameObject.Find("Player").GetComponent<PlayerStats>();
        
        healthBar = GameObject.Find("Health Bar").GetComponent<Slider>();
        manaBar = GameObject.Find("Mana Bar").GetComponent<Slider>();
        staminaBar = GameObject.Find("Stamina Bar").GetComponent<Slider>();
    }

    // Update is called once per frame
    void Update()
    {
        healthBar.maxValue = playerStats.maxHealth;
        healthBar.value = playerConstitution.Health;
        manaBar.maxValue = playerStats.maxMana;
        manaBar.value = playerConstitution.Mana;
        staminaBar.maxValue = playerStats.maxStamina;
        staminaBar.value = playerConstitution.Stamina;
    }
}
