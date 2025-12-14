using UnityEngine;
using System.Collections.Generic;

public struct HitData
{
    public Vector3 pos;
    public float normalizedTimer;

    public HitData(Vector3 pos, float normalizedTimer)
    {
        this.pos = pos;
        this.normalizedTimer = normalizedTimer;
    }
}

public class HitScript : MonoBehaviour
{
    public Transform hitPoint;

    public float hitDuration = 1f; // seconds
    public float hitRadiusMax = 0.3f;
    public AnimationCurve hitCurve;
    public int maxHits = 20;

    List<HitData> activeHits = new List<HitData>();
    Vector4[] shaderInput;
    Camera cam;

    public void Shoot(Vector3 hitPos)
    {
        if (activeHits.Count >= maxHits)
        {
            activeHits.RemoveAt(0);
        }
        activeHits.Add(new HitData(hitPos, 0));
    }

    float CalcHitRadius(float normalizedTimer)
    {
        return hitCurve.Evaluate(normalizedTimer);
    }
    void UpdateArray()
    {
        int i = 0;
        for (; i < activeHits.Count; i++)
        {
            var pos = activeHits[i].pos;
            shaderInput[i] = new Vector4(pos.x, pos.y, pos.z,
                CalcHitRadius(activeHits[i].normalizedTimer) * hitRadiusMax);
        }
        for (; i < shaderInput.Length; i++)
        {
            shaderInput[i] = Vector4.zero;
        }
    }
    void Start()
    {
        shaderInput = new Vector4[maxHits];
        cam = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                Shoot(hitInfo.point);
            }
        }
        // update hit timers
        for (int i = 0; i < activeHits.Count; i++)
        {
            var data = activeHits[i];
            data.normalizedTimer += Time.deltaTime * (1 / hitDuration);
            activeHits[i] = data;
        }
        activeHits.RemoveAll(hit => hit.normalizedTimer >= 1);
        UpdateArray();
        Shader.SetGlobalVectorArray("_HitPositions", shaderInput);
    }
}
