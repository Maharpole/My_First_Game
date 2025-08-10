using UnityEngine;

[CreateAssetMenu(fileName = "EnemyType", menuName = "POE Game/Mobs/Enemy Type")]
public class EnemyType : ScriptableObject
{
    public string enemyName;
    public GameObject prefab;

    [Header("Base Overrides (optional)")]
    public float moveSpeedOverride = -1f; // <0 means use prefab's default
    public int maxHealthOverride = -1;    // <0 means use prefab's default
}
