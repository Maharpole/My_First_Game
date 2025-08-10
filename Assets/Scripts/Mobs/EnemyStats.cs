using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Base Stats")]
    public float moveSpeed = 3.5f;
    public float attackSpeed = 1f;
    public float damage = 10f;
    public int maxHealth = 50;

    [HideInInspector] public int currentHealth;

    [Header("Projectile")] 
    public int extraProjectiles = 0;

    void Awake()
    {
        currentHealth = maxHealth;
    }
}
