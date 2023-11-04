using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActuatorControlledHinge : MonoBehaviour
{
    public float FixedLengthA = 1;
    public float FixedLengthB = 2;
    public float VariableLengthC = 1;

    private void OnDrawGizmos()
    {
        float angleDegs = CalculateAngle(FixedLengthA, FixedLengthB, VariableLengthC);
        Vector3 vertAB = transform.position;
        Vector3 vertBC = vertAB - new Vector3(Mathf.Sin(angleDegs * Mathf.Deg2Rad), Mathf.Cos(angleDegs* Mathf.Deg2Rad), 0) * FixedLengthB;
        Vector3 vertAC = vertAB + Vector3.down * FixedLengthA;
        // Length A
        Gizmos.DrawLine(vertAB, vertAC);
        // Length B
        Gizmos.DrawLine(vertAB, vertBC);
        // Length C
        Gizmos.DrawLine(vertBC, vertAC);
    }

    public static float CalculateAngle(float fixedLengthA, float fixedLengthB, float variableLengthC)
    {
        float a = fixedLengthA;
        float b = fixedLengthB;
        float c = variableLengthC;
        
        float cosTheta = (a * a + b * b - c * c) / (2 * a * b);
        float theta = Mathf.Acos(cosTheta);

        return theta * (180 / Mathf.PI); // Convert radian to degree
    }
}
