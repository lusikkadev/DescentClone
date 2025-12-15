using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

// for cutscene animation

public class MoveAlongSpline : MonoBehaviour {
    public SplineContainer container;
    public float distance = 0;
    float t;
    float sLength;
    Spline spline;

    void Start() {
        spline = container.Spline;
        if (spline == null || spline.Count < 2)
            Debug.LogError("bad spline");
        // GetLength does not use object scaling, but CalculateLength would do that.
        // Spline length calculation is pretty expensive so let's keep that in Start.
        sLength = spline.GetLength(); 
        if (sLength <= 0.001f)
            Debug.LogError("zero length spline");
        print("length:" + sLength);
    }

    void Update() {
        // Here we are assuming a good spline, already checked in Start.
        // If splines need to change dynamically, there's probably an event to listen to.

        // t is [0..1]
        t = distance / sLength;
        t = Mathf.Clamp01(t); // just in case
        
        // This is in local coordinate system of the spline:
        //SplineUtility.Evaluate(spline, t, out var pos, out var dir, out var up);
        // This includes the transform of the container (gives world positions and directions): 
        container.Evaluate(spline, t, out var pos, out var dir, out var up);
        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(dir, up);
    }
}
