using UnityEngine;

public abstract class BulletEffect : ScriptableObject
{
    // Strongly-typed, per-effect parameters shown in Weapon profiles via SerializeReference
    [System.Serializable]
    public abstract class EffectParams { }

    // Called by the editor/runtime to create a default, properly typed params object
    public virtual EffectParams CreateDefaultParams() { return null; }

    public virtual void Configure(EffectParams p) { _params = p; }
    protected EffectParams _params;

    public abstract void Apply(ref HitContext ctx);
}




