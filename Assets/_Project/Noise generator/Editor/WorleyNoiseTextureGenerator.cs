using UnityEngine;
using UnityEditor;
using System.IO;

public class WorleyNoiseTextureGenerator : EditorWindow
{
    public int width = 512;
    public int height = 512;
    public float scale = 10.0f;
    public int seed = 0;

    [MenuItem("Window/Worley Noise Texture Generator")]
    public static void ShowWindow()
    {
        GetWindow<WorleyNoiseTextureGenerator>("Worley Noise Texture Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        width = EditorGUILayout.IntField("Width", width);
        height = EditorGUILayout.IntField("Height", height);
        scale = EditorGUILayout.FloatField("Scale", scale);
        seed = EditorGUILayout.IntField("Seed", seed);

        if (GUILayout.Button("Generate Texture"))
        {
            Texture2D texture = GenerateTexture();
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(Application.dataPath + "/WorleyNoiseTexture.png", bytes);
            AssetDatabase.Refresh();
        }
    }

    Texture2D GenerateTexture()
    {
        Texture2D texture = new Texture2D(width, height);

        // Generate a random set of points
        System.Random rand = new System.Random(seed);
        Vector2[] points = new Vector2[60];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new Vector2(rand.Next(width), rand.Next(height));
        }

        // Generate the texture
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color color = CalculateColor(x, y, points);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return texture;
    }

    Color CalculateColor(int x, int y, Vector2[] points)
    {
        float shortestDistance = float.MaxValue;
        for (int i = 0; i < points.Length; i++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), points[i]);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
            }
        }

        // Normalize the distance
        shortestDistance /= Mathf.Sqrt(width * width + height * height);
        return new Color(shortestDistance, shortestDistance, shortestDistance, 1);
    }
}
