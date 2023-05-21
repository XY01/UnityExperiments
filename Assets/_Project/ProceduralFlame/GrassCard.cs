using UnityEngine;

public class GrassCard : MonoBehaviour
{
    public float width = 0.1f; // Width of the card.
    public int subdivisions = 2; // Number of vertical subdivisions.
    public float bendAngle = 15f; // Bend angle in degrees.

    [ContextMenu("Generate")]
    private void Start()
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshFilter.mesh = GenerateMesh();
        meshRenderer.material = new Material(Shader.Find("Standard"));
    }


    private Mesh GenerateMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(subdivisions + 1) * 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[subdivisions * 6];

        float halfWidth = width / 2f;
        float subdivisionHeight = 1f / subdivisions;

        for (int i = 0; i <= subdivisions; i++)
        {
            float y = i * subdivisionHeight;
            float z = 0;

            // Apply the bend at the top of the mesh.
            if (i == subdivisions)
            {
                z = Mathf.Tan(Mathf.Deg2Rad * bendAngle) * halfWidth;
            }

            vertices[i * 2] = new Vector3(-halfWidth, y, z);
            vertices[i * 2 + 1] = new Vector3(halfWidth, y, z);

            uv[i * 2] = new Vector2(0, y);
            uv[i * 2 + 1] = new Vector2(1, y);

            // We don't need to create triangles for the last pair of vertices.
            if (i == subdivisions)
            {
                break;
            }

            triangles[i * 6] = i * 2;
            triangles[i * 6 + 1] = i * 2 + 1;
            triangles[i * 6 + 2] = i * 2 + 2;
            triangles[i * 6 + 3] = i * 2 + 2;
            triangles[i * 6 + 4] = i * 2 + 1;
            triangles[i * 6 + 5] = i * 2 + 3;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}
