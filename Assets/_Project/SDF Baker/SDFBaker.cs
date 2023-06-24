using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Flaim
{
    /// <summary>
    /// Unity has its own built in SDF baker. DOing this to experiment with adding noise
    /// </summary>
    public class SDFBaker : MonoBehaviour
    {
        public MeshFilter meshFilter;
        public int res = 128;
        public Texture3D SDFTexture;
        public Material mat;

        [ContextMenu("Generate SDF")]
        public void TestSDF()
        {
            SDFTexture = GenerateSDF(res, meshFilter);

            mat.SetTexture("_Tex3D", SDFTexture);
        }

        Texture3D GenerateSDF(int resolution, MeshFilter meshFilter)
        {
            Texture3D sdfTex = new Texture3D(resolution, resolution, resolution, TextureFormat.RFloat, false);
            sdfTex.wrapMode = TextureWrapMode.Clamp;
            float[] sdfData = new float[resolution * resolution * resolution];
            Bounds bounds = meshFilter.GetComponent<MeshRenderer>().bounds;
            bounds.Expand(bounds.size * .2f);
            Vector3[] vertices = meshFilter.sharedMesh.vertices;
            int[] triangles = meshFilter.sharedMesh.triangles;


            // Iterate through each point in the 3D texture
            for (int z = 0; z < resolution; z++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        int index = x + y * resolution + z * resolution * resolution;
                       
                        Vector3 worldPoint = bounds.min + new Vector3((float)x / (resolution - 1), (float)y / (resolution - 1), (float)z / (resolution - 1));
                        worldPoint = UnityEngine.Vector3.Scale(worldPoint, bounds.size);
                        if (IsPointInsideMesh(worldPoint, meshFilter.sharedMesh, meshFilter.transform))
                        {
                            sdfData[index] = 1;
                            continue;
                        }

                        float minDistance = float.MaxValue;

                        // Iterate through each triangle in the mesh
                        for (int i = 0; i < triangles.Length; i += 3)
                        {
                            Vector3 vertexA = meshFilter.transform.TransformPoint(vertices[triangles[i]]);
                            Vector3 vertexB = meshFilter.transform.TransformPoint(vertices[triangles[i + 1]]);
                            Vector3 vertexC = meshFilter.transform.TransformPoint(vertices[triangles[i + 2]]);

                            // Calculate the distance from the point to the triangle
                            float distance = PointToTriangleDistance(worldPoint, vertexA, vertexB, vertexC);
                            minDistance = Mathf.Min(minDistance, distance);
                        }

                        // Store the SDF value in the sdfData array
                       
                        sdfData[index] = Mathf.Clamp01(1 - (minDistance/.1f));
                    }
                }
            }

            sdfTex.SetPixelData(sdfData, 0);
            sdfTex.Apply();

            return sdfTex;
        }

        float PointToTriangleDistance(Vector3 point, Vector3 vertexA, Vector3 vertexB, Vector3 vertexC)
        {
            // Compute the edges and normal of the triangle
            Vector3 edgeAB = vertexB - vertexA;
            Vector3 edgeAC = vertexC - vertexA;
            Vector3 normal = Vector3.Cross(edgeAB, edgeAC).normalized;

            // Project the point onto the triangle plane
            Vector3 projectedPoint = point - Vector3.Dot(point - vertexA, normal) * normal;

            // Check if the projected point is inside the triangle
            Vector3 edgePA = projectedPoint - vertexA;
            Vector3 edgePB = projectedPoint - vertexB;
            Vector3 edgePC = projectedPoint - vertexC;

            float areaABC = Vector3.Dot(normal, Vector3.Cross(edgeAB, edgeAC));
            float areaPBC = Vector3.Dot(normal, Vector3.Cross(edgePB, edgePC));
            float areaPCA = Vector3.Dot(normal, Vector3.Cross(edgePC, edgePA));

            // If the projected point is inside the triangle, the distance is the distance from the point to the plane
            if (areaPBC + areaPCA <= areaABC && areaPBC >= 0 && areaPCA >= 0)
            {
                return Mathf.Abs(Vector3.Dot(point - vertexA, normal));
            }

            // Otherwise, find the minimum distance to the triangle's edges or vertices
            float distanceToAB = Mathf.Sqrt(SqrDistanceToSegment(point, vertexA, vertexB));
            float distanceToAC = Mathf.Sqrt(SqrDistanceToSegment(point, vertexA, vertexC));
            float distanceToBC = Mathf.Sqrt(SqrDistanceToSegment(point, vertexB, vertexC));

            return Mathf.Min(distanceToAB, Mathf.Min(distanceToAC, distanceToBC));
        }

        float SqrDistanceToSegment(Vector3 point, Vector3 vertexA, Vector3 vertexB)
        {
            Vector3 ab = vertexB - vertexA;
            Vector3 ap = point - vertexA;
            Vector3 bp = point - vertexB;
            float e = Vector3.Dot(ap, ab);

            // If the point projects outside the segment, clamp it to the closest endpoint
            if (e <= 0.0f)
            {
                return Vector3.Dot(ap, ap);
            }

            float f = Vector3.Dot(ab, ab);
            if (e >= f)
            {
                return Vector3.Dot(bp, bp);
            }

            // If the point projects onto the segment, return the squared distance from the point to the projection
            return Vector3.Dot(ap, ap) - e * e / f;
        }


      
        //--  IS POINT INSIDE MESH
        //
        bool IsPointInsideMesh(Vector3 point, Mesh mesh, Transform t)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            int intersectionCount = 0;
            Ray ray = new Ray(point, Vector3.right);

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 vertexA = t.TransformPoint(vertices[triangles[i]]);
                Vector3 vertexB = t.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 vertexC = t.TransformPoint(vertices[triangles[i + 2]]);

                if (RayTriangleIntersection(ray, vertexA, vertexB, vertexC))
                {
                    intersectionCount++;
                }
            }

            return intersectionCount % 2 == 1;
        }

        bool RayTriangleIntersection(Ray ray, Vector3 vertexA, Vector3 vertexB, Vector3 vertexC)
        {
            Vector3 edgeAB = vertexB - vertexA;
            Vector3 edgeAC = vertexC - vertexA;
            Vector3 h = Vector3.Cross(ray.direction, edgeAC);
            float a = Vector3.Dot(edgeAB, h);

            if (a > -Mathf.Epsilon && a < Mathf.Epsilon)
            {
                return false; // Ray is parallel to the triangle
            }

            float f = 1.0f / a;
            Vector3 s = ray.origin - vertexA;
            float u = f * Vector3.Dot(s, h);

            if (u < 0.0f || u > 1.0f)
            {
                return false;
            }

            Vector3 q = Vector3.Cross(s, edgeAB);
            float v = f * Vector3.Dot(ray.direction, q);

            if (v < 0.0f || u + v > 1.0f)
            {
                return false;
            }

            float t = f * Vector3.Dot(edgeAC, q);

            return t > Mathf.Epsilon;
        }
    }
}
