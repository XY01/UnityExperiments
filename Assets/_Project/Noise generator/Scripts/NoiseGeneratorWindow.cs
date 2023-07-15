using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering;

public enum Noise
{
    WhiteNoise,
    ValueNoise,
    WorleyNoise,
}
    
// TODO: 
// - Add layering options. Noise type, freq, blend mode (add,mul,div...), range
public class NoiseGeneratorWindow : EditorWindow
{
    private int resolution = 512;
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
        fileName = EditorGUILayout.TextField("File Name", fileName);
        computeShader = EditorGUILayout.ObjectField("Compute Shader", computeShader, typeof(ComputeShader), false) as ComputeShader;

        noisetype = (Noise)EditorGUILayout.Popup("Noise Type:", (int)noisetype, System.Enum.GetNames(typeof(Noise)));
        freq = EditorGUILayout.IntField("Freq", freq);
        
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

        renderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.R8);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        int kernelHandle = computeShader.FindKernel(noisetype.ToString());
        computeShader.SetInt("resolution", resolution);
        computeShader.SetTexture(kernelHandle, "OutputTex", renderTexture);
        
        computeShader.SetFloat("freq", freq);
        computeShader.SetFloat("time", Time.time);
        computeShader.SetFloat("valueScalar", 1);
        computeShader.SetBool("additionalLayer", false);
        computeShader.Dispatch(kernelHandle, renderTexture.width / 8, renderTexture.height / 8, 1);
        //
        // computeShader.SetFloat("freq", freq*4);
        // computeShader.SetFloat("valueScalar", .5f);
        // computeShader.SetBool("additionalLayer", true);
        // computeShader.Dispatch(kernelHandle, renderTexture.width / 8, renderTexture.height / 8, 1);
        //
        // computeShader.SetFloat("freq", freq*8);
        // computeShader.SetFloat("valueScalar", .25f);
        // computeShader.SetBool("additionalLayer", true);
        // computeShader.Dispatch(kernelHandle, renderTexture.width / 8, renderTexture.height / 8, 1);

        
        SaveTextureAsPNG(renderTexture, fileName);
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
