using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class Tiling3DNoiseGenerator : MonoBehaviour
{
    public int textureSize = 128;
    public float frequency = 10f;
    public float amplitude = 1f;
    public int tileSize = 4;
    public string textureName = "Tiling3DClouds";

    public float pow = 4;
    public float lowFreq = .1f;
    [Range(0,1)]
    public float lowScalar = 1;
    public float midFreq = .3f;
    [Range(0, 1)]
    public float midScalar = 1;
    public float highFreq = 1;
    [Range(0, 1)]
    public float highScalar = 1;

    public int index = 0;

    [ContextMenu("Generate")]
    void Start()
    {
        Texture3D noiseTexture = GenerateTiling3DNoiseTexture(textureSize, frequency, amplitude, tileSize);
        noiseTexture.name = textureName;

        // Assign the noise texture to a material or save it to a file
        // ...
    }

    Texture3D GenerateTiling3DNoiseTexture(int size, float frequency, float amplitude, int tileSize)
    {
        float[,,] noiseData = new float[size, size, size];
        Texture3D noiseTexture = new Texture3D(size, size, size, TextureFormat.RGBAHalf, false);
        Color[] colorArray = new Color[size * size * size];

        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size * tileSize;
                    float ny = (float)y / size * tileSize;
                    float nz = (float)z / size * tileSize;

                    float high = (Mathf.PerlinNoise(nx * frequency * highFreq, ny * frequency*highFreq) +
                                      Mathf.PerlinNoise(ny * frequency* highFreq, nz * frequency*highFreq) +
                                      Mathf.PerlinNoise(nz * frequency* highFreq, nx * frequency* highFreq)) / 3.0f;
                    high *= highScalar;

                   float mid = ( Mathf.PerlinNoise(nx * frequency * midFreq, ny * frequency* midFreq) +
                                  Mathf.PerlinNoise(ny * frequency * midFreq, nz * frequency* midFreq) +    
                                  Mathf.PerlinNoise(nz * frequency * midFreq, nx * frequency* midFreq)) / 3.0f;

                    mid *= midScalar;

                   float low =  (Mathf.PerlinNoise(nx * frequency * lowFreq, ny * frequency * lowFreq) +
                                    Mathf.PerlinNoise(ny * frequency * lowFreq, nz * frequency * lowFreq) +
                                    Mathf.PerlinNoise(nz * frequency * lowFreq, nx * frequency * lowFreq)) / 3.0f;

                    low *= lowScalar;


                    float final = high + mid + low;
                    final = Mathf.Pow(final, pow);
                    noiseData[x, y, z] = final;
                }
            }
        }

        int idx = 0;
        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++, idx++)
                {
                    colorArray[idx] = new Color(Mathf.Pow(noiseData[x, y, z],4), 0, 0, 1);
                }
            }
        }

        noiseTexture.SetPixels(colorArray);
        noiseTexture.Apply();

#if UNITY_EDITOR
        string assetPath = "Assets/_Project/Shaders/VolumeTextures/" + textureName + index + ".asset";
        SaveTexture3DAsset(noiseTexture, assetPath);
        index++;
#endif

        return noiseTexture;
    }




    // ...

#if UNITY_EDITOR
    
    void SaveTexture3DAsset(Texture3D texture, string assetPath)
    {
        AssetDatabase.CreateAsset(texture, assetPath);
        AssetDatabase.SaveAssets();
    }
    #endif
}