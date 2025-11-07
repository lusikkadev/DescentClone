
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BeamTracer : MonoBehaviour
{
    LineRenderer lr;

    [Tooltip("Local width of the beam (start and end)")]
    public float width = 0.06f;

    [Tooltip("Color of the beam")]
    public Color color = Color.cyan;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        ConfigureLineRendererDefaults();
    }

    void ConfigureLineRendererDefaults()
    {
        if (lr == null) return;

        // Core settings - assume beam points along local +Z
        lr.useWorldSpace = false;
        lr.alignment = LineAlignment.View; // visible from camera; change to Transform if you prefer
        lr.positionCount = 2;

        // default two points 0..1 along +Z (will be overwritten in Setup)
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, Vector3.forward);

        // width
        lr.startWidth = width;
        lr.endWidth = width;

        // color
        lr.startColor = color;
        lr.endColor = color;

        // recommended: assign an additive/unlit material in the prefab inspector (see instructions)
    }

    /// <summary>
    /// Setup the tracer to stretch from origin -> target and auto-destroy after lifetime.
    /// Call immediately after Instantiate.
    /// </summary>
    public void Setup(Vector3 origin, Vector3 target, float lifetime)
    {
        transform.position = origin;
        Vector3 dir = target - origin;
        float dist = Mathf.Max(0.001f, dir.magnitude);
        transform.rotation = Quaternion.LookRotation(dir.normalized);

        // Ensure LineRenderer exists
        if (lr == null) lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            // Draw from 0 .. dist along local +Z
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, Vector3.forward * dist);
        }

        if (lifetime > 0f)
            StartCoroutine(FadeAndDestroy(lifetime));
    }

    IEnumerator FadeAndDestroy(float lifetime)
    {
        float t = 0f;
        float fadeDuration = Mathf.Min(0.25f, lifetime * 0.5f); // fade out duration
        float visibleTime = Mathf.Max(0f, lifetime - fadeDuration);

        // keep visible for visibleTime
        yield return new WaitForSeconds(visibleTime);

        // fade
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(1f - (t / fadeDuration));
            if (lr != null)
            {
                Color c = color;
                c.a = a;
                lr.startColor = c;
                lr.endColor = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}