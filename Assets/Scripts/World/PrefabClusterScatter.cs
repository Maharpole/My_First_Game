using UnityEngine;
using System.Collections.Generic;

public class PrefabClusterScatter : MonoBehaviour
{
	[Header("Terrain & Layers")]
	public Terrain terrain;
	public LayerMask groundMask = ~0; // default: Everything; best to set to Ground
	public bool useRaycastToGround = true; // raycast down for exact collider point
	public float rayHeight = 50f;

	[Header("Region (Mesh Mode)")]
	public BoxCollider scatterRegion; // used when terrain is null; defines world-space bounds

	[Header("Prefabs")]
	public GameObject[] prefabs;
	public bool randomYRotation = true;
	public Vector2 uniformScaleRange = new Vector2(1f, 1f);
	public bool alignToGroundNormal = false;

	[Header("Clusters")]
	public int clusterCount = 15;
	public Vector2Int instancesPerCluster = new Vector2Int(8, 20);
	public float clusterRadius = 12f;
	public bool gaussianDistribution = true; // denser in center

	[Header("Placement Filters")]
	[Range(0f, 90f)] public float maxSteepness = 35f;
	public float minHeight = float.NegativeInfinity;
	public float maxHeight = float.PositiveInfinity;

	[Header("Overlap Control")]
	public bool preventOverlap = true;
	public float overlapRadius = 0.5f;
	[Tooltip("Layers considered for overlap checks. Exclude Ground layers to avoid blocking spawns.")]
	public LayerMask overlapMask = 0; // if 0, we will auto-use ~groundMask at runtime
	[Tooltip("Also check already placed instances to avoid stacking even if physics does not detect an overlap.")]
	public bool checkAgainstSelf = true;

	[Header("Parenting & Cleanup")]
	public Transform parentForInstances; // default: this.transform
	public bool clearExistingOnScatter = true;

	[Header("Randomness")]
	public int seed = -1; // -1 => vary every time; >=0 => deterministic

	[Header("Debugging")]
	public bool debugVerbose = false;
	public bool debugDraw = false;
	public float debugDrawSeconds = 5f;

	[Header("Lifecycle")]
	public bool autoScatterOnStart = false;
	public float startScatterDelay = 0f;

	void Start()
	{
		if (autoScatterOnStart)
		{
			if (startScatterDelay <= 0f) ScatterClusters();
			else Invoke(nameof(ScatterClusters), startScatterDelay);
		}
	}

	const string ContainerName = "__ClusterScatter_Instances";
	Transform container;

	[ContextMenu("Scatter Clusters")]
	public void ScatterClusters()
	{
		bool terrainMode = terrain != null && terrain.terrainData != null;
		if (!terrainMode && scatterRegion == null)
		{
			Debug.LogWarning("[PrefabClusterScatter] Assign a Terrain or a BoxCollider scatterRegion.");
			return;
		}
		if (prefabs == null || prefabs.Length == 0)
		{
			Debug.LogWarning("[PrefabClusterScatter] No prefabs assigned.");
			return;
		}

		SetupContainer();
		if (clearExistingOnScatter) ClearContainer();

		// Seed
		if (seed >= 0) Random.InitState(seed);
		else Random.InitState(System.Environment.TickCount ^ System.DateTime.Now.GetHashCode());

		var td = terrainMode ? terrain.terrainData : null;
		Vector3 size = terrainMode ? td.size : Vector3.zero;
		Bounds region = scatterRegion != null ? scatterRegion.bounds : new Bounds();

		if (debugVerbose)
		{
			string mode = terrainMode ? "Terrain" : "Region";
			string area = terrainMode ? $"terrainSize={size}" : $"regionBounds={region}";
			Debug.Log($"[PrefabClusterScatter] Begin mode={mode} {area} prefabs={prefabs.Length} clusters={clusterCount} perCluster={instancesPerCluster.x}-{instancesPerCluster.y} radius={clusterRadius} groundMask={(int)groundMask} overlapMask={(int)overlapMask} useRaycast={useRaycastToGround}");
			if (((int)groundMask) == 0)
			{
				Debug.LogWarning("[PrefabClusterScatter] groundMask is 0; raycasts will hit nothing.");
			}
		}
		int placedTotal = 0;
		int totalAttempts = 0, rayMisses = 0, heightRejects = 0, slopeRejects = 0, overlapRejects = 0, nullPrefabRejects = 0;
		List<Vector3> placedPositions = checkAgainstSelf ? new List<Vector3>(clusterCount * instancesPerCluster.y) : null;

		for (int c = 0; c < clusterCount; c++)
		{
			Vector3 worldCenter;
			if (terrainMode)
			{
				float centerNx = Random.value;
				float centerNz = Random.value;
				worldCenter = new Vector3(centerNx * size.x, 0f, centerNz * size.z) + terrain.transform.position;
				worldCenter.y = terrain.SampleHeight(worldCenter) + terrain.transform.position.y;
			}
			else
			{
				worldCenter = new Vector3(
					Random.Range(region.min.x, region.max.x),
					region.center.y,
					Random.Range(region.min.z, region.max.z));
			}

			int count = Random.Range(instancesPerCluster.x, instancesPerCluster.y + 1);
			int safety = 0;
			for (int i = 0; i < count && safety++ < count * 10; i++)
			{
				// Scatter inside a circle
				Vector2 off = gaussianDistribution ? RandomInsideCircleGaussian() : RandomInsideCircleUniform();
				off *= clusterRadius;
				Vector3 pos = worldCenter + new Vector3(off.x, 0f, off.y);

				// Clamp to bounds
				if (terrainMode)
				{
					pos.x = Mathf.Clamp(pos.x, terrain.transform.position.x, terrain.transform.position.x + size.x);
					pos.z = Mathf.Clamp(pos.z, terrain.transform.position.z, terrain.transform.position.z + size.z);
				}
				else if (scatterRegion != null)
				{
					pos.x = Mathf.Clamp(pos.x, region.min.x, region.max.x);
					pos.z = Mathf.Clamp(pos.z, region.min.z, region.max.z);
				}

				// Find ground point & normal
				Vector3 groundPos; Vector3 normal;
				if (!TryProjectToGround(pos, out groundPos, out normal, td))
				{
					rayMisses++;
					if (debugVerbose) Debug.Log($"[PrefabClusterScatter] Ray miss at {pos}");
					continue;
				}

				// Filters: height & slope
				if (groundPos.y < minHeight || groundPos.y > maxHeight) { heightRejects++; if (debugVerbose) Debug.Log($"[PrefabClusterScatter] Height reject y={groundPos.y}"); continue; }
				float steep = 0f;
				if (terrainMode)
				{
					float nx = (groundPos.x - terrain.transform.position.x) / size.x;
					float nz = (groundPos.z - terrain.transform.position.z) / size.z;
					steep = td.GetSteepness(nx, nz);
				}
				else
				{
					// approximate slope from surface normal
					steep = Mathf.Acos(Mathf.Clamp(Vector3.Dot(Vector3.up, normal.normalized), -1f, 1f)) * Mathf.Rad2Deg;
				}
				if (steep > maxSteepness) { slopeRejects++; if (debugVerbose) Debug.Log($"[PrefabClusterScatter] Slope reject {steep:0.0}>", this); continue; }

				// Overlap check (exclude ground). If no mask set, derive from ~groundMask
				if (preventOverlap)
				{
					int mask = overlapMask.value != 0 ? overlapMask.value : ~groundMask.value;
					if (mask != 0)
					{
						if (Physics.CheckSphere(groundPos + Vector3.up * 0.1f, Mathf.Max(0.01f, overlapRadius), mask, QueryTriggerInteraction.Ignore))
						{
							overlapRejects++;
							if (debugVerbose) Debug.Log($"[PrefabClusterScatter] Overlap reject at {groundPos}");
							continue;
						}
					}
				}

				// Self-overlap guard
				if (checkAgainstSelf && placedPositions != null)
				{
					float r2 = Mathf.Max(0.001f, overlapRadius) * Mathf.Max(0.001f, overlapRadius);
					bool tooClose = false;
					for (int pp = 0; pp < placedPositions.Count; pp++)
					{
						Vector3 d = placedPositions[pp] - groundPos;
						d.y = 0f; // horizontal proximity only
						if (d.sqrMagnitude < r2) { tooClose = true; break; }
					}
					if (tooClose)
					{
						overlapRejects++;
						if (debugVerbose) Debug.Log($"[PrefabClusterScatter] Self-overlap reject at {groundPos}");
						continue;
					}
				}

				// Spawn
				var prefab = prefabs[Random.Range(0, prefabs.Length)];
				if (prefab == null) { nullPrefabRejects++; if (debugVerbose) Debug.Log("[PrefabClusterScatter] Null prefab entry"); continue; }
				var go = Instantiate(prefab, groundPos, Quaternion.identity, container);
				// Rotation
				Quaternion rot = Quaternion.identity;
				if (alignToGroundNormal)
				{
					rot = Quaternion.FromToRotation(Vector3.up, normal);
				}
				if (randomYRotation)
				{
					rot = rot * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
				}
				go.transform.rotation = rot;
				// Scale
				float s = Mathf.Lerp(uniformScaleRange.x, uniformScaleRange.y, Random.value);
				go.transform.localScale = Vector3.one * Mathf.Max(0.0001f, s);

				placedTotal++;
				if (checkAgainstSelf && placedPositions != null) placedPositions.Add(groundPos);
				totalAttempts++;
				if (debugDraw) Debug.DrawRay(groundPos + Vector3.up * 2f, Vector3.down * 3f, Color.green, debugDrawSeconds);
			}
		}

		Debug.Log($"[PrefabClusterScatter] Placed {placedTotal} instances in {clusterCount} clusters. Attempts={totalAttempts} RayMisses={rayMisses} HeightRejects={heightRejects} SlopeRejects={slopeRejects} OverlapRejects={overlapRejects} NullPrefabs={nullPrefabRejects}");
	}

	void SetupContainer()
	{
		if (parentForInstances == null) parentForInstances = this.transform;
		var existing = parentForInstances.Find(ContainerName);
		container = existing != null ? existing : new GameObject(ContainerName).transform;
		container.SetParent(parentForInstances, false);
	}

	void ClearContainer()
	{
		if (container == null) return;
		for (int i = container.childCount - 1; i >= 0; i--)
		{
			var ch = container.GetChild(i);
			if (Application.isEditor && !Application.isPlaying)
				DestroyImmediate(ch.gameObject);
			else
				Destroy(ch.gameObject);
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
					// ignore self & scatter region colliders
					if (h.collider.transform.IsChildOf(transform)) continue;
					if (scatterRegion != null && (h.collider == scatterRegion || h.collider.transform.IsChildOf(scatterRegion.transform))) continue;
					ground = h.point;
					normal = h.normal.sqrMagnitude > 0.0001f ? h.normal : Vector3.up;
					return true;
				}
			}
			ground = Vector3.zero; normal = Vector3.up; return false;
		}
		else
		{
			// Terrain sampling only
			Vector3 size = td.size;
			float nx = (probe.x - terrain.transform.position.x) / size.x;
			float nz = (probe.z - terrain.transform.position.z) / size.z;
			float h = td.GetInterpolatedHeight(nx, nz) + terrain.transform.position.y;
			ground = new Vector3(probe.x, h, probe.z);
			normal = td.GetInterpolatedNormal(nx, nz);
			return true;
		}
	}

	Vector2 RandomInsideCircleUniform()
	{
		float t = 2f * Mathf.PI * Random.value;
		float r = Mathf.Sqrt(Random.value);
		return new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * r;
	}

	Vector2 RandomInsideCircleGaussian()
	{
		// Box-Muller transform to get normal distribution, clamp radius to ~1
		float u1 = Mathf.Max(1e-6f, Random.value);
		float u2 = Random.value;
		float mag = Mathf.Sqrt(-2.0f * Mathf.Log(u1));
		float z0 = mag * Mathf.Cos(2.0f * Mathf.PI * u2);
		float z1 = mag * Mathf.Sin(2.0f * Mathf.PI * u2);
		Vector2 v = new Vector2(z0, z1);
		if (v.sqrMagnitude > 1f) v.Normalize();
		return v;
	}
}


