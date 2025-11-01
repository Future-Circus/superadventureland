using Unity.AI.Navigation;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DreamPark {
    public enum GameLevelSize {
        Micro,
        Boutique,
        Small,
        Square,
        Medium,
        Large,
        Jumbo,
        MallCorridor
    }
    public static class GameLevelDimensions
    {
        public static Vector2 GetDimensions(GameLevelSize size)
        {
            switch (size)
            {
                case GameLevelSize.Micro:
                    return new Vector2(14f, 16f);
                case GameLevelSize.Boutique:
                    return new Vector2(16f, 30f);
                case GameLevelSize.Small:
                    return new Vector2(30f, 64f);
                case GameLevelSize.Square:
                    return new Vector2(40f, 50f);
                case GameLevelSize.Medium:
                    return new Vector2(50f, 94f);
                case GameLevelSize.Large:
                    return new Vector2(80f, 128f);
                case GameLevelSize.Jumbo:
                    return new Vector2(120f, 150f);
                case GameLevelSize.MallCorridor:
                    return new Vector2(30f, 260f);
                default:
                    return Vector2.zero;
            }
        }

        public static Vector2 GetDimensionsInMeters(GameLevelSize size)
        {
            return GetDimensions(size) * 0.3048f;
        }
    }

    [RequireComponent(typeof(GameArea))]
    [RequireComponent(typeof(MusicArea))]
    public class LevelTemplate : MonoBehaviour {
        [ReadOnly] public string gameId;
        public GameLevelSize size;
        public Vector2 defaultAnchorPosition;
        public bool generateFloor = true;
        public bool generateCeiling = true;
        private GameObject runtimePlane;
        private GameObject runtimeCeiling;

        void Start()
        {
            if (generateFloor) GenerateFloorWithHoles();
            if (generateCeiling) GenerateDepthCeiling();
        } 

        private void GenerateDepthCeiling() {
            if (runtimeCeiling != null) Destroy(runtimeCeiling);

            Vector2 dims = GameLevelDimensions.GetDimensionsInMeters(size);
            float width = dims.x;
            float height = dims.y;

            runtimeCeiling = new GameObject("LevelCeiling");
            runtimeCeiling.transform.localPosition = new Vector3(0, 2.4f, 0);
            runtimeCeiling.layer = LayerMask.NameToLayer("Triggers");
            runtimeCeiling.transform.SetParent(transform, false);
            runtimeCeiling.AddComponent<OptimizedAFIgnore>();

            MeshFilter mf = runtimeCeiling.AddComponent<MeshFilter>();
            runtimeCeiling.AddComponent<MeshRenderer>().enabled = false;

            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[4] {
                new Vector3(-width/2f, 0, -height/2f),
                new Vector3(-width/2f, 0,  height/2f),
                new Vector3( width/2f, 0,  height/2f),
                new Vector3( width/2f, 0, -height/2f)
            };

            // Flip normals by reversing the winding order
            int[] triangles = new int[6] {
                0, 2, 1,
                0, 3, 2
            };

            Vector2[] uv = new Vector2[4] {
                new Vector2(0,0),
                new Vector2(0,1),
                new Vector2(1,1),
                new Vector2(1,0)
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();

            mf.sharedMesh = mesh;
            var depthMask = runtimeCeiling.AddComponent<DepthMask>();
            depthMask.myMeshFilters.Add(mf);
            depthMask._someOffsetFloatValue = 0.6f;
        }

        private void GenerateFloorWithHoles() {
            if (runtimePlane != null) DestroyImmediate(runtimePlane);

            Vector2 dims = GameLevelDimensions.GetDimensionsInMeters(size);
            float width = dims.x;
            float height = dims.y;

            runtimePlane = new GameObject("LevelFloor");
            runtimePlane.layer = LayerMask.NameToLayer("Level");
            runtimePlane.tag = "Ground";
            runtimePlane.transform.SetParent(transform, false);
            runtimePlane.AddComponent<OptimizedAFIgnore>();

            MeshFilter mf = runtimePlane.AddComponent<MeshFilter>();
            MeshRenderer mr = runtimePlane.AddComponent<MeshRenderer>();
            MeshCollider mc = runtimePlane.AddComponent<MeshCollider>();

            mr.material = Resources.Load<Material>("Materials/Occlusion");

            // Base rectangle outline (clockwise)
            List<Vector2> outer = new List<Vector2> {
                new Vector2(-width/2f, -height/2f),
                new Vector2(-width/2f,  height/2f),
                new Vector2( width/2f,  height/2f),
                new Vector2( width/2f, -height/2f)
            };

            // Collect lava pit holes (counter-clockwise for correct winding)
            List<List<Vector2>> holes = new List<List<Vector2>>();
            foreach (var pit in GetComponentsInChildren<FloorCutout>()) {
                List<Vector2> hole = new List<Vector2>();
                foreach (var p in pit.points) {
                    Vector3 worldP = pit.transform.TransformPoint(p);
                    Vector3 localToLevel = transform.InverseTransformPoint(worldP);
                    hole.Add(new Vector2(localToLevel.x, localToLevel.z));
                }
                if (hole.Count >= 3) holes.Add(hole);
            }

            Debug.Log("holes: " + holes.Count);

            Mesh mesh = TriangulatePolygonWithHoles(outer, holes);

            mf.sharedMesh = mesh;
            mc.sharedMesh = mesh;

            var surface = runtimePlane.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.layerMask = LayerMask.GetMask("Level");
            surface.collectObjects = CollectObjects.Children;
            //search for navmesh agents in children and use the first one's agent type id
            var navmeshAgents = GetComponentsInChildren<NavMeshAgent>();
            if (navmeshAgents.Length > 0) {
                surface.agentTypeID = navmeshAgents[0].agentTypeID;
            }
            surface.BuildNavMesh();
        }

        private Mesh TriangulatePolygonWithHoles(List<Vector2> outer, List<List<Vector2>> holes)
        {
            // Use Clipper-style polygon subtraction via LibTessDotNet (bundled-friendly triangulator)
            var tess = new LibTessDotNet.Tess();

            // Convert outer
            LibTessDotNet.ContourVertex[] outerVerts = new LibTessDotNet.ContourVertex[outer.Count];
            for (int i = 0; i < outer.Count; i++)
                outerVerts[i].Position = new LibTessDotNet.Vec3(outer[i].x, outer[i].y, 0);
            tess.AddContour(outerVerts, LibTessDotNet.ContourOrientation.Clockwise);

            // Add holes (counterclockwise)
            foreach (var hole in holes)
            {
                LibTessDotNet.ContourVertex[] holeVerts = new LibTessDotNet.ContourVertex[hole.Count];
                for (int i = 0; i < hole.Count; i++)
                    holeVerts[i].Position = new LibTessDotNet.Vec3(hole[i].x, hole[i].y, 0);
                tess.AddContour(holeVerts, LibTessDotNet.ContourOrientation.CounterClockwise);
            }

            // Triangulate
            tess.Tessellate(LibTessDotNet.WindingRule.EvenOdd, LibTessDotNet.ElementType.Polygons, 3);

            // Build mesh
            var verts = new Vector3[tess.Vertices.Length];
            for (int i = 0; i < verts.Length; i++)
                verts[i] = new Vector3(tess.Vertices[i].Position.X, 0, tess.Vertices[i].Position.Y);

            var tris = new int[tess.ElementCount * 3];
            for (int i = 0; i < tess.ElementCount; i++)
            {
                // Flip the order so normals face up (Unity uses a left-handed coordinate system)
                tris[i * 3 + 0] = tess.Elements[i * 3 + 2];
                tris[i * 3 + 1] = tess.Elements[i * 3 + 1];
                tris[i * 3 + 2] = tess.Elements[i * 3 + 0];
            }

            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void TriangulateSimplePolygon(List<Vector2> poly, List<Vector3> vertices, List<int> triangles) {
            int startIndex = vertices.Count;
            for (int i = 0; i < poly.Count; i++)
                vertices.Add(new Vector3(poly[i].x, 0, poly[i].y));

            // Simple fan triangulation
            for (int i = 1; i < poly.Count - 1; i++)
                triangles.AddRange(new int[] { startIndex, startIndex + i, startIndex + i + 1 });
        }

    #if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            Vector2 dimensions = GameLevelDimensions.GetDimensionsInMeters(size);
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.5f, 0, 1f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(dimensions.x, 0, dimensions.y));
            Gizmos.color = new Color(0.5f, 0, 1f, 0.1f);
            Gizmos.DrawCube(Vector3.zero, new Vector3(dimensions.x, 0, dimensions.y));
            Handles.Label(transform.position + transform.right * (-dimensions.x / 2f - 0.5f), GameLevelDimensions.GetDimensions(size).y + "ft");
            Handles.Label(transform.position + transform.right * ( dimensions.x / 2f + 0.3f), GameLevelDimensions.GetDimensions(size).y + "ft");
            Handles.Label(transform.position + transform.forward * ( dimensions.y / 2f + 0.5f), GameLevelDimensions.GetDimensions(size).x + "ft");
            Handles.Label(transform.position + transform.forward * (-dimensions.y / 2f - 0.3f), GameLevelDimensions.GetDimensions(size).x + "ft");
            Gizmos.color = new Color(0.5f, 0, 1f,0.1f);
            Vector3 portalPosition = new Vector3(defaultAnchorPosition.x, 0, defaultAnchorPosition.y);
            Vector3 bodyPosition = new Vector3(portalPosition.x, 0, portalPosition.z - 1f);
            Mesh humanMesh = Resources.Load<Mesh>("Meshes/HumanReference");
            Mesh quadMesh = Resources.Load<Mesh>("Meshes/Quad");
            Material unlitMat = Resources.Load<Material>("Materials/UnlitCutout");
            unlitMat.SetTexture("_baseTex", Resources.Load<Texture2D>("Textures/Portal"));
            Matrix4x4 matrix = Matrix4x4.Translate(portalPosition);
            matrix = transform.localToWorldMatrix * matrix;
            unlitMat.SetPass(0);
            Gizmos.DrawMesh(humanMesh, bodyPosition);
            Graphics.DrawMeshNow(quadMesh, matrix);
            Gizmos.matrix = oldMatrix;
        }
    #endif
    }
}