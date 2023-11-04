using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class ActuatorControlledHinge : MonoBehaviour
{
    public float FixedLengthA = 1;
    public float FixedLengthB = 2;
    public float VariableLengthC = 1;
    private float angle = 0;
    
    private void OnDrawGizmos()
    {
        
        VariableLengthC = Mathf.Clamp(VariableLengthC, Mathf.Abs(FixedLengthA-FixedLengthB), FixedLengthA + FixedLengthB);
        
        angle = CalculateAngle(FixedLengthA, FixedLengthB, VariableLengthC);
        Vector3 vertAB = transform.position;
        Vector3 vertBC = vertAB - new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle* Mathf.Deg2Rad), 0) * FixedLengthB;
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
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(ActuatorControlledHinge))]
    private class MyScriptEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ActuatorControlledHinge actuator = (ActuatorControlledHinge)target;

            DrawDefaultInspector();
            
            EditorGUILayout.LabelField("Angle", actuator.angle.ToString());
            
            // myScript.myColor = EditorGUILayout.ColorField("My Color", myScript.myColor);
            //
            // if (GUILayout.Button("Randomize Color"))
            // {
            //     myScript.myColor = new Color(Random.value, Random.value, Random.value);
            // }
        }
    }
    #endif
}
