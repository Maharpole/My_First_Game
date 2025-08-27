using UnityEngine;

public class EnemyIdentity : MonoBehaviour
{
    [Header("Source Type (optional)")]
    public EnemyType typeAsset; // ScriptableObject source for name/prefab type

    [Header("Overrides")]
    public string overrideName; // Fallback name if no type asset

    public string GetDisplayName()
    {
        if (typeAsset != null && !string.IsNullOrEmpty(typeAsset.enemyName)) return typeAsset.enemyName;
        if (!string.IsNullOrEmpty(overrideName)) return overrideName;
        return gameObject != null ? gameObject.name : string.Empty;
    }
}



