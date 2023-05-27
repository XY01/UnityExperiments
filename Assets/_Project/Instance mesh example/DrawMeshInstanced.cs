using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Testing on razer
/// 
/// Instanced
/// 1000 cubes 340 fps
/// 10000 cubes 260 fps
/// 100000 cubes 66 fps
/// 1000000 cubes 5 fps
/// 
/// </summary>
public class DrawMeshInstanced : MonoBehaviour
{
    public int instanceCount = 1000;
    public Mesh mesh;
    public Material instanceMaterial;
    public float radius = 30;

    List<Matrix4x4> matricies = new List<Matrix4x4>();

    MaterialPropertyBlock materialPropertyBlock;
    List<float> randoms = new List<float>();

    // Start is called before the first frame update
    void Start()
    {
        matricies = new List<Matrix4x4>();
        for (int i = 0; i < instanceCount; i++)
        {
            matricies.Add(Matrix4x4.TRS(Random.insideUnitSphere * radius, Quaternion.identity, Vector3.one));
            randoms.Add(Random.value * 5);
        }

        materialPropertyBlock = new MaterialPropertyBlock();
        materialPropertyBlock.SetFloatArray("_Random", randoms);
    }

    // Update is called once per frame
    void Update()
    {
        Graphics.DrawMeshInstanced(mesh, 0, instanceMaterial, matricies, materialPropertyBlock);
    }
}