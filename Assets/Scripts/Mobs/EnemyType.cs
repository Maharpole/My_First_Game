using UnityEngine;

[CreateAssetMenu(fileName = "EnemyType", menuName = "POE Game/Mobs/Enemy Type")]
public class EnemyType : ScriptableObject
{
    public string enemyName;
    public GameObject prefab;
}
