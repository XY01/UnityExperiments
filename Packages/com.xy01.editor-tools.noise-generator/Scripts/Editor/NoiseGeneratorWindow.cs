using System;
//using System.Data.Common;
using UnityEngine;
using UnityEditor;
using System.IO;
using XY01.TechArt.EditorUtils;
using UnityEngine.Rendering;

public enum Noise
{
    WhiteNoise,
    ValueNoise,
    WorleyNoise,
    PerlinNoise,
}


#if UNITY_EDITOR

/// <summary>
/// Generates a range of 2D and 3D noise textures
/// Provides control for frequency, octaves, persistance, lacunarity, tiling, noise type
/// Serializes out textures to assets
/// </summary>

// TODO: 
// Add worley noise 3D
// Some cuboid artefacts in perlin 3D, check blending
// - Add layering options. Noise type, freq, blend mode (add,mul,div...), range
public class NoiseGeneratorWindow : EditorWindow
{
    private int _resolution = 512;
    private bool _tiling = false;
    private bool _volumeTexture = false;
    private int _octaves = 3;
    private float _seed = 98872364576.23f;
    private bool _useTimeSeed = true;
   
    private float _persistence = .5f;
    private float _lacunarity = 3;
    private Noise _noiseType = Noise.WhiteNoise;
    private string _fileName = "NoiseTexture";
    private ComputeShader _computeShader;
    private RenderTexture _renderTexture;
    
    private int _freq = 8;
    private static readonly int MinMaxBuffer = Shader.PropertyToID("minMaxUintBuffer");
    private static readonly int MaxVal = Shader.PropertyToID("maxVal");
    private static readonly int OutputTex = Shader.PropertyToID("OutputTex");
    private static readonly int OutputTex3D = Shader.PropertyToID("OutputTex3D");
    private static readonly int Resolution1 = Shader.PropertyToID("resolution");
    private static readonly int Freq = Shader.PropertyToID("freq");
    private static readonly int Time1 = Shader.PropertyToID("time");
    private static readonly int Octaves = Shader.PropertyToID("octaves");
    private static readonly int Persistance = Shader.PropertyToID("persistance");
    private static readonly int Lacunarity = Shader.PropertyToID("lacunarity");
    private static readonly int Tiling = Shader.PropertyToID("tiling");
    private static readonly int MinMaxFloatBuffer = Shader.PropertyToID("minMaxFloatBuffer");


    [MenuItem("Window/Noise Generator")]
    public static void ShowWindow()
    {
        GetWindow<NoiseGeneratorWindow>("Noise Generator");
        
    }

    private void OnGUI()
    {
        GUILayout.Label("Noise Generator Settings", EditorStyles.boldLabel);
        
        _resolution = EditorGUILayout.IntField("Resolution", _resolution);
        _tiling = EditorGUILayout.Toggle("Tiling", _tiling);
        _volumeTexture = EditorGUILayout.Toggle("volumeTexture", _volumeTexture);
        _useTimeSeed = EditorGUILayout.Toggle("Use Time Seed", _useTimeSeed);
        if(!_useTimeSeed)
            _seed = EditorGUILayout.FloatField("Seed", _seed);
        EditorGUILayout.Space(10);
        
        _noiseType = (Noise)EditorGUILayout.Popup("Noise Type:", (int)_noiseType, System.Enum.GetNames(typeof(Noise)));
        _octaves = EditorGUILayout.IntField("Octaves", _octaves);
        _freq = EditorGUILayout.IntField("Freq", _freq);
        _octaves = Mathf.Max(_octaves, 1);
        _persistence = EditorGUILayout.Slider("Persistence (Amp Scalar)",_persistence, 0,1);
        _lacunarity = EditorGUILayout.FloatField("Lacunarity (Freq scalar)", _lacunarity);
        _lacunarity = Mathf.Max(_lacunarity, 1);
        EditorGUILayout.Space(10);
        
        _fileName = EditorGUILayout.TextField("File Name", _fileName);
        _computeShader = EditorGUILayout.ObjectField("Compute Shader", _computeShader, typeof(ComputeShader), false) as ComputeShader;

        if (GUILayout.Button("Generate Noise"))
            GenerateNoise();
        
        
        if (GUILayout.Button("Save"))
            SaveTextureAsPNG(_renderTexture, _fileName);
        
        
        if (_renderTexture != null)
        {
            GUILayout.Label("Generated Noise Texture");
            Rect rect = GUILayoutUtility.GetAspectRect(1, GUILayout.Width(360), GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(rect, _renderTexture, null, ScaleMode.ScaleToFit, 1, 0, ColorWriteMask.Red);
        }
    }

    private void GenerateNoise()
    {
        if (_computeShader == null)
        {
            Debug.LogError("Compute shader is not assigned!");
            return;
        }
        
        // SETUP RENDER TEX
        _renderTexture = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.RFloat);
        if (_volumeTexture)
        {
            _renderTexture.dimension = TextureDimension.Tex3D;
            _renderTexture.volumeDepth = _resolution;
        }
        _renderTexture.enableRandomWrite = true;
        _renderTexture.Create();

        // SETUP COMPUTE SHADER
        int kernelHandle = _computeShader.FindKernel(_volumeTexture ? _noiseType.ToString() +"3D" : _noiseType.ToString());
        _computeShader.SetInt(Resolution1, _resolution);
        _computeShader.SetTexture(kernelHandle, _volumeTexture ? OutputTex3D : OutputTex, _renderTexture);
        
        _computeShader.SetFloat(Freq, _freq);
        _computeShader.SetFloat(Time1, _useTimeSeed ? Time.time : _seed);
        
        _computeShader.SetInt(Octaves, _octaves);
        _computeShader.SetFloat(Persistance, _persistence);
        _computeShader.SetFloat(Lacunarity, _lacunarity);
        _computeShader.SetBool(Tiling, _tiling);

        int threads = _resolution / 8;

        // DISPATCH COMPUTE SHADER
        _computeShader.Dispatch(kernelHandle, threads, threads, 
            _volumeTexture ? threads : 1);


        if (_volumeTexture)
        {
            int minMaxKernel =_computeShader.FindKernel("MinMax3D");
            ComputeBuffer minMaxUintBuffer = new ComputeBuffer(2, sizeof(int), ComputeBufferType.Default);
            minMaxUintBuffer.SetData(new int[]{1000,0});
           _computeShader.SetBuffer(minMaxKernel,MinMaxBuffer, minMaxUintBuffer);
           _computeShader.SetTexture(minMaxKernel, OutputTex3D, _renderTexture);
           _computeShader.Dispatch(minMaxKernel, _renderTexture.width, _renderTexture.height, _renderTexture.volumeDepth / 8);
        
           
           int[] maxData = new int[2];
           minMaxUintBuffer.GetData(maxData);
           float min = AsFloat(maxData[0]);
           float max = AsFloat(maxData[1]);
           Debug.Log($"{min}   {max}");
        
           ComputeBuffer minMaxFloatBuffer = new ComputeBuffer(2, sizeof(float), ComputeBufferType.Default);
           // TODO: If perlin min is -max, seems to be an issue with asfloat converting to neg
           minMaxFloatBuffer.SetData(new float[]{min,max});
            int remapKernel =_computeShader.FindKernel("Remap3D");
           _computeShader.SetBuffer(remapKernel,MinMaxFloatBuffer, minMaxFloatBuffer);
           _computeShader.SetTexture(remapKernel, OutputTex3D, _renderTexture);
           _computeShader.Dispatch(remapKernel, _renderTexture.width / 8, _renderTexture.height / 8, _renderTexture.volumeDepth / 8);
        
        }
        else
        {
            
            int minMaxKernel =_computeShader.FindKernel("MinMax2D");
            ComputeBuffer minMaxUintBuffer = new ComputeBuffer(2, sizeof(int), ComputeBufferType.Default);
            minMaxUintBuffer.SetData(new int[]{1000,0});
           _computeShader.SetBuffer(minMaxKernel,MinMaxBuffer, minMaxUintBuffer);
           _computeShader.SetTexture(minMaxKernel, OutputTex, _renderTexture);
           _computeShader.Dispatch(minMaxKernel, _renderTexture.width, _renderTexture.height, 1);
        
           int[] maxData = new int[2];
           minMaxUintBuffer.GetData(maxData);
           float min = AsFloat(maxData[0]);
           float max = AsFloat(maxData[1]);
           Debug.Log($"{min}   {max}");
           
        
           ComputeBuffer minMaxFloatBuffer = new ComputeBuffer(2, sizeof(float), ComputeBufferType.Default);
           minMaxFloatBuffer.SetData(new float[]{-max,max});
            int remapKernel =_computeShader.FindKernel("Remap2D");
           _computeShader.SetBuffer(remapKernel,MinMaxFloatBuffer, minMaxFloatBuffer);
           _computeShader.SetTexture(remapKernel, OutputTex, _renderTexture);
           _computeShader.Dispatch(remapKernel, _renderTexture.width / 8, _renderTexture.height / 8, 1);
        
           minMaxFloatBuffer.Dispose();
           minMaxUintBuffer.Dispose();
        }

      

        if (_volumeTexture)
        {
            SaveRenderTextures.Save3D(
                _renderTexture,
                "/_Project/Noise generator/Textures/"+_fileName + " Volume",
                RenderTextureFormat.RFloat,
                TextureFormat.RFloat
            );
        }
        else
        {
            SaveTextureAsPNG(_renderTexture, _fileName);
        }
    }
    
    // Equivalent of asfloat in C#
    float AsFloat(int value)
    {
        return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
    }

    private void SaveTextureAsPNG(RenderTexture renderTexture, string fileName)
    {
        RenderTexture.active = renderTexture;
        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.R8, false);
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        byte[] bytes = texture2D.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/_Project/Noise generator/Textures/" + fileName + ".png", bytes);
    }
}
#endif
