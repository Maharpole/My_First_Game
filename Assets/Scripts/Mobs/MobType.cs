using UnityEngine;
using System;

[CreateAssetMenu(fileName = "MobType", menuName = "POE Game/Mobs/Mob Type")]
public class MobType : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public EnemyType enemyType;
        public int weight = 100; // for composition selection
    }

    public string mobName;

    [Header("Pack Size")] 
    public int minCount = 3;
    public int maxCount = 8;

    [Header("Composition")] 
    public Entry[] composition; // weighted choices among EnemyTypes
}
