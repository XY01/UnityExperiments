using UnityEngine;
using UnityEngine.Rendering;

namespace XY01.TechArt.EditorUtils
{
    public class SaveRenderTextures
    {
        //the slicer compute shader
        private static ComputeShader _computeShader;

        // //render texture format of the volume
        // private static RenderTextureFormat rtFormat = RenderTextureFormat.ARGBHalf;
        //
        // //texture3d format of the final asset
        // private static TextureFormat volumeFormat = TextureFormat.RGBAHalf;

        /// <summary>
        /// Captures a single slice of the volume we are capturing.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        static RenderTexture Copy3DSliceToRenderTexture(RenderTexture source, int layer, RenderTextureFormat rtFormat)
        {
            _computeShader = Resources.Load<ComputeShader>("SaveRenderTexturesCompute");
            if(_computeShader == null)
                Debug.Log("Compute shader not loaded");
            
            //create a SLICE of the render texture
            RenderTexture render = new RenderTexture(source.width, source.height, 0, rtFormat);

            //set our options for the render texture SLICE
            render.dimension = TextureDimension.Tex2D;
            render.enableRandomWrite = true;
            render.wrapMode = TextureWrapMode.Clamp;
            render.Create();

            //find the main function in the slicer shader and start displaying each slice
            int kernelIndex = _computeShader.FindKernel("CSMain");
            _computeShader.SetTexture(kernelIndex, "voxels", source);
            _computeShader.SetInt("layer", layer);
            _computeShader.SetTexture(kernelIndex, "Result", render);
            _computeShader.Dispatch(kernelIndex, source.width, source.height, 1);

            return render;
        }

        /// <summary>
        /// Converts a 2D render texture to a Texture2D object.
        /// </summary>
        /// <param name="rt"></param>
        /// <returns></returns>
        public static Texture2D ConvertFromRenderTexture(RenderTexture rt, TextureFormat saveTexFormat)
        {
            //create our texture2D object to store the slice
            Texture2D output = new Texture2D(rt.width, rt.height, saveTexFormat, false);

            //make sure the render texture slice is active so we can read from it
            RenderTexture.active = rt;

            //read the texture and store the data in the texture2D object
            output.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            output.Apply();

            return output;
        }

        /// <summary>
        /// Saves a 3D Render Texture
        /// </summary>
        /// <param name="rt"></param>
        public static void Save3D(RenderTexture rt, string assetName, RenderTextureFormat rtTexFormat, 
            TextureFormat saveTexFormat)
        {
            //create an array that matches in length the "depth" of the volume
            RenderTexture[] layers = new RenderTexture[rt.volumeDepth]; 
            
            //create another array to store the texture2D versions of the layers array
            Texture2D[] finalSlices = new Texture2D[rt.volumeDepth]; 
           
            //copy each slice of the volume into a single render texture
            for (int i = 0; i < rt.volumeDepth; i++)
                layers[i] = Copy3DSliceToRenderTexture(rt, i, rtTexFormat);

            //convert each single render texture slice into a texture2D
            for (int i = 0; i < rt.volumeDepth; i++)
                finalSlices[i] = ConvertFromRenderTexture(layers[i], saveTexFormat);

            //create our final texture3D object
            Texture3D output = new Texture3D(rt.width, rt.height, rt.volumeDepth, saveTexFormat, false);
            output.filterMode = FilterMode.Trilinear;
            
            
            //iterate for each slice
            for (int z = 0; z < rt.volumeDepth; z++)
            {
                //get the texture2D slice
                Texture2D slice = finalSlices[z];
            
                //iterate for the x axis
                for (int x = 0; x < rt.width; x++)
                {
                    //iterate for the y axis
                    for (int y = 0; y < rt.height; y++)
                    {
                        //get the color corresponding to the x and y resolution
                        Color singleColor = slice.GetPixel(x, y);
            
                        //apply the color corresponding to the slice we are on, and the x and y pixel of that slice.
                        output.SetPixel(x, y, z, singleColor);
                    }
                }
            }
            
             //apply our changes to the 3D texture
             output.Apply();
             output.wrapMode = TextureWrapMode.Clamp;
            
             #if UNITY_EDITOR
             // TODO: Brad review editor ifdef
             //save the 3D texture asset to the disk
             UnityEditor.AssetDatabase.CreateAsset(output, $"Assets/{assetName} VolTex.asset");
#endif
        }
    }
}