using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController)), RequireComponent(typeof(PlayerStats)), DisallowMultipleComponent]
public class Player : MonoBehaviour
{
    // Components
    private Transform cameraTransform;
    private CharacterController characterController;
    private InteractionArea interactArea;
    private Camera playerCamera;
    private Rigidbody rb;

    private Transform leftShoulderTransform;
    private Transform rightShoulderTransform;

    // Enumerations
    public enum AttackType
    {
        Hit,
        Shoot
    }

    enum HitState
    {
        Idle,
        Return,
        Swing,
        Strike
    }

    private AttackType attackType;
    private HitState hitState;

    private float gravity = -9.81f;

    private bool grounded;
    private bool run;
    private bool slide;
    private bool sneak;

    private Vector3 motion;
    private Vector3 scale;
    private float SprintAnimSpeed;

    private float currentFOV;

    private float defaultYScale;
    private float fireCooldown;

    private float hitCooldown;
    private float hitTime;

    private Vector3 restRotation = new Vector3(0.0f, 0.0f, 0.0f);
    private Vector3 swingRotation = new Vector3(-130.0f, 0.0f, 0.0f);
    private Vector3 strikeRotation = new Vector3(-30.0f, 0.0f, 0.0f);

    private Quaternion startRotation;
    private Quaternion endRotation;

    private float slideCooldown;
    private float slideTime;

    private Transform interactionTarget;
    private float interactionTimer = 0f;
    private float interactionTargetDuration = 0.2f;

    [Header("Camera")]
    public float normalFOV = 60.0f;
    public float aimFOV = 30.0f;
    public float zoomSpeed = 10.0f;

    [Header("Combat")]
    Vector3 localVelocity;
    Animator animator;
    public Projectile[] projectiles;
    public GameObject bow, sword;
    public PlayerHitZone hitZone;

    [Header("Interaction")]
    public float interactionDistance = 10f;

    private Inventory inventory;
    private Constitution constitution;
    private ItemDefinitions itemDefinitions;
    private PlayerStats stats;
    private Crosshair crosshair;

    public bool isDead = false;
    private bool opponentGotHit;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        inventory = GetComponent<Inventory>();
        constitution = GetComponent<Constitution>();
        crosshair = GameObject.Find("Canvas/Crosshair").GetComponent<Crosshair>();
        itemDefinitions = GameObject.Find("Definitions").GetComponent<ItemDefinitions>();

        GameObject mainCamera = GameObject.Find("Main Camera");

        playerCamera = mainCamera.GetComponent<Camera>();
        cameraTransform = mainCamera.GetComponent<Transform>();
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

        //leftShoulderTransform = GameObject.Find("Left Shoulder").GetComponent<Transform>();
        //rightShoulderTransform = GameObject.Find("Right Shoulder").GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        grounded = characterController.isGrounded;
        scale = transform.localScale;


        scale.y = slide ? defaultYScale * 0.5f : sneak ? defaultYScale * 0.75f : defaultYScale;
        transform.localScale = scale;
        SprintAnimSpeed = run ? 2f : 1f;
        animator.SetFloat("SprintAnimSpeed", SprintAnimSpeed);

        localVelocity = transform.InverseTransformDirection(rb.linearVelocity);

        float moveX = localVelocity.x;
        float moveZ = localVelocity.z;
        animator.SetFloat("MoveX", moveX, 0.1f, Time.deltaTime);
        animator.SetFloat("MoveZ", moveZ, 0.1f, Time.deltaTime);

        if (!grounded)
        {
            animator.SetBool("Jumping", true);
            motion.y += gravity * Time.deltaTime;
        } else
        {
            animator.SetBool("Jumping", false);
        }

        if (transform.position.y < -10 && !isDead)
        {
            Die();
        }

        if (fireCooldown > 0.0f)
        {
            fireCooldown -= Time.deltaTime;
            if (fireCooldown < 0.0f)
            {
                fireCooldown = 0.0f;
            }
        }

        if (hitCooldown > 0.0f)
        {
            hitCooldown -= Time.deltaTime;
            if (hitCooldown < 0.0f)
            {
                hitCooldown = 0.0f;
            }
        }

        if (hitState != HitState.Idle)
        {
            hitTime += Time.deltaTime;

            float duration = hitState == HitState.Swing ? stats.swingDuration :
                             hitState == HitState.Strike ? stats.strikeDuration : stats.returnDuration;
            duration /= stats.hitRate;

            float ratio = Mathf.Clamp(hitTime / duration, 0.0f, 1.0f);

            //rightShoulderTransform.localRotation = Quaternion.Slerp(startRotation, endRotation, ratio);

            if (ratio >= 1.0f)
            {
                switch (hitState)
                {
                    case HitState.Swing:
                        animator.SetTrigger("Swing");
                        hitState = HitState.Strike;
                        hitTime = 0.0f;

                        startRotation = Quaternion.Euler(swingRotation);
                        endRotation = Quaternion.Euler(strikeRotation);
                        break;
                    case HitState.Strike:
                        hitState = HitState.Return;
                        hitTime = 0.0f;

                        startRotation = Quaternion.Euler(strikeRotation);
                        endRotation = Quaternion.Euler(restRotation);
                        hitZone.gameObject.SetActive(true);
                        hitZone.SetDamage(stats.strikeDamage);
                        break;
                    case HitState.Return:
                        hitState = HitState.Idle;
                        hitCooldown = 1.0f / stats.hitRate;

                        hitZone.gameObject.SetActive(false);
                        opponentGotHit = false;
                        break;
                    default:
                        Debug.LogError("Unknown Hit State: " + hitState);
                        return;
                }
            }
        }

        if (slide)
        {
            slideTime -= Time.deltaTime;

            if (slideTime <= 0.0f)
            {
                // End slide
                slide = false;
                slideCooldown = 1.0f;
                slideTime = 0.0f;
            }
        }
        else if (slideCooldown > 0.0f)
        {
            slideCooldown -= Time.deltaTime;

            if (slideCooldown < 0.0f)
            {
                slideCooldown = 0.0f;
            }
        }

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, currentFOV, zoomSpeed * Time.deltaTime);

        if (characterController.enabled)
            characterController.Move(motion * Time.deltaTime);

        constitution.RegenerateHealth();
        constitution.RegenerateMana();
        bool idle = motion.x >= -0.1f && motion.x <= 0.1f && motion.y >= -0.1f && motion.y <= 0.1f && motion.z >= -0.1f && motion.z <= 0.1f;
        constitution.RegenerateStamina(idle);

        // give interaction hint that stays for a fixed amount of time
        interactionTimer -= Time.deltaTime;

        if (interactionTimer <= 0f) {
            interactionTarget = null;
            crosshair.Reset();
        }

        if (RaycastInteractible(out RaycastHit hit)) {
            if (hit.transform.gameObject.TryGetComponent<InteractionHint>(out InteractionHint hint)) {
                interactionTarget = hit.transform;
                interactionTimer = interactionTargetDuration;
                crosshair.SetInteractionText(hint.Text);
            }
        }
    }

    public void Aim(bool value)
    {
        if (value)
        {
            currentFOV = aimFOV;
        }
        else
        {
            currentFOV = normalFOV;
        }
    }

    public void Attack(AttackType? type = null)
    {
        // Use current attack type if no type is specified
        attackType = type ?? attackType;

        switch (attackType)
        {
            case AttackType.Hit:
                Hit();
                break;
            case AttackType.Shoot:
                Shoot();
                break;
            default:
                Debug.LogError("Unknown Attack Type: " + attackType);
                break;
        }
    }

    public void ChangeAttack()
    {
        if (attackType == AttackType.Hit)
        {
            attackType = AttackType.Shoot;
            sword.SetActive(false);
        }
        else
        {
            attackType = AttackType.Hit;
            sword.SetActive(true);
        }

        RunData.Instance.selectedAttack = attackType;
    }

    private void Die()
    {
        // TODO
        isDead = true;
        characterController.enabled = false;
        UIManager.Instance.SwitchToDeathScreen();
        RunData.Instance.NewRun();
        GameSaver.save();

    }

    public void StartGame()
    {
        attackType = AttackType.Shoot;
        hitState = HitState.Idle;
        sword.SetActive(false);

        fireCooldown = 0.0f;
        hitCooldown = 0.0f;
        hitTime = 0.0f;

        slideCooldown = 0.0f;
        slideTime = 0.0f;

        defaultYScale = transform.localScale.y;

        hitZone.gameObject.SetActive(false);
        opponentGotHit = false;

        attackType = RunData.Instance.selectedAttack;
        if(attackType == AttackType.Hit)
        {
            sword.SetActive(true);
        }

        isDead = false;
        characterController.enabled = true;
    }

    public float GetHealth()
    {
        return constitution.Health;
    }

    public float GetMana()
    {
        return constitution.Mana;
    }

    public float GetStamina()
    {
        return constitution.Stamina;
    }

    void Hit()
    {
        if (hitCooldown > 0.0f || hitState != HitState.Idle)
        {
            return;
        }

        hitState = HitState.Swing;
        hitTime = 0.0f;

        //startRotation = rightShoulderTransform.localRotation;
        endRotation = Quaternion.Euler(swingRotation);
    }

    // use raycast to find object for interaction
    public bool RaycastInteractible(out RaycastHit hit) {
        Vector3 position = cameraTransform.position + cameraTransform.forward * 0.1f;
        return Physics.Raycast(position, cameraTransform.forward, out hit, interactionDistance, ~LayerMask.GetMask("Player"));
    }

    // returns true on successfull interaction
    public bool InteractWith(Transform transform) {
        if (transform.gameObject.TryGetComponent<ShopItemSlot>(out ShopItemSlot slot)) {
            if (inventory.currency[Currency.Gold] < slot.item.cost) return false;
            inventory.currency[Currency.Gold] -= slot.item.cost;
            inventory.container.AddItem(slot.item, slot.count);
            slot.shop.RemoveItem(slot.Index);
            return true;
        } else if (transform.gameObject.TryGetComponent<DroppedItem>(out DroppedItem item)) {
            inventory.container.AddItem(item.item, item.count);
            item.Disable();
            return true;
        } else if (transform.gameObject.TryGetComponent<TrapDoor>(out TrapDoor door)){
            door.Interact();
            return true;
        }

        return false;
    }

    public void Interact()
    {
        if (interactionTarget != null) {
            InteractWith(interactionTarget);
            interactionTarget = null;
        }
        crosshair.Reset();
    }

    public bool isGrounded()
    {
        return grounded;
    }

    public bool isRunning()
    {
        return run;
    }

    public bool isSliding()
    {
        return slide;
    }

    public bool isSneaking()
    {
        return sneak;
    }

    public void Jump()
    {
        if (grounded && constitution.Stamina >= stats.jumpConsRate)
        {
            animator.SetBool("Jumping", true);
            constitution.Stamina -= stats.jumpConsRate;
            motion.y = Mathf.Sqrt(stats.jumpHeight * -2f * gravity);
        }
    }

    public void Move(Vector2 direction)
    {
        Vector3 movement = transform.right * direction.x + transform.forward * direction.y;
        movement.Normalize();

        float speed = stats.walkSpeed;

        if (run && constitution.Stamina >= stats.runConsRate)
        {
            constitution.Stamina -= stats.runConsRate * Time.deltaTime;
            speed = stats.runSpeed;

        }
        else if (sneak)
        {
            speed = stats.sneakSpeed;
        }

        if (slide)
        {
            speed += stats.slideSpeed * (slideTime / stats.maxSlideTime);
        }

        motion.x = movement.x * speed;
        motion.z = movement.z * speed;

        float ratio = Time.deltaTime * 5.0f;

        if (hitState == HitState.Idle && (direction.x != 0.0f || direction.y != 0.0f) && !slide)
        {
            float amplitude = 2.0f * speed;
            float frequency = 5.0f;

            float wave = Mathf.Sin(Time.time * frequency) * amplitude;

            //leftShoulderTransform.localRotation = Quaternion.Slerp(leftShoulderTransform.localRotation,
            //    Quaternion.Euler(wave, 0.0f, 0.0f), ratio);
            //rightShoulderTransform.localRotation = Quaternion.Slerp(rightShoulderTransform.localRotation,
            //    Quaternion.Euler(-wave, 0.0f, 0.0f), ratio);
        }
        else
        {
            //leftShoulderTransform.localRotation = Quaternion.Slerp(leftShoulderTransform.localRotation,
            //    Quaternion.Euler(0.0f, 0.0f, 0.0f), ratio);
            //rightShoulderTransform.localRotation = Quaternion.Slerp(rightShoulderTransform.localRotation,
            //    Quaternion.Euler(0.0f, 0.0f, 0.0f), ratio);
        }
    }

    public void Run(bool value)
    {
        if (value)
        {
            // Disable sneak when running
            sneak = false;
        }

        run = value;
    }

    public void Rotate(Vector2 rotation)
    {
        // Player
        transform.localRotation = Quaternion.AngleAxis(rotation.x, Vector3.up);

        // Camera
        float angle = Mathf.Clamp(rotation.y * 2, -90, 90.0f);
        cameraTransform.localRotation = Quaternion.AngleAxis(angle, Vector3.left);
    }

    void Shoot()
    {
        ItemContainer items = inventory.container;
        int bowAmmoSlot = items.GetSlotContaining(itemDefinitions[3], 1);

        Debug.Log("Bow Ammo Slot: " + bowAmmoSlot);
        items.PrintState();

        if (fireCooldown > 0.0f || projectiles.Length == 0 || bowAmmoSlot == -1)
        {
            return;
        }

        items.ConsumeItem(bowAmmoSlot);

        Vector3 position = cameraTransform.position + cameraTransform.forward * 1.0f;
        Quaternion rotation = cameraTransform.rotation;

        Projectile instance = Instantiate(projectiles[0], position, rotation);
        instance.name = projectiles[0].name;
        instance.SetDamage(stats.arrowDamage);
        instance.SetSpeed(stats.arrowSpeed);

        fireCooldown = 1.0f / stats.fireRate;
    }

    public void Slide(bool value)
    {
        if (value && constitution.Stamina >= stats.slideConsRate &&
            grounded && !slide && slideCooldown == 0.0f)
        {
            constitution.Stamina -= stats.slideConsRate;

            slide = true;
            slideTime = stats.maxSlideTime;
        }
        else if (!value && slide)
        {
            slide = false;
        }
    }

    public void Sneak(bool value)
    {
        if (value)
        {
            // Disable run when sneaking
            run = false;
        }

        sneak = value;
    }

    public void UseItem(int itemID)
    {
        ItemContainer inv = GetComponent<Inventory>().container;
        ItemSlot slot = inv[itemID];
        ItemDefinition item = slot.storedItem;
        int count = slot.count;

        if (item == null) return;

        switch (item.name)
        {
            case "health_fruit": // Health Fruit
                constitution.Health += 30.0f;
                constitution.Health = Mathf.Clamp(constitution.Health, 0.0f, stats.maxHealth);
                break;
            case "mana_fruit": // Mana Fruit
                constitution.Mana += 10.0f;
                constitution.Mana = Mathf.Clamp(constitution.Mana, 0.0f, stats.maxMana);
                break;
            case "stamina_fruit": // Stamina Fruit
                constitution.Stamina += 20.0f;
                constitution.Stamina = Mathf.Clamp(constitution.Stamina, 0.0f, stats.maxStamina);
                break;
            default:
                break;
        }

        inv.ConsumeItem(itemID);
    }

    public void TakeDamage(float damage)
    {
        constitution.TakeDamage(damage);

        if (constitution.Health <= 0.0f)
        {
            Die();
        }
    }

    public void SetOpponentGotHit(bool value)
    {
        opponentGotHit = value;
    }

    public bool GetOpponentGotHit()
    {
        return opponentGotHit;
    }
}
