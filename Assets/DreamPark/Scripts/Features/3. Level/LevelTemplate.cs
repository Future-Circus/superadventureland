using Unity.AI.Navigation;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DreamPark {
    public enum GameLevelSize {
        Micro,
        Small,
        Medium,
        Large,
        Jumbo
    }
    public static class GameLevelDimensions
    {
        public static Vector2 GetDimensions(GameLevelSize size)
        {
            switch (size)
            {
                case GameLevelSize.Micro:
                    return new Vector2(16f, 24f);
                case GameLevelSize.Small:
                    return new Vector2(30f, 64f);
                case GameLevelSize.Medium:
                    return new Vector2(50f, 94f);
                case GameLevelSize.Large:
                    return new Vector2(80f, 128f);
                case GameLevelSize.Jumbo:
                    return new Vector2(120f, 150f);
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
        private GameObject runtimePlane;

        void Start()
        {
            if (generateFloor) GenerateFloor();
        } 

        private void GenerateFloor() {
            if (runtimePlane != null) Destroy(runtimePlane);

            Vector2 dims = GameLevelDimensions.GetDimensionsInMeters(size);
            float width = dims.x;
            float height = dims.y;

            runtimePlane = new GameObject("LevelFloor");
            runtimePlane.layer = LayerMask.NameToLayer("Level");
            runtimePlane.transform.SetParent(transform, false);

            MeshFilter mf = runtimePlane.AddComponent<MeshFilter>();
            MeshCollider mc = runtimePlane.AddComponent<MeshCollider>();

            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[4] {
                new Vector3(-width/2f, 0, -height/2f),
                new Vector3(-width/2f, 0,  height/2f),
                new Vector3( width/2f, 0,  height/2f),
                new Vector3( width/2f, 0, -height/2f)
            };

            int[] triangles = new int[6] {
                0, 1, 2,
                0, 2, 3
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
            mc.sharedMesh = mesh;

            var surface = runtimePlane.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.layerMask = LayerMask.GetMask("Level");
            surface.collectObjects = CollectObjects.Children;
            surface.agentTypeID = UnityEngine.AI.NavMesh.GetSettingsByIndex(1).agentTypeID;
            surface.BuildNavMesh();
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