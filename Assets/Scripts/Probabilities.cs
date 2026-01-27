using UnityEngine;

// see wikipedia for parameter explanations
public class Distributions 
{
    public class Exponential
    {
        public static float Sample(float lambda)
        {
            return -(float)Mathf.Log(UnityEngine.Random.Range(0f,1f)) / lambda; 
        }
    }

    public class Bates
    {
        public static float Sample(float min, float max, int n)
        {
            float result = 0f;
            for (int i = 0; i < n; i++) 
            {
                result += UnityEngine.Random.Range(min, max);
            }
            return result / n;
        }
    }
}

