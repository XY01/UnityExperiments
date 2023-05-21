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
public class DrawMeshInstancedIndirect : MonoBehaviour
{
    public int instanceCount = 1000;
    public Mesh instanceMesh;
    int subMeshIndex = 0;
    public Material instanceMaterial;
    public float radius = 30;
  

    List<Matrix4x4> matricies = new List<Matrix4x4>();

    MaterialPropertyBlock materialPropertyBlock;
    List<float> randoms = new List<float>();

    private int cachedInstanceCount = -1;
    private int cachedSubMeshIndex = -1;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer randomBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    // Start is called before the first frame update
    void Start()
    {
        // indeirect specific
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        UpdateBuffers();
    }

    // Update is called once per frame
    void Update()
    {
        // Update starting position buffer
        if (cachedInstanceCount != instanceCount || cachedSubMeshIndex != subMeshIndex)
            UpdateBuffers();

        // Render
        Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
    }

    void UpdateBuffers()
    {
        // Positions
        if (positionBuffer != null)
            positionBuffer.Release();

        if (randomBuffer != null)
            randomBuffer.Release();

        randomBuffer = new ComputeBuffer(instanceCount, 4);
        float[] randomFloats = new float[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            randomFloats[i] = Random.value;
        }
        randomBuffer.SetData(randomFloats);
        instanceMaterial.SetBuffer("_RandomValueBuffer", randomBuffer);


        positionBuffer = new ComputeBuffer(instanceCount, 16);
        Vector4[] positions = new Vector4[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            Vector3 randPos = Random.insideUnitSphere * radius;
            float size = Random.Range(1,3);
            positions[i] = new Vector4(randPos.x, randPos.y, randPos.z, size);
        }
        positionBuffer.SetData(positions);
        instanceMaterial.SetBuffer("_PositionBuffer", positionBuffer);

        // Indirect args
        if (instanceMesh != null)
        {
            args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
            args[1] = (uint)instanceCount;
            args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }
        argsBuffer.SetData(args);

        cachedInstanceCount = instanceCount;
        cachedSubMeshIndex = subMeshIndex;
    }

    void OnDisable()
    {
        if (positionBuffer != null)
            positionBuffer.Release();
        positionBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
    }
}
