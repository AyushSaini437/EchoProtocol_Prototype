using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Idle, InvestigateSound, InvestigateAlarm, Retreat, Healing, Attack, SearchRandomly }
    public EnemyState currentState = EnemyState.Idle;

    [Header("Health System")]
    public int maxHealth = 100;
    private int currentHealth;
    public int retreatThreshold = 30;

    [Header("Combat & Audio")]
    public float attackRange = 15f;
    public float fireRate = 1.5f;
    public int weaponDamage = 15;
    private float nextFireTime = 0f;
    
    // Stealth Kill / Damage Alarm Variables
    private float lastTimeShot = 0f;
    private bool hasSoundedDamageAlarm = true; // True by default so they don't alarm when spawning
    public float damageAlarmDelay = 2.0f; // Seconds to wait after player STOPS shooting before screaming

    public float loseSightDuration = 6f; 
    private float timeSinceLastSawPlayer = 0f;
    private float searchWaitTimer = 0f;
    
    public AudioSource enemyAudioSource;
    public AudioClip enemyGunshot;

    [Header("Impact Effects")]
    public GameObject bulletHolePrefab;

    [Header("Muzzle Flash")]
    public MuzzleFlash muzzleFlash;

    [Header("Navigation & Senses")]
    public NavMeshAgent agent;
    public Transform player; 
    public LayerMask obstacleMask; 
    [Tooltip("Field of view cone angle in degrees (e.g. 110 means 55 degrees to each side)")]
    public float fieldOfViewAngle = 110f;
    
    private EnemyAI activeAlarmBeacon; 
    private bool isHealing = false;
    private Coroutine healingCoroutine;
    private Vector3 lastKnownPlayerPosition;

    private float fleeStartTime = 0f;
    private bool hasSoundedFleeAlarm = false;
    public float fleeAlarmDelay = 2.5f; 

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();

        // Fallback in case player was not manually linked in the Inspector
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        if (player != null)
        {
            lastKnownPlayerPosition = player.position;
        }

        // Auto-find MuzzleFlash in children if not assigned
        if (muzzleFlash == null)
        {
            muzzleFlash = GetComponentInChildren<MuzzleFlash>(true);
        }
    }

    void OnDisable()
    {
        StopHealingCoroutine();
    }

    void Update()
    {
        if (player == null) return;

        switch (currentState)
        {
            case EnemyState.Idle:
                CheckForPlayerSight();
                break;
            case EnemyState.InvestigateSound:
                CheckForPlayerSight();
                break;
            case EnemyState.InvestigateAlarm:
                TrackAlarmBeacon();
                CheckForPlayerSight();
                break;
            case EnemyState.SearchRandomly:
                SearchArea();
                CheckForPlayerSight();
                break;
            case EnemyState.Retreat:
                EvadePlayer();
                break;
            case EnemyState.Healing:
                if (!isHealing)
                {
                    healingCoroutine = StartCoroutine(HealOverTime());
                }
                break;
            case EnemyState.Attack:
                EngagePlayer();
                HandleDamageAlarm(); // Checks if the player failed to finish them off
                break;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        // Reset the timer every single time a bullet hits them
        lastTimeShot = Time.time;
        hasSoundedDamageAlarm = false; 

        if (player != null)
        {
            lastKnownPlayerPosition = player.position;
        }
        
        if (currentHealth <= 0)
        {
            StopHealingCoroutine();
            bool wasStealth = !hasSoundedDamageAlarm && !hasSoundedFleeAlarm && (currentState != EnemyState.Attack);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnHostileKilled(wasStealth);
            }
            Destroy(gameObject); // Enemy dies BEFORE the timer can finish, no alarm sounded!
        }
        else if (currentHealth <= retreatThreshold && currentState != EnemyState.Retreat)
        {
            StopHealingCoroutine();
            currentState = EnemyState.Retreat;
            agent.isStopped = false;
            
            fleeStartTime = Time.time;
            hasSoundedFleeAlarm = false; 
        }
        else if (currentState == EnemyState.Idle || currentState == EnemyState.InvestigateSound || 
                 currentState == EnemyState.InvestigateAlarm || currentState == EnemyState.SearchRandomly || 
                 currentState == EnemyState.Healing)
        {
            StopHealingCoroutine();
            currentState = EnemyState.Attack;
        }
    }

    private void StopHealingCoroutine()
    {
        if (healingCoroutine != null)
        {
            StopCoroutine(healingCoroutine);
            healingCoroutine = null;
        }
        isHealing = false;
    }

    private void HandleDamageAlarm()
    {
        // If they were shot, haven't alarmed yet, and the player stopped shooting long enough:
        if (!hasSoundedDamageAlarm && Time.time >= lastTimeShot + damageAlarmDelay)
        {
            TriggerGlobalAlarm();
            hasSoundedDamageAlarm = true;
            Debug.Log("Enemy survived the burst and called for backup!");
        }
    }

    private void TriggerGlobalAlarm()
    {
        EnemyAI[] allEnemies = FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude);
        foreach (EnemyAI ally in allEnemies)
        {
            if (ally != this) ally.ReceiveAlarm(this);
        }
    }

    public void ReceiveAlarm(EnemyAI sender)
    {
        if (currentState == EnemyState.Idle || currentState == EnemyState.InvestigateSound || currentState == EnemyState.SearchRandomly)
        {
            activeAlarmBeacon = sender;
            currentState = EnemyState.InvestigateAlarm;
            agent.isStopped = false;
        }
    }

    private void TrackAlarmBeacon()
    {
        if (activeAlarmBeacon != null)
        {
            agent.SetDestination(activeAlarmBeacon.transform.position);

            // Once allies reach the beacon ally, start searching rather than hovering indefinitely
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 1.5f)
            {
                activeAlarmBeacon = null;
                currentState = EnemyState.SearchRandomly;
                PickRandomSearchPoint();
            }
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                currentState = EnemyState.SearchRandomly; // Search the area where the beacon died
                PickRandomSearchPoint();
            }
        }
    }

    public void InvestigateSound(Vector3 soundPos)
    {
        if (currentState == EnemyState.Retreat || currentState == EnemyState.Healing || 
            currentState == EnemyState.Attack || currentState == EnemyState.InvestigateAlarm) return;
        
        currentState = EnemyState.InvestigateSound;
        agent.isStopped = false;
        agent.SetDestination(soundPos);
    }

    private void SearchArea()
    {
        // Added a 0.5f buffer so they don't get permanently stuck trying to reach the exact millimeter
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            // Make them pause and look around for 1.5 seconds before walking to the next spot
            searchWaitTimer += Time.deltaTime;
            if (searchWaitTimer >= 1.5f)
            {
                PickRandomSearchPoint();
                searchWaitTimer = 0f; // Reset the timer for the next stop
            }
        }
    }

    private void PickRandomSearchPoint()
    {
        // Use insideUnitCircle instead of Sphere so we only calculate flat X and Z coordinates
        Vector2 randomPoint2D = Random.insideUnitCircle * 15f; 
        Vector3 randomPoint3D = new Vector3(transform.position.x + randomPoint2D.x, transform.position.y, transform.position.z + randomPoint2D.y);
        
        NavMeshHit hit;
        // Map that flat coordinate to the nearest valid blue NavMesh floor
        if (NavMesh.SamplePosition(randomPoint3D, out hit, 10f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            agent.isStopped = false;
        }
    }

    private void EvadePlayer()
    {
        Vector3 fleeDirection = (transform.position - player.position).normalized;
        Vector3 fleeTarget = transform.position + fleeDirection * 10f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(fleeTarget, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        if (!hasSoundedFleeAlarm && Time.time >= fleeStartTime + fleeAlarmDelay)
        {
            TriggerGlobalAlarm();
            hasSoundedFleeAlarm = true; 
        }

        // Only stop and heal once out of line of sight AND having completed / nearly reached flee path
        if (!HasLineOfSightToPlayer() && (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 1.0f))
        {
            agent.isStopped = true; 
            currentState = EnemyState.Healing;
        }
    }

    private IEnumerator HealOverTime()
    {
        isHealing = true;
        float healTimer = 0f;
        
        while (currentHealth < maxHealth)
        {
            // Continually check if the player re-establishes line of sight
            if (HasLineOfSightToPlayer())
            {
                currentState = EnemyState.Retreat;
                agent.isStopped = false;
                isHealing = false;
                fleeStartTime = Time.time;
                hasSoundedFleeAlarm = false;
                healingCoroutine = null;
                yield break; 
            }

            healTimer += Time.deltaTime;
            if (healTimer >= 1f)
            {
                currentHealth = Mathf.Min(currentHealth + 20, maxHealth);
                healTimer = 0f;
            }

            yield return null; 
        }
        
        currentHealth = maxHealth;
        isHealing = false;
        currentState = EnemyState.Attack; 
        agent.isStopped = false;
        TriggerGlobalAlarm(); 
        healingCoroutine = null;
    }

    private void EngagePlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (HasLineOfSightToPlayer())
        {
            timeSinceLastSawPlayer = 0f;
            lastKnownPlayerPosition = player.position;

            if (distanceToPlayer > attackRange)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            else
            {
                agent.isStopped = true;
                transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

                if (Time.time >= nextFireTime)
                {
                    ShootAtPlayer();
                }
            }
        }
        else
        {
            // Move towards last known location instead of real-time walltracking
            agent.isStopped = false;
            agent.SetDestination(lastKnownPlayerPosition);
            timeSinceLastSawPlayer += Time.deltaTime;

            if (timeSinceLastSawPlayer >= loseSightDuration)
            {
                Debug.Log("Lost the player! Initiating random search.");
                currentState = EnemyState.SearchRandomly;
                timeSinceLastSawPlayer = 0f;
                PickRandomSearchPoint();
            }
        }
    }

    private void CheckForPlayerSight()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange + 5f)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            // Spot if within field of view cone, or within close-range proximity (2.5m)
            if ((angle <= fieldOfViewAngle * 0.5f || distanceToPlayer <= 2.5f) && HasLineOfSightToPlayer())
            {
                lastKnownPlayerPosition = player.position;
                currentState = EnemyState.Attack;
                hasSoundedDamageAlarm = true; // Prevent the damage timer from double-firing
                TriggerGlobalAlarm(); 
                Debug.Log("Enemy spotted you and immediately alerted the others!");
            }
        }
    }

    private bool HasLineOfSightToPlayer()
    {
        if (player == null) return false;

        Vector3 rayStart = transform.position + Vector3.up; 
        Vector3 targetPos = player.position + Vector3.up;
        Vector3 directionToPlayer = targetPos - rayStart;
        float distanceToPlayer = directionToPlayer.magnitude;

        // Raycast only up to the distance to player so obstacles behind the player don't block vision
        if (Physics.Raycast(rayStart, directionToPlayer.normalized, out RaycastHit hit, distanceToPlayer, obstacleMask))
        {
            return false; 
        }
        return true; 
    }

    private void ShootAtPlayer()
    {
        nextFireTime = Time.time + fireRate; 
        
        if (enemyAudioSource != null && enemyGunshot != null)
        {
            enemyAudioSource.PlayOneShot(enemyGunshot);
        }

        // Trigger muzzle flash at the tip of the enemy's firearm
        if (muzzleFlash != null)
        {
            muzzleFlash.TriggerFlash();
        }

        if (player == null) return;

        Vector3 rayStart = transform.position + Vector3.up; 
        Vector3 targetPos = player.position + Vector3.up;
        Vector3 directionToPlayer = targetPos - rayStart;

        // Perform raycast extending slightly past the player to hit walls/cover if player is in cover or dodges
        float maxCastDist = attackRange + 10f;
        if (Physics.Raycast(rayStart, directionToPlayer.normalized, out RaycastHit hit, maxCastDist, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<PlayerHealth>() != null)
            {
                PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
                if (pHealth != null)
                {
                    pHealth.TakeDamage(weaponDamage);
                }
            }
            else
            {
                // Hit cover or wall -> spawn bullet hole on the impacted surface
                SpawnBulletHole(hit);
            }
        }
    }

    private void SpawnBulletHole(RaycastHit hit)
    {
        if (bulletHolePrefab == null) return;

        // Offset slightly along normal to prevent z-fighting
        Vector3 spawnPos = hit.point + hit.normal * 0.002f;
        Quaternion spawnRot = Quaternion.LookRotation(-hit.normal);

        GameObject hole = Instantiate(bulletHolePrefab, spawnPos, spawnRot, hit.transform);
        hole.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));
    }
}