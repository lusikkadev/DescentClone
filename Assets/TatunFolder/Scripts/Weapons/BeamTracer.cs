using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BeamTracer : MonoBehaviour
{
    LineRenderer lr;
    [SerializeField] Transform muzzle;

    [Tooltip("Local width of the beam (start and end)")]
    public float width = 0.06f;

    [Tooltip("Color of the beam")]
    public Color color = Color.cyan;

    [Header("Spline Settings")]
    [Tooltip("Number of points used to render the spline")]
    [Range(2, 64)]
    public int splineResolution = 16;

    [Tooltip("Vertical offset for the spline control point (arc height)")]
    [Range(-5f, 5f)]
    public float controlOffset = 2f;

    [Tooltip("Direction of the spline arc (normalized vector, e.g. (0,1,0) for up)")]
    public Vector3 controlDirection = Vector3.up;

    [Header("Randomized Spline")]
    [Tooltip("Enable randomization of the spline control point")]
    public bool useRandomSpline = false;

    [Tooltip("Maximum random offset for the control point")]
    [Range(0f, 5f)]
    public float randomOffsetMagnitude = 1f;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        ConfigureLineRendererDefaults();
        
    }

    void ConfigureLineRendererDefaults()
    {
        if (lr == null) return;

        lr.useWorldSpace = false;
        lr.alignment = LineAlignment.View;
        lr.positionCount = 2;
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, Vector3.forward);

        lr.startWidth = width;
        lr.endWidth = width;

        lr.startColor = color;
        lr.endColor = color;
    }

    // Standard straight beam
    public void Setup(Vector3 origin, Vector3 target, float lifetime)
    {
        transform.position = origin;
        Vector3 dir = target - origin;
        float dist = Mathf.Max(0.001f, dir.magnitude);
        transform.rotation = Quaternion.LookRotation(dir.normalized);

        if (lr == null) lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.positionCount = 2;
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, Vector3.forward * dist);
        }

        if (lifetime > 0f)
            StartCoroutine(FadeAndDestroy(lifetime));
    }

    // Spline-based beam (quadratic Bezier) with custom Inspector settings
    public void SetupSpline(Vector3 origin, Vector3 target, float lifetime)
    {
        Vector3 control = useRandomSpline ? GetRandomizedControlPoint(origin, target) : GetControlPoint(origin, target);
        SetupSpline(origin, control, target, lifetime);
    }

    // Spline-based beam (quadratic Bezier) with explicit control
    public void SetupSpline(Vector3 origin, Vector3 control, Vector3 target, float lifetime)
    {
        transform.position = origin;
        Vector3 dir = target - origin;
        transform.rotation = Quaternion.LookRotation(dir.normalized);

        if (lr == null) lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.positionCount = splineResolution;
            for (int i = 0; i < splineResolution; i++)
            {
                float t = i / (float)(splineResolution - 1);
                Vector3 point = QuadraticBezier(origin, control, target, t);
                lr.SetPosition(i, transform.InverseTransformPoint(point));
            }
        }

        if (lifetime > 0f)
            StartCoroutine(FadeAndDestroy(lifetime));
    }

    // Helper to compute control point from Inspector settings
    Vector3 GetControlPoint(Vector3 origin, Vector3 target)
    {
        Vector3 mid = (origin + target) * 0.5f;
        Vector3 dir = controlDirection.normalized;
        return mid + dir * controlOffset;
    }

    // Helper to compute randomized control point
    Vector3 GetRandomizedControlPoint(Vector3 origin, Vector3 target)
    {
        Vector3 mid = (origin + target) * 0.5f;
        Vector3 baseDir = controlDirection.normalized * controlOffset;
        Vector3 randomDir = Random.onUnitSphere * randomOffsetMagnitude;
        return mid + baseDir + randomDir;
    }

    // Quadratic Bezier formula
    Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    IEnumerator FadeAndDestroy(float lifetime)
    {
        float t = 0f;
        float fadeDuration = Mathf.Min(0.25f, lifetime * 0.5f);
        float visibleTime = Mathf.Max(0f, lifetime - fadeDuration);

        // keep visible for visibleTime
        yield return new WaitForSeconds(visibleTime);


        // fade
        while (t < fadeDuration)
        {
            lr.startWidth -= (width / fadeDuration) * Time.deltaTime;
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