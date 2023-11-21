using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using Random = UnityEngine.Random;

public class VFXSharedBuffer : MonoBehaviour
{
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)] // This attribute marks the struct for use in VFX Graph
    public struct Particle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Size;
        public Vector4 Color;
    }
    
    
    
    public ComputeShader ComputeShader;
    private int _updateParticlesKernel;
    [SerializeField] private float Size = .2f;
    [SerializeField] private bool Dispatch = true;
    
    public VisualEffect VFX;

    public Texture3D SDFTex;
 
    private Particle[] _particles;
    
    [SerializeField] private int ParticleCount = 128;
    private GraphicsBuffer _particleBuffer;

    private int _threadCount;

    void Start() 
    {
        _updateParticlesKernel = ComputeShader.FindKernel("UpdateParticles");

        // Initialize particles
        _particles = new Particle[ParticleCount];
        for (int i = 0; i < ParticleCount; i++)
        {
            float norm = i / (ParticleCount - 1.0f);
            _particles[i] = new Particle()
            {
                Position = new Vector3( norm * 10, 0, 0),
                Velocity = new Vector3(-0,Random.Range(.4f, 1.4f),0),
                Color = new Vector4(1, 0, 0, 1),
                Size = Size
            };
        }

        // --- CREATE GRAPHICS BUFFERS
        //
        _particleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, ParticleCount, sizeof(float) * 11);
        _particleBuffer.SetData(_particles);
      

        // --- COMPUTE SHADER
        //
        // BUFFERS
        ComputeShader.SetBuffer(_updateParticlesKernel, "ParticleBuffer", _particleBuffer);
        // VARS
        ComputeShader.SetTexture(_updateParticlesKernel, "SDF", SDFTex);
        ComputeShader.SetInt("ParticleCount", ParticleCount);
        
        _threadCount = Mathf.CeilToInt(_particles.Length/64f);
    
        
        // --- VFX GRAPH
        //
        // VARS
        VFX.SetInt("ParticleCount", ParticleCount);
        // BUFFERS
        VFX.SetGraphicsBuffer("ParticleBuffer", _particleBuffer);
    }

    void Update() 
    {
        if(!Dispatch) return;
        
        ComputeShader.SetFloat("time", Time.time);
        ComputeShader.SetFloat("deltaTime", Time.deltaTime);
        // Dispatch the compute shader
        ComputeShader.Dispatch(_updateParticlesKernel, _threadCount, 1, 1);

        // Retrieve data
        //particleBuffer.GetData(particles);

        // Update your particle system or visualization here with the new positions
    }

    void OnDestroy()
    {
        // Release the buffer
        if (_particleBuffer != null) 
        {
            _particleBuffer.Release();
        }
    }
}