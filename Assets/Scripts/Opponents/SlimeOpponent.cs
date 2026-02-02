using System.Collections;
using UnityEngine;

public class SlimeOpponent : Opponent
{
    [Header("Combat")]
    public OpponentHitZone hitZone;

    [SerializeField] private float leapDuration = 0.55f;
    [SerializeField] private float leapHeight = 0.9f;
    [SerializeField] private float damageWindowStart = 0.10f;
    [SerializeField] private float damageWindowEnd = 0.38f;

    [Header("Chase")]
    [SerializeField] private float stopBeforePlayer = 1.0f;

    [Header("Launch")]
    [SerializeField] private float launchDistance = 1.4f;
    [SerializeField] private float launchDuration = 0.12f;

    [Header("Visual")]
    [SerializeField] private Renderer slimeRenderer;
    [SerializeField] private Color attackColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("Death")]
    [SerializeField] private Transform modelRoot;
    [SerializeField] private float deathDuration = 1.0f;
    [SerializeField] private float deathXZMultiplier = 2.0f;
    [SerializeField] private float deathYMultiplier = 0.2f;

    private MaterialPropertyBlock mpb;
    private Color normalBaseColor;
    private static readonly int ID_BaseColor = Shader.PropertyToID("_BaseColor");

    private bool aggroed;
    private float attackTimer = 0.0f;

    private Vector3 leapStart;
    private Vector3 leapTarget;

    private bool launching;
    private float launchTimer;
    private Vector3 launchDir;
    private Vector3 launchPrevApplied;

    protected override void Start()
    {
        base.Start();

        aggroed = false;

        if (hitZone != null)
        {
            hitZone.gameObject.SetActive(false);
            hitZone.SetDamage(stats.attackDamage);
        }

        if (slimeRenderer == null)
            slimeRenderer = GetComponentInChildren<Renderer>();

        if (slimeRenderer != null)
        {
            mpb = new MaterialPropertyBlock();
            if (slimeRenderer.sharedMaterial != null && slimeRenderer.sharedMaterial.HasProperty(ID_BaseColor))
                normalBaseColor = slimeRenderer.sharedMaterial.GetColor(ID_BaseColor);
            else
                normalBaseColor = Color.white;
        }

        if (modelRoot == null)
        {
            Transform t = transform.Find("ModelRoot");
            modelRoot = t != null ? t : transform;
        }

        navMeshAgent.updateRotation = false;
        navMeshAgent.updatePosition = true;
    }

    protected override void Update()
    {
        if (currentHealth <= 0.0f || playerTransform == null) return;

        if (!aggroed && CanSeePlayer())
            aggroed = true;

        if (launching)
            LaunchUpdate();

        if (attackCooldown > 0.0f)
        {
            attackCooldown -= Time.deltaTime;
            if (attackCooldown < 0.0f) attackCooldown = 0.0f;
        }

        if (float.IsNaN(attackCooldown) || float.IsInfinity(attackCooldown))
            attackCooldown = 0.0f;

        if (attacking)
        {
            AttackUpdate();
            return;
        }

        if (aggroed && !player.isDead)
            Combat();
        else
            Idle();

        velocity = transform.InverseTransformDirection(navMeshAgent.velocity);
    }

    private Vector3 GetChaseTarget()
    {
        Vector3 slimePos = transform.position;
        Vector3 playerPos = playerTransform.position;

        Vector3 toPlayer = playerPos - slimePos;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 1e-6f)
            return slimePos;

        toPlayer.Normalize();

        float d = Mathf.Max(0.1f, stopBeforePlayer);
        return playerPos - toPlayer * d;
    }


    public override void TakeDamage(float damage, Vector3? hitDirection = null)
    {
        if (currentHealth <= 0.0f) return;

        aggroed = true;

        if (attacking)
            StopAttackSoft();

        currentHealth -= damage;

        if (currentHealth <= 0.0f)
        {
            currentHealth = 0.0f;
            Die();
            return;
        }
        attackCooldown = 1.0f / Mathf.Max(0.0001f, stats.attackRate);


        Vector3 xz;

        if (hitDirection.HasValue)
        {
            Vector3 away = -hitDirection.Value;
            xz = new Vector3(away.x, 0f, away.z);
        }
        else
        {
            Vector3 away = transform.position - playerTransform.position;
            xz = new Vector3(away.x, 0f, away.z);
        }

        if (xz.sqrMagnitude > 1e-6f)
        {
            xz.Normalize();
            direction = xz;
            StartLaunch(xz);
        }
    }

    protected override bool CanSeePlayer()
    {
        if (playerTransform == null) return false;

        Vector3 toPlayer = playerTransform.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance > stats.detectionRange) return false;

        if (distance < 5.0f && !player.isSneaking())
            return true;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > viewAngle) return false;

        if (Physics.Raycast(transform.position, toPlayer.normalized, out RaycastHit hitInfo, stats.detectionRange))
            return hitInfo.transform.CompareTag("Player");

        return false;
    }

    protected override void Combat()
    {
        if (!navMeshAgent.enabled) return;
        if (!navMeshAgent.isOnNavMesh) return;

        navMeshAgent.isStopped = false;
        navMeshAgent.updateRotation = false;

        Vector3 chaseTarget = GetChaseTarget();
        navMeshAgent.SetDestination(chaseTarget);

        float distance = Vector3.Distance(playerTransform.position, transform.position);

        direction = (playerTransform.position - transform.position);
        direction.y = 0.0f;

        if (direction.sqrMagnitude > 1e-6f)
            RotateTowards(direction.normalized);

        if (attackCooldown <= 0.0001f && distance <= stats.attackRange)
            StartAttack();
    }

    protected override void Idle()
    {
        if (navMeshAgent.enabled)
            navMeshAgent.isStopped = true;

        RegenerateHealth();
    }

    protected override IEnumerator AttackRoutine()
    {
        yield return null;
    }

    private void StartAttack()
    {
        attacking = true;
        attackTimer = 0.0f;
        playerGotHit = false;

        navMeshAgent.isStopped = true;
        navMeshAgent.updatePosition = false;
        navMeshAgent.nextPosition = transform.position;

        // compute leap target
        Vector3 delta = (transform.position - playerTransform.position);
        float distance = GetComponent<CapsuleCollider>().radius + playerTransform.GetComponent<CapsuleCollider>().radius;
        leapStart = transform.position;
        leapTarget = playerTransform.position + delta.normalized * distance;
        leapTarget.y = leapStart.y;

        if (hitZone != null)
        {
            hitZone.SetDamage(stats.attackDamage);
            hitZone.gameObject.SetActive(false);
        }

        SetAttackTint(true);
    }

    private void StopAttackSoft()
    {
        if (hitZone != null)
            hitZone.gameObject.SetActive(false);

        attacking = false;

        navMeshAgent.updatePosition = true;
        navMeshAgent.isStopped = false;

        if (navMeshAgent.enabled)
            navMeshAgent.nextPosition = transform.position;

        SetAttackTint(false);
    }

    private void AttackUpdate()
    {
        attackTimer += Time.deltaTime;

        float t = Mathf.Clamp01(attackTimer / Mathf.Max(0.0001f, leapDuration));

        Vector3 pos = Vector3.Lerp(leapStart, leapTarget, t);
        float arc = 4f * t * (1f - t);
        pos.y = leapStart.y + arc * leapHeight;

        transform.position = pos;
        navMeshAgent.nextPosition = pos;

        Vector3 flat = (leapTarget - leapStart);
        flat.y = 0f;
        if (flat.sqrMagnitude > 1e-6f)
            RotateTowards(flat.normalized);

        bool inDamage = attackTimer >= damageWindowStart && attackTimer <= damageWindowEnd;
        if (hitZone != null)
            hitZone.gameObject.SetActive(inDamage);

        if (attackTimer >= leapDuration)
        {
            if (hitZone != null)
                hitZone.gameObject.SetActive(false);

            attacking = false;

            float rate = stats.attackRate;
            if (rate <= 0.0001f) rate = 1.0f;
            attackCooldown = 1.0f / rate;

            navMeshAgent.updatePosition = true;
            navMeshAgent.isStopped = false;
            navMeshAgent.nextPosition = transform.position;
            navMeshAgent.SetDestination(GetChaseTarget());

            SetAttackTint(false);
        }
    }

    private void StartLaunch(Vector3 dirXZ)
    {
        launching = true;
        launchTimer = 0f;
        launchDir = dirXZ;
        launchPrevApplied = Vector3.zero;
    }

    private void LaunchUpdate()
    {
        float dt = Time.deltaTime;
        launchTimer += dt;

        float dur = Mathf.Max(0.0001f, launchDuration);
        float t = Mathf.Clamp01(launchTimer / dur);
        float k = Mathf.SmoothStep(0f, 1f, t);

        Vector3 total = launchDir * (launchDistance * k);
        Vector3 delta = total - launchPrevApplied;
        launchPrevApplied = total;

        transform.position += delta;

        if (navMeshAgent != null && navMeshAgent.enabled)
            navMeshAgent.nextPosition = transform.position;

        if (launchTimer >= launchDuration)
        {
            launching = false;

            if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                navMeshAgent.SetDestination(GetChaseTarget());
        }
    }

    protected override void Die()
    {
        StopAllCoroutines();

        launching = false;
        attacking = false;

        if (hitZone != null)
            hitZone.gameObject.SetActive(false);

        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.isStopped = true;
            if (navMeshAgent.isOnNavMesh)
                navMeshAgent.Warp(transform.position);
            navMeshAgent.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        SetAttackTint(false);

        StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        if (modelRoot == null) modelRoot = transform;

        Vector3 start = modelRoot.localScale;
        Vector3 end = new Vector3(
            start.x * deathXZMultiplier,
            start.y * deathXZMultiplier,
            start.z * deathYMultiplier
        );

        float t = 0f;
        float dur = Mathf.Max(0.0001f, deathDuration);

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float kk = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            modelRoot.localScale = Vector3.Lerp(start, end, kk);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetAttackTint(bool on)
    {
        if (slimeRenderer == null) return;
        if (mpb == null) mpb = new MaterialPropertyBlock();

        slimeRenderer.GetPropertyBlock(mpb);

        Color baseCol = normalBaseColor;
        float alpha = baseCol.a;

        Color c = on ? attackColor : baseCol;
        c.a = alpha;

        mpb.SetColor(ID_BaseColor, c);
        slimeRenderer.SetPropertyBlock(mpb);
    }
}
