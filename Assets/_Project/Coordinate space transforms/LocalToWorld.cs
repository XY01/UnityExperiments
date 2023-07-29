using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalToWorld : MonoBehaviour
{
    public enum SpaceConversion
    {
        LocalToWorld,
        WorldToLocal,
    }

    public SpaceConversion spaceConversion = SpaceConversion.LocalToWorld;
    public Transform[] localToWorldTransforms;
    public Vector3[] localPositions;

    public bool offsetPos = false;
    public Vector3 UVW;
    public Transform[] worldToLocalTransforms;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public Vector3 worldPosToBoundsUVW(Matrix4x4 boundsWorldToLocal, Vector3 boundsPos, Vector3 worldPosIn)
    {
        worldPosIn -= boundsPos;
        Vector3 convertedPosition = boundsWorldToLocal * worldPosIn;
        convertedPosition += Vector3.one * .5f;
        return convertedPosition;
    }

    // Update is called once per frame
    void OnDrawGizmos()
    {
        foreach (var t in localToWorldTransforms)
        {
            foreach (var locPos in localPositions)
            {

                Vector3 convertedPosition = spaceConversion == SpaceConversion.LocalToWorld ?
                    t.localToWorldMatrix * locPos
                    : t.worldToLocalMatrix * locPos;
                
                convertedPosition = offsetPos ? 
                    convertedPosition + t.position 
                    : convertedPosition;
                
                
               // Gizmos.DrawSphere(convertedPosition, .1f);
               // Gizmos.DrawLine(t.position, convertedPosition);
            }
            
            foreach (var w in worldToLocalTransforms)
            {
              
                UVW = worldPosToBoundsUVW(t.worldToLocalMatrix, t.position, w.position);

                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(UVW, .1f);
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(Vector3.one * .5f, Vector3.one);
                //Gizmos.DrawLine(t.position, convertedPosition);
                //Debug.Log(UVW);
            }
        }

       
    }
}
