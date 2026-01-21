using UnityEngine;



/// stores player stats depending on player upgrades and items.
[RequireComponent(typeof(Inventory)), RequireComponent(typeof(PlayerUpgrades)), DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    private StatScalingDefinitions def;
    private PlayerUpgrades upgrades;
    private Inventory inventory;

    public void Awake() {
        def = GameObject.Find("Definitions").GetComponent<StatScalingDefinitions>();
        upgrades = GetComponent<PlayerUpgrades>();
        inventory = GetComponent<Inventory>();
    }

    // 
    public float Compute(StatKey stat) {
        float value = def[stat].ComputeFrom(upgrades.levels);

        foreach (var slot in inventory.container.slots)
        {
            if (slot.storedItem == null) continue;
            var buff = slot.storedItem.itemBuffs[stat];
            if (buff == null) continue;

            switch (buff.operation) {
                case ScalingFormula.Operation.Addition:
                    value += buff.value;
                    break;
                case ScalingFormula.Operation.Multiplication:
                    value *= buff.value;
                    break;
            }
        }

        return value;
    }

    public static float ComputeInitial(StatKey stat, StatScalingDefinitions def, PlayerUpgradeState upgrades) {
        return def[stat].ComputeFrom(upgrades);
    }

    // should be called whenever player upgrades or inventory (or anything influencing stats) is modified
    public void Recompute() {
        fireRate = Compute(StatKey.FireRate); 
        hitRate = Compute(StatKey.HitRate); 
        swingDuration = Compute(StatKey.SwingDuration); 
        strikeDuration = Compute(StatKey.StrikeDuration); 
        strikeDamage = Compute(StatKey.StrikeDamage);
        returnDuration = Compute(StatKey.ReturnDuration); 
        arrowDamage = Compute(StatKey.ArrowDamage);
        arrowSpeed = Compute(StatKey.ArrowSpeed);

        maxMana = Compute(StatKey.MaxMana); 
        manaRegRate = Compute(StatKey.ManaRegRate); 

        runSpeed = Compute(StatKey.RunSpeed); 
        walkSpeed = Compute(StatKey.WalkSpeed); 
        sneakSpeed = Compute(StatKey.SneakSpeed); 

        maxSlideTime = Compute(StatKey.MaxSlideTime); 
        slideSpeed = Compute(StatKey.SlideSpeed); 

        maxHealth = Compute(StatKey.MaxHealth); 
        healthRegRate = Compute(StatKey.HealthRegRate); 

        jumpHeight = Compute(StatKey.JumpHeight); 

        maxStamina = Compute(StatKey.MaxStamina); 
        staminaRegRate = Compute(StatKey.StaminaRegRate); 
        runConsRate = Compute(StatKey.RunConsRate); 
        slideConsRate = Compute(StatKey.SlideConsRate); 
        jumpConsRate = Compute(StatKey.JumpConsRate); 
    }

    // conventient accessors for stats
    [Header("Combat")]
    public float fireRate { get; private set; }
    public float hitRate { get; private set; }
    public float swingDuration { get; private set; }
    public float strikeDuration { get; private set; }
    public float strikeDamage { get; private set; }
    public float returnDuration { get; private set; }
    public float arrowDamage { get; private set; }
    public float arrowSpeed { get; private set; }

    [Header("Mana")]
    public float maxMana { get; private set; }
    public float manaRegRate { get; private set; }

    [Header("Movement")]
    public float runSpeed { get; private set; }
    public float walkSpeed { get; private set; }
    public float sneakSpeed { get; private set; }

    [Header("Sliding")]
    public float maxSlideTime { get; private set; }
    public float slideSpeed { get; private set; }

    [Header("Health")]
    public float maxHealth { get; private set; }
    public float healthRegRate { get; private set; }

    [Header("Jump")]
    public float jumpHeight { get; private set; }

    [Header("Stamina")]
    public float maxStamina { get; private set; }
    public float staminaRegRate { get; private set; }
    public float runConsRate { get; private set; }
    public float slideConsRate { get; private set; }
    public float jumpConsRate { get; private set; }
}
