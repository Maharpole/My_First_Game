using UnityEngine;

public struct HitContext
{
    public Transform instigator;
    public Transform weapon;
    public Vector3 origin;
    public Vector3 direction;
    public float traveled;
    public RaycastHit hit;
    public int damage;
    public BulletProfile profile;
    public WeaponFireProfile weaponProfile;
}




