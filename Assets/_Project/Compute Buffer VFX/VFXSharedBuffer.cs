using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;

public class VFXSharedBuffer : MonoBehaviour
{
        public ComputeShader computeShader;
        private GraphicsBuffer particleBuffer;
        private int kernelHandle;
        private Vector3[] particles;
        public VisualEffect vfx;
    
    
        void Start() {
            kernelHandle = computeShader.FindKernel("CSMain");
    
            // Initialize particles
            particles = new Vector3[128];
            for (int i = 0; i < particles.Length; i++) {
                particles[i] = new Vector3(i * 0.1f, 0, 0);
            }
    
            // Create compute buffer
            particleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 128, 3 * sizeof(float));
            particleBuffer.SetData(particles);
    
            // Set buffer for the compute shader
            computeShader.SetBuffer(kernelHandle, "Particles", particleBuffer);
            
            vfx.SetGraphicsBuffer("ParticlePosBuffer", particleBuffer);
        }
    
        void Update() {
            
            computeShader.SetFloat("time", Time.time);
            // Dispatch the compute shader
            computeShader.Dispatch(kernelHandle, particles.Length, 1, 1);
    
            // Retrieve data
            //particleBuffer.GetData(particles);
    
            // Update your particle system or visualization here with the new positions
        }
    
        void OnDestroy() {
            // Release the buffer
            if (particleBuffer != null) {
                particleBuffer.Release();
            }
        }
}


