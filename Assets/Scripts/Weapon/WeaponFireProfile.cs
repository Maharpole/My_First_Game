using UnityEngine;
using System.Collections.Generic;

// Attach this to each WEAPON prefab to define how it fires.
// When the weapon is equipped, WeaponEquipper copies these settings
// to the player's ClickShooter so each gun behaves uniquely even if
// ClickShooter lives on the player.
public class WeaponFireProfile : MonoBehaviour
{
    [Header("Firing")] public float fireRate = 6f;
    [Min(1)] public int pellets = 1;
    [Tooltip("Additional spread applied per pellet (deg). For precise guns, set near zero.")]
    public float extraPelletSpread = 0f;

    [Header("Bullet")] public BulletProfile bullet;

    [Header("Effects (ordered)")] public List<BulletEffect> effects = new List<BulletEffect>();

    [Header("Audio")] public AudioClip[] fireClips;
    [Range(0f,1f)] public float fireVolume = 1f;
    public Vector2 firePitchRange = new Vector2(1f, 1f);

	public void ApplyTo(ClickShooter shooter)
	{
		if (shooter == null) return;
		shooter.fireRate = fireRate;
		shooter.profile = this;
		shooter.fireClips = fireClips;
		shooter.fireVolume = fireVolume;
		shooter.firePitchRange = firePitchRange;
	}
}



