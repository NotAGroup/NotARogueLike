using UnityEngine;
using System;

public class FlickeringEffect : MonoBehaviour
{
    public Light[] targets;
    
    [Header("Parameters")]
    [Range(0f,1f)]
    public float flickerMin = 0.5f;
    [Range(1f,3f)]
    public float flickerMax = 1.5f;
    public float flickerReturnRate = 5f;

    [Header("flicker events")]
    [Tooltip("expected number of flicker events per second")]
    public float lambda = 0.1f;

    // intensities to apply the dynamic multiplier to
    private float[] baseIntensity;

    // internal state
    private float time = 0f;
    private float factor = 1f;

    void Start()
    {
        // store base intensities
        baseIntensity = new float[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            baseIntensity[i] = targets[i].intensity;
        }
        factor = 1f;
    }

    void Update()
    {
        // apply current intensities
        for (int i = 0; i < targets.Length && i < baseIntensity.Length; i++)
        {
            targets[i].intensity = baseIntensity[i] * factor;
        }

        // return factor towards 1
        float interpolationRate = flickerReturnRate * Time.deltaTime;
        factor = factor * (1 - interpolationRate) + interpolationRate;

        if (time <= 0f) {
            // flicker event occured, compute waiting time until next one
            time = Distributions.Exponential.Sample(lambda);

            // set new intensity factor by sampling something 
            // in expectation, sets factor to either 1 + (flickerMax - 1) / 2
            //                          or to either 1 - (1 - flickerMin) / 2
            float value = Distributions.Bates.Sample(0f,1f, 3);
            if (UnityEngine.Random.Range(0f, 1f) > 0.7)
            {
                factor = flickerMax * value;
            } 
            else
            {
                factor = 1f - (1f - flickerMin) * value;
            }
        }

        time -= Time.deltaTime;
    }
}
