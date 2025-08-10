using UnityEngine;

[CreateAssetMenu(fileName = "MapConfig", menuName = "POE Game/Mobs/Map Config")]
public class MapConfig : ScriptableObject
{
    [Header("Spawn Bounds")]
    public Bounds worldBounds = new Bounds(Vector3.zero, new Vector3(100, 0, 100));
    public LayerMask groundMask;

    [Header("Density & Counts")]
    public int minPacks = 10;
    public int maxPacks = 20;
    public float minPackSpacing = 8f;

    [Header("Seed")]
    public int seed = 12345;
}
