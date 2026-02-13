using UnityEngine;
using System;

public class FlickeringEffect : MonoBehaviour
{
    public Light[] targets;
    
    [Header("Parameters")]
    [Range(0f,1f)]
    [Tooltip("intensity will vary by a factor between e^(+/- flickerAmount)")]
    public float flickerAmount = 0.3f;
    [Header("Mass-Spring-Damper-like return of intensity")]
    public float flickerMass = 0.05f;
    public float flickerDampening = 0.4f;

    [Header("flicker events")]
    [Tooltip("expected number of flicker events per second")]
    public float lambda = 0.5f;

    // intensities to apply the dynamic multiplier to
    private float[] baseIntensity;

    // internal state
    private float time = 0f;
    private float factor = 1f;
    private float dfactor = 1f;
    private float ddfactor = 1f;

    void OnEnable()
    {
        // store base intensities
        baseIntensity = new float[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            baseIntensity[i] = targets[i].intensity;
        }
        factor = 0f;
        dfactor = 0f;
    }

    void OnDisable()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            targets[i].intensity = baseIntensity[i];
        }
    }

    void FixedUpdate()
    {
        Simulate(Time.fixedDeltaTime);
    }

    void Simulate(float delta)
    {
        if (factor * factor + dfactor * dfactor >= 0.00001f)
        {
            // apply current intensities
            for (int i = 0; i < targets.Length && i < baseIntensity.Length; i++)
            {
                targets[i].intensity = baseIntensity[i] * (1f + factor);
            }

            // simulate spring-mass-damper
            ddfactor = (-factor - flickerDampening * dfactor) / flickerMass;
            dfactor += ddfactor * delta;
            factor  += dfactor;
        }

        time -= delta;
        if (time <= 0f) {
            // flicker event occured, compute waiting time until next one
            time = Distributions.Exponential.Sample(lambda);

            // set new intensity factor by sampling 
            float value = Mathf.Exp(UnityEngine.Random.Range(-1f,1f) * flickerAmount);
            factor = flickerAmount * value;
        }
    }
}
