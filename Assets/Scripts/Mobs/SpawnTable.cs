using UnityEngine;
using System;

[CreateAssetMenu(fileName = "SpawnTable", menuName = "POE Game/Mobs/Spawn Table")]
public class SpawnTable : ScriptableObject
{
    [Serializable]
    public class MobEntry
    {
        public MobType mobType;
        public int weight = 100;
    }

    [Serializable]
    public class ModifierEntry
    {
        public MobModifier modifier;
        public int weight = 100;
    }

    public MobEntry[] mobs;
    public ModifierEntry[] modifiers;

    [Header("Global Modifier Count (per pack)")]
    public int minModifiers = 0;
    public int maxModifiers = 2;
}
