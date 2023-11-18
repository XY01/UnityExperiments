using System.Linq;
using UnityEngine;

public class RaymarchingSphere : MonoBehaviour
{
    public struct Sphere
    {
        public Vector3 position;
        public float radius;
    }

    private ComputeBuffer sphereBuffer;
    private Sphere[] spheres;
    [SerializeField] private int numSpheres = 5;
    
    public ComputeShader RaymarchingShader;
    public Vector3 SpherePosition;
    public float SphereRadius = 1.0f;
    [SerializeField] private RenderTexture _target;
    public int TextureDownScalar = 4;
    [SerializeField] private Camera _cam;

    private int _kernelHandle;
    [SerializeField] private Material OutputMat;

    [SerializeField, Range(0,3)] private float Smoothing = 0;
    [SerializeField, Range(0,5)] private float SphereOffset = 0;

    void Start()
    {
        _cam = Camera.main;
        InitializeRenderTexture();
        InitializeSpheres();
        _kernelHandle = RaymarchingShader.FindKernel("RunRaymarcher");
    }

    void InitializeRenderTexture()
    {
        if (_target != null)
        {
            _target.Release();
        }
        
        _target = new RenderTexture(Screen.width/TextureDownScalar, Screen.height/TextureDownScalar, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        
        _target.enableRandomWrite = true;
        _target.Create();
        
        OutputMat.SetTexture("_MainTex", _target);
    }

    void InitializeSpheres()
    {
        spheres = new Sphere[numSpheres];
        for (int i = 0; i < spheres.Length; i++)
        {
            spheres[i] = new Sphere() { position = Random.insideUnitSphere * 2.5f, radius = Random.Range(.1f, .3f)};
        }

        spheres = spheres.OrderBy(x => Vector3.Distance(Vector3.forward * -4f, x.position)).ToArray();
        
        // Make sure the buffer is created with the right size
        sphereBuffer = new ComputeBuffer(spheres.Length, sizeof(float) * 4);
        sphereBuffer.SetData(spheres);
    }

    void Update()
    {
        //if (_cam.transform.hasChanged || transform.hasChanged)
        {
            DispatchShader();
            _cam.transform.hasChanged = false;
            transform.hasChanged = false;
        }
    }

    void DispatchShader()
    {
        RaymarchingShader.SetTexture(_kernelHandle, "Result", _target);
        RaymarchingShader.SetVector("_SpherePosition", SpherePosition);
        RaymarchingShader.SetFloat("_SphereRadius", SphereRadius);
        RaymarchingShader.SetMatrix("_CameraToWorld", _cam.cameraToWorldMatrix);
        RaymarchingShader.SetMatrix("_InverseProjection", _cam.projectionMatrix.inverse);
        RaymarchingShader.SetVector("_Resolution", new Vector2(_target.width, _target.height));
        
        // Bind the buffer to the compute shader
        RaymarchingShader.SetBuffer(_kernelHandle, "spheres", sphereBuffer);
        RaymarchingShader.SetInt("numSpheres", spheres.Length);
        
        RaymarchingShader.SetFloat("_Smoothing", Smoothing);
        RaymarchingShader.SetFloat("_SphereOffset", SphereOffset);
        
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 32.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 32.0f);
        RaymarchingShader.Dispatch(_kernelHandle, threadGroupsX, threadGroupsY, 1);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Copy the render texture to the screen.
        Graphics.Blit(_target, destination);
    }

    void OnDestroy()
    {
        if (_target != null)
        {
            _target.Release();
        }
        
        sphereBuffer.Release();
    }
}
