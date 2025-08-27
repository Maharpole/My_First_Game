using UnityEngine;

public class MobManager : MonoBehaviour
{
    public static MobManager Instance { get; private set; }

    [Header("Roots")]
    public Transform mobsRoot;
    public Transform spawnersRoot;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureRoots();
    }

    void EnsureRoots()
    {
        if (mobsRoot == null)
        {
            var go = new GameObject("Mobs");
            go.transform.SetParent(transform, false);
            mobsRoot = go.transform;
        }
        if (spawnersRoot == null)
        {
            var go = new GameObject("Spawners");
            go.transform.SetParent(transform, false);
            spawnersRoot = go.transform;
        }
    }

    public static Transform MobsRoot
    {
        get
        {
            var mgr = GetOrCreate();
            return mgr.mobsRoot;
        }
    }

    public static Transform SpawnersRoot
    {
        get
        {
            var mgr = GetOrCreate();
            return mgr.spawnersRoot;
        }
    }

    public static void ParentMob(Transform t)
    {
        if (t == null) return;
        t.SetParent(MobsRoot, true);
    }

    public static void ParentSpawner(Transform t)
    {
        if (t == null) return;
        t.SetParent(SpawnersRoot, true);
    }

    static MobManager GetOrCreate()
    {
        if (Instance != null) return Instance;
        var existing = Object.FindFirstObjectByType<MobManager>();
        if (existing != null)
        {
            Instance = existing;
            Instance.EnsureRoots();
            return Instance;
        }
        var root = new GameObject("Mob Manager");
        Instance = root.AddComponent<MobManager>();
        Instance.EnsureRoots();
        return Instance;
    }
}



