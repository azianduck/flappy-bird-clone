using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using DigitalRuby.RainMaker;
using UnityEngine;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    // Start is called before the first frame update
    public static PlayerManager instance;
    [Header("BIRD GRAVITY SETTINGS")]
    public bool applyGravity;
    public bool spawnAtStart;
    public GameObject birdPlayer;
    public float gravity = -9.81f; // gravity strength
    public float jumpVelocity = 5f;  // Upward velocity on jump
    public float verticalVelocity; // Current vertical velocity
    private float downwardTiltDelay = 0.2f; // Delay before tilting downwards (in seconds)
    private float tiltTimer = 0f; // Timer to track time since the bird started falling
    [Header("BACKGROUND MOVING")]
    public float bgSpeed;
    public Renderer[] bgRendererList;
    private float[] paralaxValues = { 0.01f, 0.04f, 0.05f, 0.06f, 0.07f, 0.1f };
    public GameObject newGameButton;
    public bool isPlaying;
    public GameObject bgMusic;
    public GameObject wingEffect;
    public GameObject hitEffect;
    public GameObject pointEffect;
    public BaseRainScript baseRainScript;

    private bool isBackgroundMusicOn = false;
    public bool isSoundEffectOn = false;

    private float screenWidth;
    private float screenHeight;
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        OnBGMusicChanged();
        UpdateScoreDisplay();
        screenWidth = Camera.main.aspect * Camera.main.orthographicSize * 2;
        screenHeight = screenWidth / Camera.main.aspect; // Calculate screenHeight
        bgMusic.SetActive(true);
        if (spawnAtStart) birdPlayer.transform.localPosition = new Vector3(-screenWidth * 40, screenHeight, 0);
    }

    private void FixedUpdate()
    {
        if (isPlaying) BackgroundMoving();
    }


    public float dropThreshHold;

    public bool dead;

    private void Update()
    {
        // Check for jump input
        if (!dead && isPlaying)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                wingEffect.SetActive(false);
                wingEffect.SetActive(true);
                verticalVelocity = jumpVelocity;
                Invoke("StartApplyingGravity", 0.1f);
                return;
            }
        }

        if (applyGravity)
        {
            SimulateGravity();
            CheckObstacleCollisions();
        }
    }

    void SimulateGravity()
    {
        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;

        if (verticalVelocity < 0)
        {
            tiltTimer += Time.deltaTime;
            if (tiltTimer >= downwardTiltDelay)
            {
                // Directly set the rotation to -90 when falling
                // Smoothly rotate towards -90 degrees when falling
                float targetAngle = -90f;
                float angleDifference = targetAngle - birdPlayer.transform.eulerAngles.z;

                // Ensure the shortest rotation direction
                if (angleDifference > 180f)
                {
                    angleDifference -= 360f;
                }
                else if (angleDifference < -180f)
                {
                    angleDifference += 360f;
                }

                float rotationAmount = angleDifference * Time.deltaTime * 5f;
                birdPlayer.transform.Rotate(0f, 0f, rotationAmount);
                birdPlayer.transform.parent.GetComponent<Animator>().SetTrigger("dropping");
            }
        }
        else
        {
            birdPlayer.transform.parent.GetComponent<Animator>().SetTrigger("flying");
            tiltTimer = 0f;
            // Restore the rotation to 0 when not falling
            birdPlayer.transform.rotation = Quaternion.Euler(0f, 0f, 35f);
        }

        dropThreshHold = -screenHeight;

        if (birdPlayer.transform.position.y < -screenHeight / 2)
        {

            GameOver();
            ResetGame();
        }
        // Move the bird vertically
        birdPlayer.transform.position += new Vector3(0f, verticalVelocity * Time.deltaTime, 0f);
    }

    void StartApplyingGravity()
    {
        // This function will be called after a short delay to start applying gravity
    }


    private float score = 0f;
    private HashSet<GameObject> scoredObstacles = new HashSet<GameObject>(); // Track scored obstacles


    void CheckObstacleCollisions()
    {
        RectTransform birdRectTransform = birdPlayer.GetComponent<RectTransform>();
        Vector3[] birdCorners = new Vector3[4];
        birdRectTransform.GetWorldCorners(birdCorners);
        Rect birdRect = new Rect(birdCorners[0], birdCorners[2] - birdCorners[0]);

        RectTransform sensorRectTransform = birdPlayer.transform.GetChild(0).GetComponent<RectTransform>();
        Vector3[] sensorCorners = new Vector3[4];
        sensorRectTransform.GetWorldCorners(sensorCorners);
        Rect sensorRect = new Rect(sensorCorners[0], sensorCorners[2] - sensorCorners[0]);

        foreach (GameObject obstacle in ObstacleSpawner.instance.obstacles)
        {
            if (!obstacle.activeSelf) continue; // Skip inactive obstacles

            RectTransform topObstacleRectTransform = obstacle.transform.GetChild(1).GetComponent<RectTransform>();
            RectTransform bottomObstacleRectTransform = obstacle.transform.GetChild(0).GetComponent<RectTransform>();
            RectTransform middleGapRectTransform = obstacle.transform.GetChild(2).GetComponent<RectTransform>();

            Vector3[] topObstacleCorners = new Vector3[4];
            Vector3[] bottomObstacleCorners = new Vector3[4];
            Vector3[] middleGapCorners = new Vector3[4];
            topObstacleRectTransform.GetWorldCorners(topObstacleCorners);
            bottomObstacleRectTransform.GetWorldCorners(bottomObstacleCorners);
            middleGapRectTransform.GetWorldCorners(middleGapCorners);

            Rect topObstacleRect = new Rect(topObstacleCorners[0], topObstacleCorners[2] - topObstacleCorners[0]);
            Rect bottomObstacleRect = new Rect(bottomObstacleCorners[0], bottomObstacleCorners[2] - bottomObstacleCorners[0]);
            Rect middleObstacleRect = new Rect(middleGapCorners[0], middleGapCorners[2] - middleGapCorners[0]);

            if (birdRect.Overlaps(topObstacleRect) || birdRect.Overlaps(bottomObstacleRect))
            {
                // Collision detected!
                hitEffect.SetActive(false);
                hitEffect.SetActive(true);
                dead = true;
                flashing.SetActive(true);
                GameOver();
                return;
            }
            else if (sensorRect.Overlaps(middleObstacleRect) && !scoredObstacles.Contains(obstacle))
            {
                GetScore(obstacle);
            }
        }
    }

    public void GetScore(GameObject obstacle)
    {
        scoredObstacles.Add(obstacle);
        score++;
        pointEffect.SetActive(false);
        pointEffect.SetActive(true);
        UpdateScoreDisplay();
        // scoreText.text = score.ToString();
        // if (rect)
    }

    public GameObject flashing;

    public void GameOver()
    {
        isPlaying = false;
    }

    private void ResetGame()
    {
        newGameButton.SetActive(true);
        dead = false;
        applyGravity = false;
        flashing.SetActive(false);
        birdPlayer.SetActive(false);
        ObstacleSpawner.instance.StopSpawning();
        score = 0;
    }

    public void OnStartGameClick()
    {
        dead = false;
        isPlaying = true;
        applyGravity = true;
        verticalVelocity = jumpVelocity;
        UpdateScoreDisplay();
        SpawnTheBird();
        ObstacleSpawner.instance.StartSpawnObstacle();
    }

    public void OnBGMusicChanged()
    {
        if (isBackgroundMusicOn)
        {
            bgMusic.GetComponent<AudioSource>().mute = true;
            isBackgroundMusicOn = false;
        }
        else
        {
            bgMusic.GetComponent<AudioSource>().mute = false;
            isBackgroundMusicOn = true;
        }
    }

    public void OnSoundEffectChanged()
    {
        if (isSoundEffectOn)
        {
            hitEffect.GetComponent<AudioSource>().mute = true;
            wingEffect.GetComponent<AudioSource>().mute = true;
            pointEffect.GetComponent<AudioSource>().mute = true;
            baseRainScript.audioSourceRainCurrent.AudioSource.mute = true;
            isSoundEffectOn = false;
        }
        else
        {
            hitEffect.GetComponent<AudioSource>().mute = false;
            wingEffect.GetComponent<AudioSource>().mute = false;
            pointEffect.GetComponent<AudioSource>().mute = false;
            baseRainScript.audioSourceRainCurrent.AudioSource.mute = false;
            isSoundEffectOn = true;
        }
    }


    public void SpawnTheBird()
    {
        // Set the bird's initial position
        birdPlayer.SetActive(true);
        birdPlayer.transform.localPosition = new Vector3(-screenWidth * 40, screenHeight, 0);
    }

    public void ResetObstacleScore(GameObject obstacle)
    {
        scoredObstacles.Remove(obstacle);
    }

    void BackgroundMoving()
    {
        for (int i = 0; i < bgRendererList.Count(); i++)
        {
            bgRendererList[i].material.mainTextureOffset += new Vector2((paralaxValues[i] * Time.deltaTime), 0);
        }
    }

    public GameObject imageNumberPrefab; // Prefab for the ImageNumber object
    public Transform scoreHolder;        // Parent transform to hold the score digits
    public Sprite[] numberSprites;       // Array of number sprites (0-9)

    private List<GameObject> digitInstances = new List<GameObject>(); // Store instantiated digits

    void UpdateScoreDisplay()
    {
        // Clear previous score display
        foreach (GameObject digit in digitInstances)
        {
            Destroy(digit);
        }
        digitInstances.Clear();

        // Split the score into digits
        int[] digits = GetDigits((int)score);

        // Instantiate and position new digits
        for (int i = 0; i < digits.Length; i++)
        {
            GameObject digitInstance = Instantiate(imageNumberPrefab, scoreHolder);
            digitInstances.Add(digitInstance); // Add to the list for tracking

            Image digitImage = digitInstance.GetComponent<Image>();
            digitImage.sprite = numberSprites[digits[i]];
        }
    }

    int[] GetDigits(int num)
    {
        List<int> digits = new List<int>();
        while (num > 0)
        {
            digits.Insert(0, num % 10); // Add the last digit to the front of the list
            num /= 10; // Remove the last digit
        }
        return digits.ToArray();
    }
}



// private void OnDrawGizmos()
// {
//     // Draw Bird Bounds
//     if (birdPlayer != null)
//     {
//         RectTransform birdRectTransform = birdPlayer.GetComponent<RectTransform>();
//         if (birdRectTransform != null)
//         {
//             Gizmos.color = Color.red; // Color for the bird's bounds
//             Vector3[] birdCorners = new Vector3[4];
//             birdRectTransform.GetWorldCorners(birdCorners);

//             // Draw lines connecting bird's corners
//             for (int i = 0; i < 4; i++)
//             {
//                 Gizmos.DrawLine(birdCorners[i], birdCorners[(i + 1) % 4]);
//             }
//         }
//     }

//     // Draw Obstacle Bounds
//     if (ObstacleSpawner.instance != null && ObstacleSpawner.instance.obstacles != null)
//     {
//         Gizmos.color = Color.green; // Color for the obstacles' bounds

//         foreach (GameObject obstacle in ObstacleSpawner.instance.obstacles)
//         {
//             if (obstacle != null)
//             {
//                 // Get RectTransforms of top and bottom obstacles
//                 RectTransform topObstacleRectTransform = obstacle.transform.GetChild(1).GetComponent<RectTransform>();
//                 RectTransform bottomObstacleRectTransform = obstacle.transform.GetChild(0).GetComponent<RectTransform>();

//                 // Draw top obstacle bounds
//                 if (topObstacleRectTransform != null)
//                 {
//                     Vector3[] topObstacleCorners = new Vector3[4];
//                     topObstacleRectTransform.GetWorldCorners(topObstacleCorners);
//                     for (int i = 0; i < 4; i++)
//                     {
//                         Gizmos.DrawLine(topObstacleCorners[i], topObstacleCorners[(i + 1) % 4]);
//                     }
//                 }

//                 // Draw bottom obstacle bounds
//                 if (bottomObstacleRectTransform != null)
//                 {
//                     Vector3[] bottomObstacleCorners = new Vector3[4];
//                     bottomObstacleRectTransform.GetWorldCorners(bottomObstacleCorners);
//                     for (int i = 0; i < 4; i++)
//                     {
//                         Gizmos.DrawLine(bottomObstacleCorners[i], bottomObstacleCorners[(i + 1) % 4]);
//                     }
//                 }
//             }
//         }
//     }
// }
