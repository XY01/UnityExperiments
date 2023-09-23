using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatPropBlockAlbedoSharingTest : MonoBehaviour
{
    public MeshRenderer[] MeshRenderers;
    public Texture tex;
    
    [ContextMenu("Set prop block")]
    // Update is called once per frame
    void UpdatePropblock()
    {
        MaterialPropertyBlock matBlock = new MaterialPropertyBlock();
        MeshRenderers[0].GetPropertyBlock(matBlock);
        tex = MeshRenderers[0].sharedMaterial.GetTexture("_BaseMap");
        matBlock.SetTexture("_BaseMap", tex); 
        foreach (var rend in MeshRenderers)
        {
            rend.SetPropertyBlock(matBlock);
        }
    }
}
