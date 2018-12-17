using UnityEngine;
using NoiseRings;

public class RepeatedNoiseRingSourceTrigger : MonoBehaviour
{
    NoiseRingSource source;
    float animationTime = 0.0f;

    void OnEnable ()
    {
        source = GetComponent<NoiseRingSource>();
        
        if (source != null)
            source.Trigger();
        else
            Debug.LogError("Must be attached to a GameObject with a NoiseRingSource component");
    }

    void Update ()
    {
        if (source == null)
            return;
        
        float animationDuration = source.AnimationDuration();

        animationTime += Time.deltaTime;

        while (animationTime >= animationDuration)
        {
            source.Trigger();
            animationTime -= animationDuration;
        }
    }
}
