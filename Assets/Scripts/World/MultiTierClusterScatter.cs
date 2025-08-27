using UnityEngine;
using System.Collections.Generic;

// Spawns a primary layer (e.g., rocks + trees), then spawns secondary layers (e.g., plants, mushrooms)
// around each primary instance within a ring. Supports either Terrain or a BoxCollider region.
public class MultiTierClusterScatter : MonoBehaviour
{
    [Header("Ground & Region")]
    public Terrain terrain;                   // optional
    public BoxCollider scatterRegion;         // used when terrain is null
    public LayerMask groundMask = ~0;         // include ground layers
    public bool useRaycastToGround = true;
    public float rayHeight = 50f;

    [Header("Primary Layer (anchors)")]
    public GameObject[] primaryPrefabs;       // e.g., Trees + Rocks
    public int primaryClusterCount = 12;
    public Vector2Int primaryPerCluster = new Vector2Int(4, 10);
    public float primaryClusterRadius = 10f;
    [Range(0,90)] public float maxSteepness = 35f;
    public float minHeight = float.NegativeInfinity;
    public float maxHeight = float.PositiveInfinity;
    public bool randomYRotation = true;
    public Vector2 uniformScaleRange = new Vector2(1f,1f);
    public bool alignToGroundNormal = false;
    public bool preventOverlap = true;
    public float overlapRadius = 0.75f;
    public LayerMask overlapMask = 0;         // if 0, will use ~groundMask

    [System.Serializable]
    public class SecondaryLayer
    {
        public string name = "Plants";
        public GameObject[] prefabs;
        public Vector2Int countPerAnchor = new Vector2Int(3, 8);
        public float ringRadiusMin = 1.5f;
        public float ringRadiusMax = 3.5f;
        public bool randomYRotation = true;
        public Vector2 uniformScaleRange = new Vector2(0.8f, 1.2f);
        public bool alignToGroundNormal = true;
        public bool preventOverlap = true;
        public float overlapRadius = 0.35f;
        public LayerMask overlapMask = 0;     // if 0, will use ~groundMask
    }

    [Header("Secondary Layers (around primary anchors)")]
    public SecondaryLayer[] secondaryLayers;

    [Header("Parenting & Random")] 
    public Transform parentRoot;
    public bool clearExistingOnScatter = true;
    public int seed = -1; // -1 => vary every time

    [Header("Auto & Debug")] 
    public bool autoScatterOnStart = false; public float startDelay = 0f;
    public bool debugVerbose = false; public bool debugDraw = false; public float debugDrawSeconds = 5f;

    const string RootPrimary = "Primary";
    const string RootSecondary = "Secondary";
    Transform _rootPrimary; Transform _rootSecondary;

    void Start()
    {
        if (autoScatterOnStart)
        {
            if (startDelay <= 0f) Scatter();
            else Invoke(nameof(Scatter), startDelay);
        }
    }

    [ContextMenu("Scatter Multi Tier")]
    public void Scatter()
    {
        bool terrainMode = terrain != null && terrain.terrainData != null;
        if (!terrainMode && scatterRegion == null)
        {
            Debug.LogWarning("[MultiTierClusterScatter] Assign a Terrain or a BoxCollider region.");
            return;
        }
        if (primaryPrefabs == null || primaryPrefabs.Length == 0)
        {
            Debug.LogWarning("[MultiTierClusterScatter] No primary prefabs.");
            return;
        }

        if (seed >= 0) Random.InitState(seed); else Random.InitState(System.Environment.TickCount ^ System.DateTime.Now.GetHashCode());

        SetupRoots();
        if (clearExistingOnScatter) { Clear(_rootPrimary); Clear(_rootSecondary); }

        var td = terrainMode ? terrain.terrainData : null;
        Vector3 tSize = terrainMode ? td.size : Vector3.zero;
        Bounds region = scatterRegion != null ? scatterRegion.bounds : new Bounds();

        if (debugVerbose)
        {
            string mode = terrainMode ? "Terrain" : "Region";
            Debug.Log($"[MultiTierClusterScatter] Begin mode={mode} primaryClusters={primaryClusterCount} perCluster={primaryPerCluster.x}-{primaryPerCluster.y} primaryRadius={primaryClusterRadius} groundMask={(int)groundMask}");
        }

        // ---- Primary pass ----
        List<Transform> anchors = new List<Transform>();
        int attempts = 0, rayMisses = 0, heightRejects = 0, slopeRejects = 0, overlapRejects = 0, nullPrefabRejects = 0;
        for (int c = 0; c < primaryClusterCount; c++)
        {
            Vector3 center = terrainMode
                ? new Vector3(Random.value * tSize.x, 0f, Random.value * tSize.z) + terrain.transform.position
                : new Vector3(Random.Range(region.min.x, region.max.x), region.center.y, Random.Range(region.min.z, region.max.z));
            if (terrainMode) center.y = terrain.SampleHeight(center) + terrain.transform.position.y;

            int count = Random.Range(primaryPerCluster.x, primaryPerCluster.y + 1);
            int safety = 0;
            for (int i = 0; i < count && safety++ < count * 10; i++)
            {
                Vector2 off = Random.insideUnitCircle * primaryClusterRadius;
                Vector3 pos = center + new Vector3(off.x, 0f, off.y);
                // clamp to bounds
                if (terrainMode)
                {
                    pos.x = Mathf.Clamp(pos.x, terrain.transform.position.x, terrain.transform.position.x + tSize.x);
                    pos.z = Mathf.Clamp(pos.z, terrain.transform.position.z, terrain.transform.position.z + tSize.z);
                }
                else
                {
                    pos.x = Mathf.Clamp(pos.x, region.min.x, region.max.x);
                    pos.z = Mathf.Clamp(pos.z, region.min.z, region.max.z);
                }

                attempts++;
                if (!TryProjectToGround(pos, out var groundPos, out var normal, td)) { rayMisses++; if (debugVerbose) Debug.Log($"[MultiTier] Ray miss at {pos}"); continue; }
                if (groundPos.y < minHeight || groundPos.y > maxHeight) { heightRejects++; if (debugVerbose) Debug.Log($"[MultiTier] Height reject y={groundPos.y}"); continue; }
                float steep = terrainMode ? td.GetSteepness((groundPos.x - terrain.transform.position.x)/tSize.x, (groundPos.z - terrain.transform.position.z)/tSize.z)
                                          : Mathf.Acos(Mathf.Clamp(Vector3.Dot(Vector3.up, normal.normalized), -1f, 1f)) * Mathf.Rad2Deg;
                if (steep > maxSteepness) { slopeRejects++; if (debugVerbose) Debug.Log($"[MultiTier] Slope reject {steep:0.0}"); continue; }

                // Physics overlap vs everything but ground
                if (preventOverlap)
                {
                    int mask = overlapMask.value != 0 ? overlapMask.value : ~groundMask.value;
                    if (mask != 0 && Physics.CheckSphere(groundPos + Vector3.up * 0.1f, Mathf.Max(0.01f, overlapRadius), mask, QueryTriggerInteraction.Ignore))
                    { overlapRejects++; if (debugVerbose) Debug.Log($"[MultiTier] Overlap reject at {groundPos}"); continue; }
                }

                var prefab = primaryPrefabs[Random.Range(0, primaryPrefabs.Length)];
                if (prefab == null) { nullPrefabRejects++; if (debugVerbose) Debug.Log("[MultiTier] Null prefab entry"); continue; }
                var go = Instantiate(prefab, groundPos, Quaternion.identity, _rootPrimary);
                Quaternion rot = alignToGroundNormal ? Quaternion.FromToRotation(Vector3.up, normal) : Quaternion.identity;
                if (randomYRotation) rot = rot * Quaternion.Euler(0f, Random.Range(0f,360f), 0f);
                go.transform.rotation = rot;
                float s = Mathf.Lerp(uniformScaleRange.x, uniformScaleRange.y, Random.value);
                go.transform.localScale = Vector3.one * Mathf.Max(0.0001f, s);

                anchors.Add(go.transform);
                if (debugDraw) Debug.DrawRay(groundPos + Vector3.up * 2f, Vector3.down * 3f, Color.green, debugDrawSeconds);
            }
        }

        // ---- Secondary passes ----
        if (secondaryLayers != null)
        {
            if (debugVerbose) Debug.Log($"[MultiTierClusterScatter] Anchors={anchors.Count} secondaryLayers={secondaryLayers.Length}");
            foreach (var layer in secondaryLayers)
            {
                if (layer == null || layer.prefabs == null || layer.prefabs.Length == 0) continue;
                var layerRoot = new GameObject(string.IsNullOrEmpty(layer.name) ? "SecondaryLayer" : layer.name).transform;
                layerRoot.SetParent(_rootSecondary, false);

                foreach (var anchor in anchors)
                {
                    int count = Random.Range(layer.countPerAnchor.x, layer.countPerAnchor.y + 1);
                    int safety = 0;
                    for (int i = 0; i < count && safety++ < count * 10; i++)
                    {
                        float r = Random.Range(layer.ringRadiusMin, layer.ringRadiusMax);
                        float ang = Random.Range(0f, Mathf.PI * 2f);
                        Vector3 pos = anchor.position + new Vector3(Mathf.Cos(ang)*r, 0f, Mathf.Sin(ang)*r);

                        if (!TryProjectToGround(pos, out var groundPos, out var normal, td)) continue;
                        if (groundPos.y < minHeight || groundPos.y > maxHeight) continue;

                        if (layer.preventOverlap)
                        {
                            int mask = layer.overlapMask.value != 0 ? layer.overlapMask.value : ~groundMask.value;
                            if (mask != 0 && Physics.CheckSphere(groundPos + Vector3.up * 0.05f, Mathf.Max(0.01f, layer.overlapRadius), mask, QueryTriggerInteraction.Ignore))
                                continue;
                        }

                        var prefab = layer.prefabs[Random.Range(0, layer.prefabs.Length)];
                        if (prefab == null) continue;
                        var go = Instantiate(prefab, groundPos, Quaternion.identity, layerRoot);
                        Quaternion rot = layer.alignToGroundNormal ? Quaternion.FromToRotation(Vector3.up, normal) : Quaternion.identity;
                        if (layer.randomYRotation) rot = rot * Quaternion.Euler(0f, Random.Range(0f,360f), 0f);
                        go.transform.rotation = rot;
                        float s = Mathf.Lerp(layer.uniformScaleRange.x, layer.uniformScaleRange.y, Random.value);
                        go.transform.localScale = Vector3.one * Mathf.Max(0.0001f, s);
                        if (debugDraw) Debug.DrawRay(groundPos + Vector3.up * 1.5f, Vector3.down * 2.5f, Color.cyan, debugDrawSeconds);
                    }
                }
            }
        }

        if (debugVerbose || anchors.Count == 0)
        {
            Debug.Log($"[MultiTierClusterScatter] Summary: anchors={anchors.Count} attempts={attempts} rayMisses={rayMisses} heightRejects={heightRejects} slopeRejects={slopeRejects} overlapRejects={overlapRejects} nullPrefabs={nullPrefabRejects}");
        }
    }

    void SetupRoots()
    {
        if (parentRoot == null) parentRoot = transform;
        _rootPrimary = FindOrCreate(parentRoot, RootPrimary);
        _rootSecondary = FindOrCreate(parentRoot, RootSecondary);
    }

    Transform FindOrCreate(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) return t;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    void Clear(Transform root)
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var ch = root.GetChild(i);
            if (Application.isEditor && !Application.isPlaying) DestroyImmediate(ch.gameObject);
            else Destroy(ch.gameObject);
        }
    }

    bool TryProjectToGround(Vector3 probe, out Vector3 ground, out Vector3 normal, TerrainData td)
    {
        if (useRaycastToGround)
        {
            Vector3 origin = probe + Vector3.up * Mathf.Max(1f, rayHeight);
            var hits = Physics.RaycastAll(origin, Vector3.down, rayHeight * 2f, groundMask, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    var h = hits[i];
                    if (h.collider == null) continue;
                    if (scatterRegion != null && (h.collider == scatterRegion || h.collider.transform.IsChildOf(scatterRegion.transform))) continue;
                    if (h.collider.transform.IsChildOf(transform)) continue;
                    ground = h.point; normal = h.normal.sqrMagnitude > 0.0001f ? h.normal : Vector3.up; return true;
                }
            }
            ground = Vector3.zero; normal = Vector3.up; return false;
        }
        else if (terrain != null && td != null)
        {
            Vector3 size = td.size;
            float nx = (probe.x - terrain.transform.position.x) / size.x;
            float nz = (probe.z - terrain.transform.position.z) / size.z;
            float h = td.GetInterpolatedHeight(nx, nz) + terrain.transform.position.y;
            ground = new Vector3(probe.x, h, probe.z);
            normal = td.GetInterpolatedNormal(nx, nz);
            return true;
        }
        ground = probe; normal = Vector3.up; return true;
    }
}


