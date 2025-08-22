using UnityEngine;

// Bridges data assets to scene objects safely.
// Listens for unlock events and applies effects via PlayerSkillHooks on the live Player.
public class PlayerSkillNodeApplier : MonoBehaviour
{
    [Header("Node Assets")]
    public SkillNodeData reflectorClassDefinitionNode; // assign the asset for the first node

    [Header("Targets")]
    public PlayerSkillHooks skillHooks; // assign the Player's component

    void Awake()
    {
        if (skillHooks == null) skillHooks = FindFirstObjectByType<PlayerSkillHooks>();
    }

    void OnEnable()
    {
        SkillTreeState.OnUnlocked += HandleUnlocked;
        // Apply on load if already unlocked
        if (reflectorClassDefinitionNode != null && SkillTreeState.IsUnlocked(reflectorClassDefinitionNode))
        {
            ApplyReflectorDefinition();
        }
    }

    void OnDisable()
    {
        SkillTreeState.OnUnlocked -= HandleUnlocked;
    }

    void HandleUnlocked(SkillNodeData node)
    {
        if (node == null || skillHooks == null) return;
        if (node == reflectorClassDefinitionNode)
        {
            ApplyReflectorDefinition();
        }
    }

    void ApplyReflectorDefinition()
    {
        if (skillHooks == null) return;
        // Velocity affects player size
        skillHooks.EnableVelocityScale();
        // +10 flat reflect
        skillHooks.AddReflectFlat(10);
        // +25 max health and recompute
        skillHooks.AddMaxHealthFlat(25);
        // +10 regen/sec
        skillHooks.AddRegenPerSecond(10);
        // Recompute to refresh UI and clamps
        skillHooks.Recompute();
        Debug.Log("[PlayerSkillNodeApplier] Applied Reflector_Class_DefinitionNode effects.");
    }
}


