using UnityEngine;

[RequireComponent(typeof(BoxCollider)), RequireComponent(typeof(ParticleSystem))]
public class FireBreath : MonoBehaviour
{
    private BoxCollider col;
    private ParticleSystem ps;

    private Player player;

    private Vector3 startCenter, startSize;

    private float damage = 20.0f;
    private float elapsedTime = 0.0f;

    private void Awake()
    {
        col = GetComponent<BoxCollider>();
        ps = GetComponent<ParticleSystem>();

        col.isTrigger = true;
        col.enabled = false;

        startCenter = col.center;
        startSize = col.size;
    }

    private void Update()
    {
        if (ps.isPlaying)
        {
            col.enabled = true;
            elapsedTime += Time.deltaTime;

            UpdateCollider();

            if (player != null)
            {
                player.TakeDamage(damage * Time.deltaTime);
            }
        }
        else
        {
            col.enabled = false;
            col.center = startCenter;
            col.size = startSize;

            elapsedTime = 0.0f;
        }
    }

    private void UpdateCollider()
    {
        ParticleSystem.MainModule main = ps.main;
        ParticleSystem.ShapeModule shape = ps.shape;

        float maxLength = main.startLifetime.constant * main.startSpeed.constant;

        float length = Mathf.Min(elapsedTime * main.startSpeed.constant, maxLength);
        float width = 2.0f * shape.radius;

        col.size = new Vector3 (width, width, length);
        col.center = new Vector3(0.0f, 0.0f, length * 0.5f);
    }

    public void SetDamage(float damage)
    {
        this.damage = damage;
    }

    private void OnTriggerEnter(Collider other)
    {
        other.TryGetComponent<Player>(out player);
    }

    private void OnTriggerExit(Collider other)
    {
        string name = other.gameObject.name;
        string tag = other.gameObject.tag;

        if (player != null && (name == "Player" || tag == "Player"))
        {
            player = null;
        }
    }
}
