using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class ActuatorControlledHinge : MonoBehaviour
{
    [SerializeField] private float HingeTipLengthFixed = 1;
    [SerializeField] private float HingeBaseLengthFixed = 2;
    private float BaseTipLength;
    [SerializeField] float ActuatroLengthDynamic = 1;
    private float angle = 0;

    [SerializeField] private Transform ActuatorBase;
    [SerializeField] private Transform ActuatorTip;
    [SerializeField] private Transform Hinge;

    [SerializeField] private float angleOffset = 0;

    [SerializeField] private float ActuatorAtRestLength = .7f;
    [SerializeField] private float Stroke = .4f;

    private float MinLength => ActuatorAtRestLength;
    private float MaxLength => ActuatorAtRestLength + Stroke;
    
    
    private void OnValidate()
    {
    
    }

    private void OnDrawGizmos()
    {
        ActuatroLengthDynamic = Mathf.Clamp(ActuatroLengthDynamic,MinLength,MaxLength);
        
        // Angle from base to hinge
        float angleBaseToHinge = Vector3.SignedAngle(Vector3.down, (ActuatorBase.position - Hinge.position).normalized, Vector3.forward);
        
        // Angle from hinge to actuator tip
        angle = CalculateAngle(HingeTipLengthFixed,HingeBaseLengthFixed,ActuatroLengthDynamic);
        angle -= angleBaseToHinge;
        
        Vector3 vertHinge = Hinge.position;
        Vector3 vertActuatorTip = vertHinge - new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle* Mathf.Deg2Rad), 0) * HingeTipLengthFixed;
        Vector3 vertActuatorBase = ActuatorBase.position;
        
        BaseTipLength = Vector3.Distance(vertActuatorTip, vertActuatorBase);

        ActuatorTip.position = vertActuatorTip;
        // Rotations 
        ActuatorBase.localRotation = Quaternion.LookRotation(ActuatorBase.localPosition - vertActuatorTip);
        Hinge.localRotation = Quaternion.LookRotation(Hinge.localPosition - vertActuatorTip);
        
        // Length A
        Gizmos.DrawLine(vertHinge, vertActuatorBase);
        // Length B
        Gizmos.DrawLine(vertHinge, vertActuatorTip);
        // Length C
        Gizmos.DrawLine(vertActuatorTip, vertActuatorBase);
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
            
            EditorGUILayout.LabelField("Angle", actuator.angle.ToString("F2"));
            
            EditorGUILayout.LabelField("Hinge - Base Length", actuator.HingeBaseLengthFixed.ToString("F2"));
            EditorGUILayout.LabelField("Hinge - Tip Length", actuator.HingeTipLengthFixed.ToString("F2"));
            EditorGUILayout.LabelField("Base - Tip Length", actuator.BaseTipLength.ToString("F2"));
            
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
