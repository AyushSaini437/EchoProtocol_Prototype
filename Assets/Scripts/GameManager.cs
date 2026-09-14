using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Mission Hostiles")]
    public int targetEnemyCount = 15;
    public int totalHostiles = 15;
    public int remainingHostiles = 15;
    public int stealthKills = 0;

    [Header("Scoring & Timer")]
    public int currentScore = 0;
    public float missionTime = 0f;
    public bool isMissionActive = false;

    [Header("Spawn Settings")]
    public float minDistanceFromPlayer = 25f;
    public LayerMask obstacleMask;

    [Header("References")]
    public MenuManager menuManager;
    public Transform playerTransform;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (menuManager == null)
        {
            menuManager = Object.FindAnyObjectByType<MenuManager>();
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        // Set default obstacle mask if not assigned (Layers Obstacle & Default)
        if (obstacleMask.value == 0)
        {
            obstacleMask = LayerMask.GetMask("Obstacle", "Default");
        }

        InitializeMission();
    }

    public void InitializeMission()
    {
        // Gather existing enemies in the scene
        List<EnemyAI> enemiesList = new List<EnemyAI>(Object.FindObjectsByType<EnemyAI>());

        // If fewer than targetEnemyCount, dynamically clone template enemy to populate the compound
        if (enemiesList.Count > 0 && enemiesList.Count < targetEnemyCount)
        {
            GameObject template = enemiesList[0].gameObject;
            int toSpawn = targetEnemyCount - enemiesList.Count;
            for (int s = 0; s < toSpawn; s++)
            {
                GameObject clone = Instantiate(template, template.transform.position, template.transform.rotation);
                clone.name = $"Enemy_{enemiesList.Count + s + 1}";
                EnemyAI ai = clone.GetComponent<EnemyAI>();
                if (ai != null)
                {
                    enemiesList.Add(ai);
                }
            }
        }

        EnemyAI[] enemies = enemiesList.ToArray();
        totalHostiles = enemies.Length;
        remainingHostiles = totalHostiles;
        currentScore = 0;
        missionTime = 0f;
        stealthKills = 0;

        // Randomize enemy spawn positions across map with NO direct sight to player
        RandomizeEnemySpawns(enemies);
    }

    void RandomizeEnemySpawns(EnemyAI[] enemies)
    {
        if (playerTransform == null || enemies.Length == 0) return;

        Vector3 playerPos = playerTransform.position;

        // Candidate patrol areas distributed across the entire 200m compound
        List<Vector3> candidatePoints = new List<Vector3>
        {
            // Sector A: East Outpost & High Ground
            new Vector3(55.4f, 116.4f, -94.5f),
            new Vector3(55.4f, 116.4f, -85.3f),
            new Vector3(62.0f, 116.4f, -88.0f),
            new Vector3(58.0f, 116.4f, -102.0f),
            new Vector3(64.5f, 116.4f, -76.0f),
            new Vector3(72.0f, 116.4f, -82.0f),
            new Vector3(75.0f, 116.4f, -95.0f),

            // Sector B: Central Courtyard & Cover Blocks
            new Vector3(45.4f, 116.6f, -76.6f),
            new Vector3(40.1f, 117.8f, -97.6f),
            new Vector3(45.9f, 116.4f, -76.1f),
            new Vector3(48.2f, 116.4f, -90.0f),
            new Vector3(38.5f, 116.4f, -82.5f),
            new Vector3(42.0f, 116.4f, -88.0f),
            new Vector3(35.0f, 116.4f, -78.0f),

            // Sector C: South Warehouse & Alleys
            new Vector3(50.0f, 116.4f, -100.0f),
            new Vector3(35.0f, 116.4f, -104.0f),
            new Vector3(28.0f, 116.4f, -100.0f),
            new Vector3(20.0f, 116.4f, -95.0f),
            new Vector3(12.0f, 116.4f, -96.0f),
            new Vector3(25.0f, 116.4f, -108.0f),
            new Vector3(40.0f, 116.4f, -112.0f),

            // Sector D: North Corridors & Perimeter
            new Vector3(52.0f, 116.4f, -64.0f),
            new Vector3(42.0f, 116.4f, -68.0f),
            new Vector3(30.0f, 116.4f, -70.0f),
            new Vector3(22.0f, 116.4f, -66.0f),
            new Vector3(18.5f, 116.4f, -72.0f),
            new Vector3(35.0f, 116.4f, -62.0f),
            new Vector3(48.0f, 116.4f, -58.0f),

            // Sector E: Mid-West Interior & Logistics
            new Vector3(35.0f, 116.4f, -92.0f),
            new Vector3(25.0f, 116.4f, -85.0f),
            new Vector3(28.0f, 116.4f, -80.0f),
            new Vector3(15.0f, 116.4f, -78.0f),
            new Vector3(10.0f, 116.4f, -88.0f),
            new Vector3(5.0f, 116.4f, -82.0f),
            new Vector3(0.0f, 116.4f, -76.0f),
            new Vector3(-10.0f, 116.4f, -85.0f),
            new Vector3(-15.0f, 116.4f, -75.0f),
            new Vector3(-20.0f, 116.4f, -90.0f)
        };

        // Filter points: must be at least minDistanceFromPlayer AND physically occluded by an obstacle
        List<Vector3> validSpawnPoints = new List<Vector3>();

        foreach (Vector3 pt in candidatePoints)
        {
            float dist = Vector3.Distance(pt, playerPos);
            if (dist >= minDistanceFromPlayer)
            {
                // Check if an obstacle blocks direct line of sight from spawn point to player
                Vector3 origin = pt + Vector3.up * 1.5f;
                Vector3 target = playerPos + Vector3.up * 1.5f;
                Vector3 dir = target - origin;

                // STRICT: Line of sight must be blocked by an obstacle
                bool hasObstacle = Physics.Raycast(origin, dir.normalized, dir.magnitude, obstacleMask);
                if (hasObstacle)
                {
                    if (NavMesh.SamplePosition(pt, out NavMeshHit hit, 6f, NavMesh.AllAreas))
                    {
                        validSpawnPoints.Add(hit.position);
                    }
                }
            }
        }

        // Shuffle valid points first
        for (int i = 0; i < validSpawnPoints.Count; i++)
        {
            int rnd = Random.Range(i, validSpawnPoints.Count);
            Vector3 temp = validSpawnPoints[i];
            validSpawnPoints[i] = validSpawnPoints[rnd];
            validSpawnPoints[rnd] = temp;
        }

        // Fallback if not enough points met occlusion criteria
        if (validSpawnPoints.Count == 0)
        {
            validSpawnPoints.AddRange(candidatePoints);
        }

        // ENFORCE: Never place more than 2 enemies together in the same cluster (within 22m radius)
        float clusterRadius = 22f;
        int maxEnemiesPerCluster = 2;

        List<Vector3> chosenSpawnPoints = new List<Vector3>();

        foreach (Vector3 pt in validSpawnPoints)
        {
            int nearbyCount = 0;
            for (int c = 0; c < chosenSpawnPoints.Count; c++)
            {
                if (Vector3.Distance(pt, chosenSpawnPoints[c]) < clusterRadius)
                {
                    nearbyCount++;
                }
            }

            // Only allow lone sentries (0 nearby) or duo pairs (1 nearby)
            if (nearbyCount < maxEnemiesPerCluster)
            {
                chosenSpawnPoints.Add(pt);
                if (chosenSpawnPoints.Count >= enemies.Length) break;
            }
        }

        // Fallback: If clustering limit left fewer slots, fill remaining from valid points
        if (chosenSpawnPoints.Count < enemies.Length)
        {
            for (int k = 0; k < validSpawnPoints.Count && chosenSpawnPoints.Count < enemies.Length; k++)
            {
                if (!chosenSpawnPoints.Contains(validSpawnPoints[k]))
                {
                    chosenSpawnPoints.Add(validSpawnPoints[k]);
                }
            }
        }

        // Warp enemies to their selected positions
        for (int i = 0; i < enemies.Length; i++)
        {
            NavMeshAgent agent = enemies[i].GetComponent<NavMeshAgent>();
            Vector3 spawnPos = chosenSpawnPoints[i % chosenSpawnPoints.Count];
            if (agent != null)
            {
                agent.Warp(spawnPos);
            }
            enemies[i].transform.position = spawnPos;
            enemies[i].transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
    }

    void Update()
    {
        // Advance mission timer when playing
        if (isMissionActive && !MenuManager.isGamePaused)
        {
            missionTime += Time.deltaTime;
        }
    }

    public void StartMission()
    {
        isMissionActive = true;
    }

    public void OnHostileKilled(bool wasStealth)
    {
        remainingHostiles--;

        // Reward points
        int killPoints = 500;
        int stealthBonus = 0;

        if (wasStealth)
        {
            stealthKills++;
            stealthBonus = 250;
        }

        currentScore += killPoints + stealthBonus;

        // Check for victory
        if (remainingHostiles <= 0)
        {
            TriggerVictory();
        }
    }

    public void TriggerVictory()
    {
        isMissionActive = false;

        // Calculate final score bonuses
        int timeBonus = Mathf.Max(0, (int)((300f - missionTime) * 10f));
        PlayerHealth ph = FindAnyObjectByType<PlayerHealth>();
        int healthBonus = (ph != null) ? ph.CurrentHealth * 10 : 0;

        currentScore += timeBonus + healthBonus;

        // Determine Rank
        string rank;
        if (currentScore >= 5000) rank = "S-RANK (GHOST AGENT)";
        else if (currentScore >= 3500) rank = "A-RANK (OPERATIVE)";
        else if (currentScore >= 2000) rank = "B-RANK (ENFORCER)";
        else rank = "C-RANK (SURVIVOR)";

        string timeStr = FormatTime(missionTime);

        if (menuManager != null)
        {
            menuManager.ShowVictory(timeStr, totalHostiles, stealthKills, currentScore, rank);
        }
    }

    public void OnPlayerDied()
    {
        isMissionActive = false;
        string timeStr = FormatTime(missionTime);
        int kills = totalHostiles - remainingHostiles;

        if (menuManager != null)
        {
            menuManager.ShowDefeat(timeStr, kills, currentScore);
        }
    }

    public static string FormatTime(float timeInSeconds)
    {
        int minutes = (int)(timeInSeconds / 60f);
        int seconds = (int)(timeInSeconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
