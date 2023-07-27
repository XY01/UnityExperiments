using System.Data.Common;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityUtils.Rendering;
using UnityEngine.Rendering;

public enum Noise
{
    WhiteNoise,
    ValueNoise,
    WorleyNoise,
    PerlinNoise,
}
    
// TODO: 
// - Add layering options. Noise type, freq, blend mode (add,mul,div...), range
public class NoiseGeneratorWindow : EditorWindow
{
    private int resolution = 512;
    private bool tiling = false;
    private bool _volumeTexture = false;
    private int octaves = 3;
    private float persistance = .5f;
    private float lacunarity = 3;
    private Noise noisetype = Noise.WhiteNoise;
    private string fileName = "NoiseTexture";
    private ComputeShader computeShader;
    private RenderTexture renderTexture;
    
    private int freq = 8;
    

    [MenuItem("Window/Noise Generator")]
    public static void ShowWindow()
    {
        GetWindow<NoiseGeneratorWindow>("Noise Generator");
        
    }

    private void OnGUI()
    {
        GUILayout.Label("Noise Generator Settings", EditorStyles.boldLabel);

        resolution = EditorGUILayout.IntField("Resolution", resolution);
        tiling = EditorGUILayout.Toggle("Tiling", tiling);
        _volumeTexture = EditorGUILayout.Toggle("volumeTexture", _volumeTexture);
        EditorGUILayout.Space(10);
        noisetype = (Noise)EditorGUILayout.Popup("Noise Type:", (int)noisetype, System.Enum.GetNames(typeof(Noise)));
        octaves = EditorGUILayout.IntField("Octaves", octaves);
        freq = EditorGUILayout.IntField("Freq", freq);
        octaves = Mathf.Max(octaves, 1);
        persistance = EditorGUILayout.Slider("Persistance (Amp Scalar)",persistance, 0,1);
        lacunarity = EditorGUILayout.FloatField("Lacunarity (Freq scalar)", lacunarity);
        lacunarity = Mathf.Max(lacunarity, 1);
        EditorGUILayout.Space(10);
        fileName = EditorGUILayout.TextField("File Name", fileName);
        computeShader = EditorGUILayout.ObjectField("Compute Shader", computeShader, typeof(ComputeShader), false) as ComputeShader;

      
      
        
        if (GUILayout.Button("Generate Noise"))
        {
            GenerateNoise();
        }
        
        if (GUILayout.Button("Save"))
        {
            SaveTextureAsPNG(renderTexture, fileName);
        }
        
        if (renderTexture != null)
        {
            GUILayout.Label("Generated Noise Texture");
            Rect rect = GUILayoutUtility.GetAspectRect(1, GUILayout.Width(360), GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(rect, renderTexture, null, ScaleMode.ScaleToFit, 1, 0, ColorWriteMask.Red);
        }
    }

    private void GenerateNoise()
    {
        if (computeShader == null)
        {
            Debug.LogError("Compute shader is not assigned!");
            return;
        }

        renderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat);
        if (_volumeTexture)
        {
            renderTexture.dimension = TextureDimension.Tex3D;
            renderTexture.volumeDepth = resolution;
        }


        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        int kernelHandle = computeShader.FindKernel(_volumeTexture ? noisetype.ToString() +"3D" : noisetype.ToString());
        computeShader.SetInt("resolution", resolution);
        computeShader.SetTexture(kernelHandle, _volumeTexture ? "OutputTex3D" : "OutputTex", renderTexture);
        
        computeShader.SetFloat("freq", freq);
        computeShader.SetFloat("time", Time.time);
        
        computeShader.SetInt("octaves", octaves);
        computeShader.SetFloat("persistance", persistance);
        computeShader.SetFloat("lacunarity", lacunarity);
        computeShader.SetBool("tiling", tiling);
        computeShader.SetFloat("valueScalar", 1);
        computeShader.SetBool("additionalLayer", false);

        int threads = resolution / 8;

        computeShader.Dispatch(kernelHandle, threads, threads, 
            _volumeTexture ? threads : 1);


        // int minMaxKernel = computeShader.FindKernel("MinMax");
        // ComputeBuffer minMaxBuffer = new ComputeBuffer(2, 4, ComputeBufferType.Default);
        // minMaxBuffer.SetData(new float[]{1000,0});
        // computeShader.SetBuffer(minMaxKernel,"minMaxBuffer", minMaxBuffer);
        // computeShader.SetInt("maxVal", 0);
        // computeShader.SetTexture(minMaxKernel, "OutputTex", renderTexture);
        // computeShader.Dispatch(minMaxKernel, renderTexture.width, renderTexture.height, 1);
        //
        //
        // int remapKernel = computeShader.FindKernel("Remap");
        // computeShader.SetBuffer(remapKernel,"minMaxBuffer", minMaxBuffer);
        // computeShader.SetTexture(remapKernel, "OutputTex", renderTexture);
        // computeShader.Dispatch(remapKernel, renderTexture.width / 8, renderTexture.height / 8, 1);
        //
        // float[] maxData = new float[1];
        // minMaxBuffer.GetData(maxData);
        // Debug.Log(maxData[0]);

        if (_volumeTexture)
        {
            SaveRenderTextures.Save3D(
                renderTexture,
                fileName + " Volume",
                RenderTextureFormat.RFloat,
                TextureFormat.RFloat
            );
        }
        else
        {
            SaveTextureAsPNG(renderTexture, fileName);
        }
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
