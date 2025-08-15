using System;

[Serializable]
public class GeneratedAffix
{
    public string affixId;
    public string tierName;
    public bool isPrefix;
    public StatType statType;
    [System.Obsolete("Removed. StatType inherently defines value kind.")] public bool isPercentage; // retained for backward compatibility; StatTypeInfo determines effective percent
    public float value;
    // Range info for the chosen tier to enable extended tooltips
    public float tierMin;
    public float tierMax;
    public string displayName;
    public string modGroup;
}
