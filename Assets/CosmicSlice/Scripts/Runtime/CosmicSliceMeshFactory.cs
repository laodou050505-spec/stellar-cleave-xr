using UnityEngine;

namespace CosmicSlice
{
    public static class CosmicSliceMeshFactory
    {
        public static Mesh CreateTorus(float majorRadius, float minorRadius, int majorSegments = 48, int minorSegments = 10)
        {
            var vertices = new Vector3[(majorSegments + 1) * (minorSegments + 1)];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[majorSegments * minorSegments * 6];

            for (var major = 0; major <= majorSegments; major++)
            {
                var a = major / (float)majorSegments * Mathf.PI * 2f;
                var radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                for (var minor = 0; minor <= minorSegments; minor++)
                {
                    var b = minor / (float)minorSegments * Mathf.PI * 2f;
                    var index = major * (minorSegments + 1) + minor;
                    var normal = radial * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                    vertices[index] = radial * majorRadius + normal * minorRadius;
                    normals[index] = normal;
                    uv[index] = new Vector2(major / (float)majorSegments, minor / (float)minorSegments);
                }
            }

            var triangle = 0;
            for (var major = 0; major < majorSegments; major++)
            {
                for (var minor = 0; minor < minorSegments; minor++)
                {
                    var current = major * (minorSegments + 1) + minor;
                    var next = current + minorSegments + 1;
                    triangles[triangle++] = current;
                    triangles[triangle++] = next;
                    triangles[triangle++] = current + 1;
                    triangles[triangle++] = current + 1;
                    triangles[triangle++] = next;
                    triangles[triangle++] = next + 1;
                }
            }

            var mesh = new Mesh { name = "Cosmic Torus" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static GameObject CreateTorusObject(string name, float majorRadius, float minorRadius, Material material)
        {
            var gameObject = new GameObject(name);
            gameObject.AddComponent<MeshFilter>().sharedMesh = CreateTorus(majorRadius, minorRadius);
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            return gameObject;
        }

        public static GameObject CreateFrustumObject(string name, float topRadius, float bottomRadius, float height, Material material, int segments = 48)
        {
            var vertices = new Vector3[(segments + 1) * 2];
            var triangles = new int[segments * 6];
            for (var index = 0; index <= segments; index++)
            {
                var angle = index / (float)segments * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[index * 2] = direction * topRadius;
                vertices[index * 2 + 1] = direction * bottomRadius + Vector3.down * height;
            }

            var triangle = 0;
            for (var index = 0; index < segments; index++)
            {
                var current = index * 2;
                triangles[triangle++] = current;
                triangles[triangle++] = current + 1;
                triangles[triangle++] = current + 2;
                triangles[triangle++] = current + 2;
                triangles[triangle++] = current + 1;
                triangles[triangle++] = current + 3;
            }

            var mesh = new Mesh { name = "Cosmic Gravity Frustum" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var gameObject = new GameObject(name);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            return gameObject;
        }

        public static Mesh CreateOctahedron(float radius = 0.5f)
        {
            var vertices = new[]
            {
                Vector3.up * radius,
                Vector3.right * radius,
                Vector3.forward * radius,
                Vector3.left * radius,
                Vector3.back * radius,
                Vector3.down * radius
            };
            var triangles = new[]
            {
                0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 1,
                5, 2, 1, 5, 3, 2, 5, 4, 3, 5, 1, 4
            };
            var mesh = new Mesh { name = "Faceted Anomaly Crystal" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static GameObject CreateOctahedronObject(string name, float radius, Material material)
        {
            var gameObject = new GameObject(name);
            gameObject.AddComponent<MeshFilter>().sharedMesh = CreateOctahedron(radius);
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            return gameObject;
        }
    }
}
