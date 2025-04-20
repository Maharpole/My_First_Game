using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f; // Speed of the player movement
    public float dashSpeed = 20f; // Speed of the dash
    public float dashDuration = 0.2f; // Duration of the dash in seconds
    public int maxDashCharges = 3; // Maximum number of dash charges
    public float dashRechargeTime = 5f; // Time in seconds to recharge one dash charge
    
    [Header("Sound Effects")]
    [Tooltip("List of possible dash sounds to randomly select from")]
    public AudioClip[] dashSounds;
    
    [Tooltip("Volume of the dash sound (0-1)")]
    [Range(0f, 1f)]
    public float dashVolume = 1f;
    
    [Header("Visual Effects")]
    [Tooltip("Particle system to play during dash")]
    public ParticleSystem dashParticles;
    
    [Tooltip("How long the particles should play after dash ends")]
    public float particleDuration = 0.5f;
    
    private bool isDashing = false; // Tracks if the player is currently dashing
    private float dashTime = 0f; // Timer for the dash
    private int currentDashCharges; // Current number of available dash charges
    private float rechargeTimer = 0f; // Timer for recharging dash charges
    private AudioSource audioSource;
    private ParticleSystem.EmissionModule particleEmission;
    
    private void Start()
    {
        // Initialize dash charges
        currentDashCharges = maxDashCharges;
        
        // Set up audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Set up particle system
        if (dashParticles != null)
        {
            particleEmission = dashParticles.emission;
            particleEmission.enabled = false;
        }
        else
        {
            Debug.LogWarning("No particle system assigned for dash effect!");
        }
    }

    private void Update()
    {
        // Get input from WASD or arrow keys
        float moveX = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrow
        float moveZ = Input.GetAxis("Vertical");   // W/S or Up/Down Arrow

        // Calculate movement vector
        Vector3 movement = new Vector3(moveX, 0, moveZ).normalized;

        // Check if the player is dashing
        if (isDashing)
        {
            // Apply dash movement
            transform.Translate(movement * dashSpeed * Time.deltaTime, Space.World);

            // Update dash timer
            dashTime -= Time.deltaTime;
            if (dashTime <= 0)
            {
                isDashing = false; // End the dash
                
                // Stop particles after a delay
                if (dashParticles != null)
                {
                    StartCoroutine(StopParticlesAfterDelay());
                }
            }
        }
        else
        {
            // Apply normal movement
            transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);

            // Check for dash input
            if (Input.GetKeyDown(KeyCode.Space) && movement != Vector3.zero && currentDashCharges > 0)
            {
                isDashing = true;
                dashTime = dashDuration;
                currentDashCharges--; // Consume one dash charge
                
                // Play random dash sound
                if (dashSounds != null && dashSounds.Length > 0 && audioSource != null)
                {
                    int randomIndex = Random.Range(0, dashSounds.Length);
                    audioSource.PlayOneShot(dashSounds[randomIndex], dashVolume);
                }
                
                // Start particles
                if (dashParticles != null)
                {
                    particleEmission.enabled = true;
                    dashParticles.Play();
                }
            }
        }

        // Recharge dash charges over time
        if (currentDashCharges < maxDashCharges)
        {
            rechargeTimer += Time.deltaTime;
            if (rechargeTimer >= dashRechargeTime)
            {
                currentDashCharges++; // Add one dash charge
                rechargeTimer = 0f; // Reset the recharge timer
            }
        }
    }
    
    IEnumerator StopParticlesAfterDelay()
    {
        yield return new WaitForSeconds(particleDuration);
        if (dashParticles != null)
        {
            particleEmission.enabled = false;
        }
    }
}