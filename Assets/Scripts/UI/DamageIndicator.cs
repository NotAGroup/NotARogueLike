using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

public class DamageIndicator : MonoBehaviour
{
    private Constitution constitution;
    private Image indicator;
    private PlayerStats stats;

    private Color color;
    private float alpha;
    private float duration;
    private float timer = 0.0f;

    public float alphaFullHealth = 0.07f;
    public float alphaLowHealth = 0.16f;
    public float durationFullHealth = 0.3f;
    public float durationLowHealth = 0.9f;

    void Awake()
    {
        GameObject player = GameObject.Find("Player");
        constitution = player.GetComponent<Constitution>();
        stats = player.GetComponent<PlayerStats>();

        indicator = GetComponent<Image>();
        SetAlpha(0.0f);
    }

    void Update()
    {
        if (timer > 0.0f)
        {
            timer -= Time.deltaTime;
            SetAlpha(alpha * (timer / duration));

            if (timer <= 0.0f)
            {
                timer = 0.0f;
            }
        }
    }

    public void Flash()
    {
        float factor = Mathf.InverseLerp(1.0f, 0.25f, constitution.Health / stats.maxHealth);

        alpha = Mathf.Lerp(alphaFullHealth, alphaLowHealth, factor);
        duration = Mathf.Lerp(durationFullHealth, durationLowHealth, factor);

        SetAlpha(alpha);
        timer = duration;
    }

    private void SetAlpha(float alpha)
    {
        color = indicator.color;
        color.a = alpha;
        indicator.color = color;
    }
}
