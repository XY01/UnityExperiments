using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace Flaim.Rendering
{
    public enum TextureSize
    {
        Small = 512,
        Medium = 1024,
        Large = 2048,
        ExtraLarge = 4096
    }

    /// <summary>
    /// Bakes world positions of opaque meshes into a render texture using a top down orthographic camera with a renderer setup to draw opaques with world position shader
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class WorldPositionTextureBaker : MonoBehaviour
    {      
        Camera orthoCam;

        public RenderTexture worldPosTex { get; private set; }

        public RenderTexture GenerateWorldSpaceTexture(Vector3 camPos, float areaSize, int res)
        {
            // Get cam
            orthoCam = GetComponent<Camera>();           

            // Set camera parameters
            orthoCam.transform.position = camPos + Vector3.up * 10;
            orthoCam.transform.rotation = Quaternion.Euler(90, 0, 0);
            orthoCam.orthographicSize = areaSize * .5f;
            orthoCam.aspect = 1;

            // Create render texture
            if (worldPosTex != null)
                DestroyImmediate(worldPosTex);
            worldPosTex = new RenderTexture(res, res, 0);
            worldPosTex.format = RenderTextureFormat.ARGBFloat;
            worldPosTex.enableRandomWrite = true;
            worldPosTex.filterMode = FilterMode.Trilinear;
            worldPosTex.wrapMode = TextureWrapMode.Clamp;
            worldPosTex.Create();

            // Assign render text to camera and render
            orthoCam.targetTexture = worldPosTex;
            orthoCam.Render();

            // Disable to make sure it's only rendering once
            orthoCam.enabled = false;

            return worldPosTex;
        }
    }
}
