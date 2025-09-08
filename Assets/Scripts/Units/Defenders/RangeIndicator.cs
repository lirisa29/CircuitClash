using System.Collections.Generic;
using UnityEngine;

public class RangeIndicator : MonoBehaviour
{
    public float range = 5f;      // Match this to DefenderUnit.range
    public int segments = 30;     // Smoothness of the arc (higher = smoother)

    private Mesh mesh;

    private void Start()
    {
        GenerateMesh();
    }

    public void GenerateMesh()
    {
        mesh = new Mesh();
        mesh.name = "RangeMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Center vertex at local origin
        vertices.Add(Vector3.zero);

        // Parent scale to compensate
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;

        float angleStep = 180f / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = -90f + i * angleStep;
            float rad = angle * Mathf.Deg2Rad;

            // Compensate for parent scale
            Vector3 point = new Vector3(
                Mathf.Sin(rad) * range / parentScale.x,
                0f,
                Mathf.Cos(rad) * range / parentScale.z
            );

            vertices.Add(point);
        }

        // Generate triangles
        for (int i = 0; i < segments; i++)
        {
            triangles.Add(0);
            triangles.Add(i + 2);
            triangles.Add(i + 1);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}
