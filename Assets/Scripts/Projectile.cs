using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Progress;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    // Components
    private Rigidbody rigidBody;

    public GameObject droppedItemPrefab;
    public AudioClip arrowHitSound;

    private ItemDefinitions itemDefinitions;
    private ItemDefinition bowAmmo;
    Vector3 travelDirection;
    Vector3 lastPosition;

    private float damage;
    private bool fired = false;
    private string ignoreTag;

    [Header("Properties")]
    public float lifetime;
    public Vector3 tipPosition;

    void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.useGravity = true; // TODO: Make this projectile-specific

        itemDefinitions = GameObject.Find("Definitions").GetComponent<ItemDefinitions>();
        bowAmmo = itemDefinitions.definitions[3];
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!fired)
        {
            return;
        }

        if (transform.position.y < -10.0f)
        {
            Destroy(this.gameObject);
        }

        travelDirection = rigidBody.linearVelocity.normalized;
        transform.rotation = Quaternion.LookRotation(travelDirection);

        if (Physics.Raycast(lastPosition, travelDirection, out RaycastHit hit, 1.2f * (transform.position - lastPosition).magnitude)) {
            OnHit(hit);
        }

        lastPosition = transform.position;
    }

    private void OnBecameInvisible()
    {
        // Destroy the projectile when it goes off-screen
        Destroy(this.gameObject);
    }

    private void OnHit(RaycastHit hit)
    {
        GameObject hitObject = hit.transform.gameObject;
        string name = hitObject.name;
        string tag = hitObject.tag;

        AudioSource.PlayClipAtPoint(arrowHitSound, transform.position);

        if (tag.Equals(ignoreTag)) {
            return;
        }

        Debug.Log("Projectile hit: " + name);

        GameObject droppedInstance = null;

        // if destroyable, deal damage
        if (hit.transform.TryGetComponent<DestroyableObject>(out DestroyableObject destroyableObject)) {
            Debug.Log("Projectile dealing " + damage + " damage to " + name);
            destroyableObject.TakeDamage(damage);
        }

        // deal damage to player
        if (hit.transform.TryGetComponent<Player>(out Player player))
        {
            Debug.Log("Projectile dealing " + damage + " damage to " + name);
            player.TakeDamage(damage);

            Destroy(this.gameObject);
            return;
        }

        // deal damage to opponent
        if (hit.transform.TryGetComponent<Opponent>(out Opponent opponent))
        {
            Animator animator = opponent.GetComponentInChildren<Animator>();

            if (animator != null) {
                Transform head = animator.GetBoneTransform(HumanBodyBones.Head);

                if (Vector3.Distance(hit.point, head.position) <= 0.75f) {
                    Debug.Log("Headshot!");
                    damage *= 1.25f; // headshot multiplier
                }
            }

            if (hit.transform.GetComponentInParent<SlimeOpponent>() != null)
            {
                damage *= 2f;
                Debug.Log("Slime hit! Arrow damage x2");
            }

            Debug.Log("Projectile dealing " + damage + " damage to " + name);
            opponent.TakeDamage(damage, new Vector3(-travelDirection.x, 0.0f, -travelDirection.z));
        }

        // arrow can stick to opponent, so its position has be computed appropriately
        if (tag.Equals("Opponent"))
        {
            Vector3 diffToOrthPlane = hit.transform.position - hit.point;
            diffToOrthPlane.y = 0f;
            Vector3 point = hit.point + travelDirection.normalized * Vector3.Dot(travelDirection.normalized, diffToOrthPlane);
            droppedInstance = Instantiate(droppedItemPrefab, point, transform.GetChild(0).rotation);
            droppedInstance.GetComponent<DroppedItem>().SetItem(bowAmmo, 1);
            droppedInstance.name = bowAmmo.name;
        }
        else if (ignoreTag == "Player")
        {
            float depthFactor = 0.5f; // 1 means the arrow does not go into the hit object, -1 means it fully goes in
            droppedInstance = Instantiate(droppedItemPrefab, hit.point - transform.TransformVector(tipPosition * depthFactor), transform.GetChild(0).rotation);
            droppedInstance.GetComponent<DroppedItem>().SetItem(bowAmmo, 1);
            droppedInstance.name = bowAmmo.name;
        }

        if (droppedInstance != null) {
            if (hitObject.TryGetComponent<AttachedObjects>(out AttachedObjects a)) {
                a.Attach(droppedInstance.transform);
            } else {
                // if no AttachedObjects component, stick to objects that are not rigidbodies
                var rb = droppedInstance.GetComponent<Rigidbody>();
                rb.isKinematic = !hitObject.TryGetComponent<Rigidbody>(out Rigidbody _);
            }

            Destroy(this.gameObject);
        }
    }

    public void SetDamage(float damage)
    {
        this.damage = damage;
    }

    public void Shoot(Vector3 direction, string ignoreTag, float speed)
    {
        this.ignoreTag = ignoreTag;

        rigidBody.AddForce(direction * speed, ForceMode.Impulse);
        lastPosition = transform.position;

        fired = true;

        // move one frame ahead already
        travelDirection = rigidBody.linearVelocity.normalized;
        transform.rotation = Quaternion.LookRotation(travelDirection);
        transform.position += travelDirection * Time.deltaTime;

        // Destroy the projectile after its lifetime expires
        Destroy(this.gameObject, lifetime);
    }
}
