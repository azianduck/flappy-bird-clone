using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public static ObstacleSpawner instance;
    public GameObject obstaclePrefab;       // Prefab for a single obstacle
    public Transform canvasTransform;     // Reference to the canvas object
    public float spawnIntervalMin = 1f;    // Minimum time between spawns
    public float spawnIntervalMax = 3f;    // Maximum time between spawns
    public float minHeight = 2f;           // Minimum obstacle height
    public float maxHeight = 6f;           // Maximum obstacle height
    public float obstacleSpeed = -5f;      // Speed at which obstacles move
    public int initialPoolSize = 10;        // Initial number of obstacles
    private float screenWidth;
    private float spawnX;
    public List<GameObject> obstacles = new List<GameObject>();
    private Coroutine obstacleActivationCoroutine; // Reference to the coroutine

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        screenWidth = Camera.main.aspect * Camera.main.orthographicSize * 2;
        spawnX = screenWidth * 200; // Spawn 1.5 times the screen width to the right

        // Pre-spawn obstacles and set them to "waiting" state
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject obstacle = Instantiate(obstaclePrefab, canvasTransform);
            obstacle.SetActive(false); // Initially inactive ("waiting")
            obstacles.Add(obstacle);
            RandomizeObstacle(obstacle, spawnX + i * 5); // Initial spacing to avoid clumping
        }

        // Start activating obstacles one by one with random delays

    }

    IEnumerator ActivateObstaclesWithDelay()
    {
        while (true)
        {
            foreach (GameObject obst in obstacles)
            {
                if (!obst.activeSelf)
                {
                    float delay = Random.Range(spawnIntervalMin, spawnIntervalMax);
                    yield return new WaitForSeconds(delay);

                    RandomizeObstacle(obst, spawnX); // Set new position and height
                    obst.SetActive(true);
                }
            }
        }
    }

    public void StartSpawnObstacle()
    {
        obstacleActivationCoroutine = StartCoroutine(ActivateObstaclesWithDelay());
    }

    void Update()
    {
        if (PlayerManager.instance.isPlaying)
        {
            MoveAndRecycleObstacles();
        }
    }

    void MoveAndRecycleObstacles()
    {
        foreach (GameObject obst in obstacles)
        {
            if (obst.activeSelf)
            {
                obst.transform.Translate(obstacleSpeed * Time.deltaTime, 0, 0);

                if (obst.transform.position.x < -screenWidth)
                {
                    ReturnObstacleToPool(obst);
                }
            }
        }
    }

    void RandomizeObstacle(GameObject obst, float xPos)
    {
        float prefabHeight = Random.Range(minHeight, maxHeight);
        obst.transform.localPosition = new Vector3(xPos, prefabHeight, 0);
    }

    void ReturnObstacleToPool(GameObject obst)
    {
        obst.SetActive(false);
        PlayerManager.instance.ResetObstacleScore(obst);
    }

    public void StopSpawning()
    {
        if (obstacleActivationCoroutine != null)
        {
            StopCoroutine(obstacleActivationCoroutine);
            obstacleActivationCoroutine = null; // Clear the reference
        }
        // Reset all obstacles to waiting state
        foreach (GameObject obst in obstacles)
        {
            ReturnObstacleToPool(obst);
            RandomizeObstacle(obst, spawnX + obstacles.IndexOf(obst) * 5); // Reposition for next spawn
        }
    }
}
