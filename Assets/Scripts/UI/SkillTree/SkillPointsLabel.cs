using UnityEngine;
using TMPro;

public class SkillPointsLabel : MonoBehaviour
{
    public TMP_Text label;
    [Tooltip("Format string: {0}=total available, {1}=left to spend")] public string format = "{0}/{1} skill points";

    void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        Refresh();
    }

    void OnEnable()
    {
        SkillTreeState.OnUnlocked += OnChanged;
        Refresh();
    }

    void OnDisable()
    {
        SkillTreeState.OnUnlocked -= OnChanged;
    }

    void OnChanged(SkillNodeDefinition _)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (label == null) return;
        int total = SkillTreeState.TotalPoints;
        int remaining = SkillTreeState.RemainingPoints;
        label.text = string.Format(format, total, remaining);
    }
}


