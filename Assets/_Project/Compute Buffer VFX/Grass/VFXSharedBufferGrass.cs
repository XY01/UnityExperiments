using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using Random = UnityEngine.Random;

public class VFXSharedBufferGrass : MonoBehaviour
{
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)] // This attribute marks the struct for use in VFX Graph
    public struct GrassBlade
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public float Size;
        public Vector4 Color;
    }
    
    
    public ComputeShader ComputeShader;
    private int _updateParticlesKernel;
    private uint _threadGroupCount = 8;
    private int _threadCountPerGroup;
    [SerializeField] private bool DispatchComputeShader = true;
    
    [SerializeField] private float Size = .2f;
   
    
    public VisualEffect VFX;
    
    public float PlaneSize = 10;
 
    private GrassBlade[] _grassBlades;
    
    [SerializeField] private int GrassCount = 128;
    private GraphicsBuffer _grassBuffer;


    public Transform lookAtT;
   

    void Start() 
    {
        _updateParticlesKernel = ComputeShader.FindKernel("UpdateGrass");

        // Initialize particles
        _grassBlades = new GrassBlade[GrassCount];
        for (int i = 0; i < GrassCount; i++)
        {
            float norm = i / (GrassCount - 1.0f);
            Vector2 xyPos = Random.insideUnitCircle * PlaneSize;
            _grassBlades[i] = new GrassBlade()
            {
                //Vector2 xyPos =  new Vector2(i/n, ((i*PHI)%1f) * new Vector2(PlaneSize, PlaneSize);
                Position = new Vector3(xyPos.x, 0, xyPos.y),
                Color = new Vector4(0, 1, 0, 1),
                Size = Size
            };
        }

        // --- CREATE GRAPHICS BUFFERS
        //
        _grassBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GrassCount, sizeof(float) * 11);
        _grassBuffer.SetData(_grassBlades);
      

        // --- COMPUTE SHADER
        //
        // BUFFERS
        ComputeShader.SetBuffer(_updateParticlesKernel, "GrassBuffer", _grassBuffer);
        // VARS
        ComputeShader.SetInt("GrassCount", GrassCount);
        ComputeShader.SetFloat("PlaneSize", PlaneSize);
        _threadCountPerGroup = Mathf.CeilToInt(_grassBlades.Length/64);
    
        
        // --- VFX GRAPH
        //
        // VARS
        VFX.SetInt("GrassCount", GrassCount);
        // BUFFERS
        VFX.SetGraphicsBuffer("GrassBuffer", _grassBuffer);
    }

    void Update() 
    {
        if(!DispatchComputeShader) return;
        
        ComputeShader.SetFloat("time", Time.time);
        ComputeShader.SetFloat("deltaTime", Time.deltaTime);
        ComputeShader.SetVector("cameraPosition", lookAtT.position);
        
        
        // Dispatch the compute shader
        ComputeShader.Dispatch(_updateParticlesKernel, _threadCountPerGroup, 1, 1);

        // Retrieve data
        //particleBuffer.GetData(particles);

        // Update your particle system or visualization here with the new positions
    }

    void OnDestroy()
    {
        // Release the buffer
        if (_grassBuffer != null) 
        {
            _grassBuffer.Release();
        }
    }
}