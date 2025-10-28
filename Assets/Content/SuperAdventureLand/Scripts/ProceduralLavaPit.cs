namespace SuperAdventureLand
{
    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;
    using Meta.XR.EnvironmentDepth;
    using System;
    using System.IO;

    #if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using DreamPark;

    [CustomEditor(typeof(ProceduralLavaPit), true)]
    public class ProceduralLavaPitEditor : UnityEditor.Editor
    {
        private float _taperAmount = 0f;
        private float _textureScale = 40f;
        [SerializeField] private bool _boot = false;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI(); // Draws the default inspector

            ProceduralLavaPit lavaPit = (ProceduralLavaPit)target;

            if (GUILayout.Button("Generate Lava Pit"))
            {
                lavaPit.PreProcessGenerateLavaPit();
                lavaPit.SaveMeshIfPrefab();
            }

            if (!_boot)
            {
                _boot = true;
                if (lavaPit.points.Count == 0)
                {
                    lavaPit.points = new Vector3[]{
                    new Vector3(-1, 0, -1),
                    new Vector3(1, 0, -1),
                    new Vector3(1, 0, 1),
                    new Vector3(-1, 0, 1)
                }.ToList();
                }
            }

            EditorGUILayout.HelpBox("Add platforms as children of the lava pit for player detection. Make sure their local Y-position is 0 or less.", MessageType.Info);
        }

        private Vector3 PlayModeConversion(Vector3 v, ProceduralLavaPit p)
        {
            if (Application.isPlaying)
            {
                return p.ogPosition.TransformPoint(v);
            }
            else
            {
                return p.transform.TransformPoint(v);
            }
        }

        private void OnSceneGUI()
        {
            ProceduralLavaPit lavaPit = (ProceduralLavaPit)target;
            int handleControlId = -1;

            Handles.color = Color.red;
            for (int i = 0; i < lavaPit.points.Count; i++)
            {

                Vector3 worldPosition = PlayModeConversion(lavaPit.points[i], lavaPit);
                Vector3 lastPosition = worldPosition;

                handleControlId = GUIUtility.GetControlID(FocusType.Passive);

                Vector3 newWorldPosition = Handles.FreeMoveHandle(
                    worldPosition,
                    0.2f,
                    Vector3.zero,
                    Handles.CircleHandleCap
                );

                if (newWorldPosition != worldPosition)
                {
                    Vector3 newLocalPosition = lavaPit.transform.InverseTransformPoint(newWorldPosition);
                    newLocalPosition.y = 0;
                    lavaPit.points[i] = newLocalPosition;
                    if (!Application.isPlaying)
                    {
                        EditorUtility.SetDirty(lavaPit);
                        Undo.RegisterCompleteObjectUndo(lavaPit, "Move Point");
                    }
                }
            }

            // Draw lines connecting the points
            Handles.color = Color.green;
            for (int i = 0; i < lavaPit.points.Count; i++)
            {
                Handles.DrawLine(
                    PlayModeConversion(lavaPit.points[i], lavaPit),
                    PlayModeConversion(lavaPit.points[(i + 1) % lavaPit.points.Count], lavaPit)
                );
            }

            if (GUI.changed)
            {
                lavaPit.PreProcessGenerateLavaPit();
            }
        }
    }
    #endif

    [ExecuteInEditMode]
    public class ProceduralLavaPit : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        public bool isLevelMapPin = false;
        public List<Vector3> points = new List<Vector3>();
        public float depth = 0.5f;

        [Range(0.0f, 1.0f)]
        public float taperAmount = 0f;// 0 means no taper, 1 means fully tapered to the center
        public int subdivisions = 10;
        [Range(1.0f, 100.0f)]
        public float textureScale = 40f;
        public Material stoneMaterial;
        public Material lavaMaterial;
        public Material occluderMaterial;
        private GameObject depthMaskObject;
        private MeshFilter depthMaskMeshFilter;
        private float timeInside = 0f;
        private float nextWarningTime = 0f;
        private float punishInterval = 1f;
        private Vector3 playerVolumeHalfSize = new Vector3(0.15f, 0.15f, 0.15f);
        private List<GameObject> platforms = new List<GameObject>();
        private readonly List<Collider> _platformColliders = new();
        private readonly List<Renderer> _platformRenderers = new();
        [NonSerialized] public Transform ogPosition;

        void Awake()
        {
            if (!Application.isPlaying)
                return;

            ogPosition = new GameObject("ogPosition").transform;
            ogPosition.position = transform.position;
            ogPosition.rotation = transform.rotation;
            ogPosition.parent = transform.parent;
        }

        void Start()
        {
            if (Application.isPlaying)
            {
                SetupDepthMask();
            }

            int layer = LayerMask.NameToLayer("Level");
            CollectChildrenRecursive(transform, layer, platforms);

            GenerateColliders();
        }

        void Update()
        {
            if (punishInterval > 0 && IsPlayerVolumeInsidePit(Camera.main.transform) && !IsAnyPlayerPointInsidePlatform(Camera.main.transform))
            {
                if (timeInside >= punishInterval && Time.time >= nextWarningTime)
                {
                    // First frame entering pit → punish immediately
                    if (DreamBand.Instance != null && DreamBand.Instance is SA_DreamBand saDreamBand)
                    {
                        saDreamBand.InjurePlayer();
                    }

                    nextWarningTime = Time.time + punishInterval;
                }

                timeInside += Time.deltaTime;
            }
            else
            {
                // Reset all timers when leaving pit
                timeInside = 0f;
                nextWarningTime = 0f;
            }
        }

        public void GenerateLavaPit(List<Vector3> p)
        {
            p.Add(p[0]); // Close the loop

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

            if (!meshRenderer)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();

            // Generate vertices and UVs
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> wallTriangles = new List<int>();
            List<int> bottomTriangles = new List<int>();
            List<int> occluderTriangles = new List<int>();

            float totalWallLength = 0;
            List<float> segmentLengths = new List<float>();

            // Calculate total wall length and segment lengths
            for (int i = 0; i < p.Count; i++)
            {
                Vector3 current = transform.TransformPoint(p[i]);
                Vector3 next = transform.TransformPoint(p[(i + 1) % p.Count]);
                float segmentLength = Vector3.Distance(current, next);
                segmentLengths.Add(segmentLength);
                totalWallLength += segmentLength;
            }

            float cumulativeLength = 0;

            Vector3 midpoint = p.Aggregate(Vector3.zero, (acc, p) => acc + p) / p.Count;

            for (int i = 0; i < p.Count; i++)
            {
                Vector3 top = transform.TransformPoint(p[i]);
                Vector3 bottom = transform.TransformPoint(p[i]) - new Vector3(0, depth, 0);
                Vector3 taperedBottom = Vector3.Lerp(bottom, transform.TransformPoint(midpoint), taperAmount);

                vertices.Add(transform.InverseTransformPoint(top)); // Top vertex
                vertices.Add(transform.InverseTransformPoint(taperedBottom)); // Bottom vertex

                // UVs for walls based on cumulative length
                float u = cumulativeLength / totalWallLength;
                u = u * textureScale;
                uvs.Add(new Vector2(u, 1)); // Top UV
                uvs.Add(new Vector2(u, 0)); // Bottom UV
                cumulativeLength += segmentLengths[i];
            }

            // Generate triangles for the walls
            for (int i = 0; i < points.Count; i++)
            {
                int top1 = i * 2;
                int bottom1 = i * 2 + 1;
                int top2 = ((i + 1) % p.Count) * 2;
                int bottom2 = ((i + 1) % p.Count) * 2 + 1;

                // First triangle
                wallTriangles.Add(top1);
                wallTriangles.Add(bottom1);
                wallTriangles.Add(top2);

                // Second triangle
                wallTriangles.Add(bottom1);
                wallTriangles.Add(bottom2);
                wallTriangles.Add(top2);
            }

            // Add bottom vertices and subdivide interior
            Vector3 bottomCenter = Vector3.zero;
            foreach (var point in p)
            {
                bottomCenter += transform.TransformPoint(point);
            }
            bottomCenter /= p.Count;
            bottomCenter.y -= depth;

            Vector3 localBottomCenter = transform.InverseTransformPoint(bottomCenter);
            vertices.Add(localBottomCenter); // Add center vertex
            uvs.Add(new Vector2(0.5f, 0.5f)); // UV for center vertex

            int centerIndex = vertices.Count - 1;

            for (int i = 0; i < p.Count; i++)
            {
                int current = i * 2 + 1;
                int next = ((i + 1) % p.Count) * 2 + 1;

                bottomTriangles.Add(centerIndex);
                bottomTriangles.Add(next);
                bottomTriangles.Add(current);
            }

            // Add interior grid subdivisions
            float step = 1f / subdivisions;
            for (int i = 0; i < subdivisions; i++)
            {
                float t1 = step * i;
                float t2 = step * (i + 1);

                for (int j = 0; j < p.Count; j++)
                {
                    Vector3 edgeStart = Vector3.Lerp(vertices[j * 2 + 1], localBottomCenter, t1);
                    Vector3 edgeEnd = Vector3.Lerp(vertices[j * 2 + 1], localBottomCenter, t2);

                    vertices.Add(edgeStart);
                    vertices.Add(edgeEnd);

                    // Add UVs for subdivisions
                    uvs.Add(new Vector2(0.5f, 0.5f)); // Placeholder UV for edgeStart
                    uvs.Add(new Vector2(0.5f, 0.5f)); // Placeholder UV for edgeEnd

                    if (j > 0)
                    {
                        int prevEdgeStart = vertices.Count - 4;
                        int prevEdgeEnd = vertices.Count - 3;
                        int currEdgeStart = vertices.Count - 2;
                        int currEdgeEnd = vertices.Count - 1;

                        bottomTriangles.Add(prevEdgeStart);
                        bottomTriangles.Add(prevEdgeEnd);
                        bottomTriangles.Add(currEdgeStart);

                        bottomTriangles.Add(prevEdgeEnd);
                        bottomTriangles.Add(currEdgeEnd);
                        bottomTriangles.Add(currEdgeStart);
                    }
                }
            }

            // Add occluder walls covering the interior facing outward
            int occluderStartIndex = vertices.Count;
            for (int i = 0; i < p.Count; i++)
            {
                Vector3 top = transform.TransformPoint(p[i]);
                Vector3 occluderTop = top + new Vector3(0, 0.01f, 0); // Slightly above ground
                Vector3 occluderBottom = transform.TransformPoint(p[i]) - new Vector3(0, Mathf.Max(depth, 0.5f), 0);

                vertices.Add(transform.InverseTransformPoint(occluderTop)); // Top occluder vertex
                vertices.Add(transform.InverseTransformPoint(occluderBottom)); // Bottom occluder vertex

                // Add UVs for occluders
                float u = (float)i / p.Count;
                uvs.Add(new Vector2(u, 1)); // Top UV
                uvs.Add(new Vector2(u, 0)); // Bottom UV
            }

            for (int i = 0; i < points.Count; i++)
            {
                int top1 = occluderStartIndex + i * 2;
                int bottom1 = occluderStartIndex + i * 2 + 1;
                int top2 = occluderStartIndex + ((i + 1) % p.Count) * 2;
                int bottom2 = occluderStartIndex + ((i + 1) % p.Count) * 2 + 1;

                // Reverse triangle winding order for outward facing
                occluderTriangles.Add(top2);
                occluderTriangles.Add(bottom1);
                occluderTriangles.Add(top1);

                occluderTriangles.Add(top2);
                occluderTriangles.Add(bottom2);
                occluderTriangles.Add(bottom1);
            }

            // Combine triangles
            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.subMeshCount = 3;
            mesh.SetTriangles(wallTriangles, 0); // Walls
            mesh.SetTriangles(bottomTriangles, 1); // Bottom
            mesh.SetTriangles(occluderTriangles, 2); // Occluder
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;

            // Assign materials
            Material[] materials = new Material[3];
            materials[0] = stoneMaterial ? stoneMaterial : new Material(Shader.Find("Standard"));
            materials[1] = lavaMaterial ? lavaMaterial : new Material(Shader.Find("Standard"));
            materials[2] = occluderMaterial ? occluderMaterial : new Material(Shader.Find("Standard"));

            meshRenderer.materials = materials;
        }

        void SetupDepthMask()
        {
            if (!Application.isPlaying)
                return;

            MeshFilter referenceMeshFilter = GetComponent<MeshFilter>();

            if (referenceMeshFilter == null || referenceMeshFilter.sharedMesh == null)
            {
                Debug.LogError("SetupDepthMask: Reference MeshFilter is null or has no mesh.");
                return;
            }

            Mesh referenceMesh = referenceMeshFilter.sharedMesh;
            Bounds bounds = referenceMesh.bounds;

            MeshFilter maskMeshFilter;

            // Create a new GameObject for the depth mask
            if (depthMaskObject == null)
            {
                depthMaskObject = new GameObject("DepthMask");
                depthMaskObject.transform.SetParent(referenceMeshFilter.transform, false);
                depthMaskMeshFilter = depthMaskObject.AddComponent<MeshFilter>();
            }

            var bleedOver = 1.2f;

            // Generate a simple plane using the X and Z bounds of the reference mesh
            List<Vector3> vertices = new List<Vector3>
            {
                new Vector3(bounds.min.x - bleedOver, 0, bounds.min.z - bleedOver),
                new Vector3(bounds.max.x + bleedOver, 0, bounds.min.z - bleedOver),
                new Vector3(bounds.max.x + bleedOver, 0, bounds.max.z + bleedOver),
                new Vector3(bounds.min.x - bleedOver, 0, bounds.max.z + bleedOver)
            };

            List<Vector2> uvs = new List<Vector2>
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };

            List<int> triangles = new List<int>
            {
                0, 2, 1,  // First triangle (flipped)
                0, 3, 2   // Second triangle (flipped)
            };

            Mesh maskMesh = new Mesh
            {
                vertices = vertices.ToArray(),
                uv = uvs.ToArray(),
                subMeshCount = 1
            };
            maskMesh.SetTriangles(triangles, 0);
            maskMesh.RecalculateNormals();
            maskMesh.RecalculateBounds();

            maskMeshFilter = depthMaskObject.GetComponent<MeshFilter>();

            // Assign the new mesh to the MeshFilter
            maskMeshFilter.mesh = maskMesh;

            // Find the depth manager
            EnvironmentDepthManager environmentDepthManager = FindFirstObjectByType<EnvironmentDepthManager>();

            if (environmentDepthManager == null)
            {
                Debug.LogWarning("EnvironmentDepthManager not found in the scene.");
                return;
            }

            if (maskMesh == null || maskMesh.vertexCount == 0) return; // guard

            // Add the depth mask mesh filter if not already in the list
            if (!environmentDepthManager.MaskMeshFilters.Contains(maskMeshFilter) && maskMeshFilter != null)
            {
                environmentDepthManager.MaskMeshFilters.Add(maskMeshFilter);
            }

            environmentDepthManager.MaskMeshFilters = environmentDepthManager.MaskMeshFilters
            .Where(mf => mf != null && mf.sharedMesh != null && mf.sharedMesh.vertexCount > 0)
            .ToList();

            environmentDepthManager.MaskBias = 0.2f;
        }
        public virtual void PreProcessGenerateLavaPit()
        {
            GenerateLavaPit(new List<Vector3>(points));
        }

        public void SaveMeshIfPrefab()
        {
    #if UNITY_EDITOR
            Debug.Log("Save Mesh!");
            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;

            if (mesh == null)
            {
                Debug.LogWarning("Mesh or GameObject is null.");
                return;
            }

            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null)
            {
                Debug.Log("Not part of a prefab instance — skipping mesh save.");
                return;
            }

            GameObject prefabRoot = prefabStage.prefabContentsRoot;

            string prefabPath = prefabStage.assetPath;
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            string prefabDir = Path.GetDirectoryName(prefabPath);

            mesh.name = $"{prefabName}_Mesh";
            string meshPath = $"{prefabDir}/{prefabName}_Mesh.asset";

            Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existingMesh != null)
            {
                AssetDatabase.CreateAsset(mesh, meshPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            AssetDatabase.Refresh();

            // Assign mesh
            MeshFilter mf = gameObject.GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }

            Debug.Log($"Saved mesh to {meshPath} and assigned to prefab '{prefabName}'");
    #endif
        }

        private void OnDestroy()
        {
            if (depthMaskObject != null)
            {
                EnvironmentDepthManager environmentDepthManager = FindFirstObjectByType<EnvironmentDepthManager>();

                if (environmentDepthManager == null)
                {
                    Debug.LogWarning("EnvironmentDepthManager not found in the scene.");
                    return;
                }
                environmentDepthManager.MaskMeshFilters = environmentDepthManager.MaskMeshFilters.Except(new List<MeshFilter> { depthMaskMeshFilter }).ToList();
            }
        }

        public bool IsPlayerVolumeInsidePit(Transform playerTransform)
        {
            // All 8 corners of the cube (in local space relative to its transform)
            Vector3[] localCorners = new Vector3[]
            {
                new Vector3(-playerVolumeHalfSize.x, 0, -playerVolumeHalfSize.z),
                new Vector3(-playerVolumeHalfSize.x, 0,  playerVolumeHalfSize.z),
                new Vector3( playerVolumeHalfSize.x, 0, -playerVolumeHalfSize.z),
                new Vector3( playerVolumeHalfSize.x, 0,  playerVolumeHalfSize.z),
            };

            foreach (var cornerLocal in localCorners)
            {
                // Convert corner to world, then to pit local space
                Vector3 worldCorner = playerTransform.TransformPoint(cornerLocal);
                Vector3 localToPit = transform.InverseTransformPoint(worldCorner);

                // Check if corner is inside polygon
                if (!IsPointInPolygon(localToPit, points))
                {
                    return false; // If any corner is outside, cube is not fully inside
                }
            }

            return true; // All corners inside
        }

        public bool IsPlayerPointInsidePit(Transform playerTransform)
        {
            // Convert player position to local space of the pit
            Vector3 localPos = transform.InverseTransformPoint(playerTransform.position);

            // Run point-in-polygon test
            return IsPointInPolygon(localPos, points);
        }

        public bool IsAnyPlayerPointInsidePlatform(Transform playerTransform)
        {
            foreach (GameObject platform in platforms)
            {
                if (platform.IsDestroyed())
                    continue;
                Bounds bounds = GetCombinedBoundsXZ(platform.transform, true);
                if (IsPointInBounds(playerTransform.position, bounds))
                {
                    return true;
                }
            }

            return false;
        }

        // Classic even-odd rule point-in-polygon test
        private bool IsPointInPolygon(Vector3 point, List<Vector3> polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                bool intersect = ((polygon[i].z > point.z) != (polygon[j].z > point.z)) &&
                                 (point.x < (polygon[j].x - polygon[i].x) * (point.z - polygon[i].z) /
                                 (polygon[j].z - polygon[i].z) + polygon[i].x);
                if (intersect)
                    inside = !inside;
            }
            return inside;
        }

        private bool IsPointInBounds(Vector3 point, Bounds bounds)
        {
            // Only X-Z plane check
            return point.x >= bounds.min.x && point.x <= bounds.max.x &&
                   point.z >= bounds.min.z && point.z <= bounds.max.z;
        }

        public Bounds GetCombinedBoundsXZ(Transform root, bool includeChildren = true)
        {
            // Fetch components once — no new array allocations per call
            _platformColliders.Clear();
            _platformRenderers.Clear();

            if (includeChildren)
            {
                root.GetComponentsInChildren(true, _platformColliders);
                root.GetComponentsInChildren(true, _platformRenderers);
            }
            else
            {
                root.GetComponents(_platformColliders);
                root.GetComponents(_platformRenderers);
            }

            if (_platformColliders.Count == 0 && _platformRenderers.Count == 0)
                return new Bounds(root.position, Vector3.zero);

            Bounds combined = _platformColliders.Count > 0 ? _platformColliders[0].bounds : _platformRenderers[0].bounds;

            // Combine colliders
            for (int i = 0; i < _platformColliders.Count; i++)
                combined.Encapsulate(_platformColliders[i].bounds);

            // Combine renderers
            for (int i = 0; i < _platformRenderers.Count; i++)
                combined.Encapsulate(_platformRenderers[i].bounds);

            return combined;
        }

        void CollectChildrenRecursive(Transform parent, int layer, List<GameObject> list)
        {
            foreach (Transform child in parent)
            {
                if (child.gameObject.layer == layer)
                    list.Add(child.gameObject);

                CollectChildrenRecursive(child, layer, list);
            }
        }

        public void GenerateColliders()
        {
            // Ensure we have a MeshFilter with a valid mesh
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogWarning($"{name}: No mesh found to generate colliders.");
                return;
            }

            Mesh mesh = mf.sharedMesh;

            // --- Clean up any previous colliders ---
            var oldColliders = GetComponentsInChildren<MeshCollider>().ToList();
            foreach (var c in oldColliders)
            {
        #if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(c.gameObject);
                else
        #endif
                    Destroy(c.gameObject);
            }

            // --- WALL COLLIDER (submesh 0) ---
            GameObject wallColliderObj = new GameObject("WallCollider");
            wallColliderObj.transform.SetParent(transform, false);
            wallColliderObj.layer = LayerMask.NameToLayer("Level");
            wallColliderObj.tag = "Ground";

            Mesh wallMesh = new Mesh();
            wallMesh.vertices = mesh.vertices;
            wallMesh.triangles = mesh.GetTriangles(0); // submesh 0 = wall triangles
            wallMesh.RecalculateNormals();
            wallMesh.RecalculateBounds();

            MeshCollider wallCollider = wallColliderObj.AddComponent<MeshCollider>();
            wallCollider.sharedMesh = wallMesh;
            wallCollider.convex = false;

            // --- LAVA COLLIDER (submesh 1) ---
            GameObject lavaColliderObj = new GameObject("LavaCollider");
            lavaColliderObj.transform.SetParent(transform, false);
            lavaColliderObj.layer = LayerMask.NameToLayer("Level");
            lavaColliderObj.tag = "Lava";

            Mesh lavaMesh = new Mesh();
            lavaMesh.vertices = mesh.vertices;
            lavaMesh.triangles = mesh.GetTriangles(1); // submesh 1 = bottom/lava
            lavaMesh.RecalculateNormals();
            lavaMesh.RecalculateBounds();

            MeshCollider lavaCollider = lavaColliderObj.AddComponent<MeshCollider>();
            lavaCollider.sharedMesh = lavaMesh;
            lavaCollider.convex = false;

            Debug.Log($"{name}: Added wall + lava colliders.");
        }
    }
}
