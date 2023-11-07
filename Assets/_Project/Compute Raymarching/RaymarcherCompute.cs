using UnityEngine;

public class RaymarchingSphere : MonoBehaviour
{
    public ComputeShader RaymarchingShader;
    public Vector3 SpherePosition;
    public float SphereRadius = 1.0f;
    [SerializeField] private RenderTexture _target;
    [SerializeField] private Camera _cam;

    private int _kernelHandle;
    [SerializeField] private Material OutputMat;

    [SerializeField, Range(0,3)] private float Smoothing = 0;
    [SerializeField, Range(0,5)] private float SphereOffset = 0;

    void Start()
    {
        _cam = Camera.main;
        InitializeRenderTexture();
        _kernelHandle = RaymarchingShader.FindKernel("RunRaymarcher");
    }

    void InitializeRenderTexture()
    {
        if (_target != null)
        {
            _target.Release();
        }
        
        _target = new RenderTexture(Screen.width, Screen.height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        
        _target.enableRandomWrite = true;
        _target.Create();
        
        OutputMat.SetTexture("_MainTex", _target);
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
        
        RaymarchingShader.SetFloat("_Smoothing", Smoothing);
        RaymarchingShader.SetFloat("_SphereOffset", SphereOffset);
        
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
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
    }
}
