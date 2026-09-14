using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Gun Settings")]
    public float range = 150f;
    public int weaponDamage = 40;
    public float fireRate = 0.25f; // Minimum time between shots in seconds
    public Transform playerCamera;

    [Header("Audio & Visuals")]
    public GameObject ripplePrefab;
    public float gunShotNoiseRadius = 25f;
    public AudioSource gunAudioSource;
    public AudioClip gunshotSound;

    [Header("Impact Effects")]
    public GameObject bulletHolePrefab;

    [Header("Muzzle Flash")]
    public MuzzleFlash muzzleFlash;

    [Header("Layer Masks")]
    [Tooltip("Layers the bullet can hit. Defaults to Everything.")]
    public LayerMask hitLayers = ~0;

    private float nextFireTime = 0f;

    void Start()
    {
        // Fallback in case playerCamera was not assigned in the Inspector
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        // Auto-find MuzzleFlash in children if not assigned
        if (muzzleFlash == null)
        {
            muzzleFlash = GetComponentInChildren<MuzzleFlash>(true);
        }
    }

    void Update()
    {
        if (MenuManager.isGamePaused) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time >= nextFireTime)
            {
                Shoot();
            }
        }
    }

    private void Shoot()
    {
        nextFireTime = Time.time + fireRate;

        // Play gunshot audio
        if (gunAudioSource != null && gunshotSound != null)
        {
            gunAudioSource.PlayOneShot(gunshotSound);
        }

        // Trigger muzzle flash at the tip of the gun
        if (muzzleFlash != null)
        {
            muzzleFlash.TriggerFlash();
        }

        // Spawn sound ripple dynamically snapped to actual ground surface below player
        SpawnSoundRipple();

        // Alert nearby enemies with deduplication
        AlertNearbyEnemies();

        // Hitscan Raycast
        PerformRaycast();
    }

    private void SpawnSoundRipple()
    {
        if (ripplePrefab == null) return;

        Vector3 spawnPos = transform.position;

        // Raycast down to find the actual floor/terrain surface height
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 5f, hitLayers, QueryTriggerInteraction.Ignore))
        {
            spawnPos = groundHit.point + Vector3.up * 0.01f; // Slight offset to prevent z-fighting
        }

        GameObject ripple = Instantiate(ripplePrefab, spawnPos, Quaternion.identity);
        SoundRipple rippleScript = ripple.GetComponent<SoundRipple>();
        if (rippleScript != null)
        {
            rippleScript.maxRadius = gunShotNoiseRadius;
        }
    }

    private void AlertNearbyEnemies()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, gunShotNoiseRadius, hitLayers, QueryTriggerInteraction.Ignore);
        HashSet<EnemyAI> alertedEnemies = new HashSet<EnemyAI>();

        foreach (var hitCollider in hitColliders)
        {
            EnemyAI enemy = hitCollider.GetComponentInParent<EnemyAI>();
            if (enemy != null && !alertedEnemies.Contains(enemy))
            {
                alertedEnemies.Add(enemy);
                enemy.InvestigateSound(transform.position);
            }
        }
    }

    private void PerformRaycast()
    {
        if (playerCamera == null) return;

        RaycastHit hit;
        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, range, hitLayers, QueryTriggerInteraction.Ignore))
        {
            EnemyAI enemy = hit.collider.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(weaponDamage);
                Debug.Log($"Hit enemy for {weaponDamage} damage! Target: {enemy.name}");
            }
            else
            {
                // Hit surface / environment -> spawn bullet hole
                SpawnBulletHole(hit);
            }
        }
    }

    private void SpawnBulletHole(RaycastHit hit)
    {
        if (bulletHolePrefab == null) return;

        // Offset slightly along normal to prevent z-fighting with the surface
        Vector3 spawnPos = hit.point + hit.normal * 0.002f;
        // Align quad face with surface normal (Quad front is -Z)
        Quaternion spawnRot = Quaternion.LookRotation(-hit.normal);

        GameObject hole = Instantiate(bulletHolePrefab, spawnPos, spawnRot, hit.transform);
        // Randomize rotation around normal for visual variation
        hole.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));
    }
}