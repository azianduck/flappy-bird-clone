using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using DigitalRuby.RainMaker;
using UnityEngine;
using UnityEngine.UI;
using System;

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
    // public bool effectedByGravity;
    public bool autoPlay = false;
    public bool autoPlayDebug = false;
    public float minPace;
    public float maxPace;
    public float normalPace;
    public bool dead;
    private float score = 0f;
    private HashSet<GameObject> scoredObstacles = new HashSet<GameObject>(); // Track scored obstacles

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        // effectedByGravity = true;
        // slider1.value = autoJumpPace;
        OnAutoPlayChanged();
        OnBGMusicChanged();
        UpdateScoreDisplay();
        screenWidth = Camera.main.aspect * Camera.main.orthographicSize * 2;
        screenHeight = screenWidth / Camera.main.aspect; // Calculate screenHeight
        bgMusic.SetActive(true);
        if (spawnAtStart) birdPlayer.transform.localPosition = new Vector3(-screenWidth * 40, screenHeight, 0);
        // GetFirstLowValue();
        // if (!jumped)
        // {
        //     jumped = true;
        //     Invoke("AutoJumpRoutine", 3f);
        // }
    }

    private void FixedUpdate()
    {
        deltaTime = Time.fixedDeltaTime;
        gravityDelta = gravity * Time.fixedDeltaTime;
        if (isPlaying) BackgroundMoving();
        if (applyGravity)
        {
            SimulateGravity();
            CheckObstacleCollisions();
        }

        if (autoPlayDebug)
        {
            if (!jumped)
            {
                jumped = true;
                Invoke("AutoJumpRoutine", 0);
            }
        }


        if (autoPlay)
        {
            if (nextObstacle != null) AutoChangePace();
            if (heightDifferent == 0)
            {

            }
            if (!dead && isPlaying)
            {
                if (!jumped)
                {
                    jumped = true;
                    Invoke("AutoJumpRoutine", 0f);
                }

            }
        }
    }
    public bool jumped;

    private void Update()
    {
        if (!autoPlay)
        {
            // if (nextObstacle != null)
            // {
            //     AutoChangePace();
            // }
            if (!dead && isPlaying)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    BirdJump();
                }
            }
        }
    }

    public bool getFirstLowValue = false;
    public bool getFirstBigValue = false;

    public void AutoJumpRoutine()
    {
        applyGravity = true;
        StartCoroutine(BirdAutoJump());
    }

    IEnumerator BirdAutoJump()
    {
        wingEffect.SetActive(false);
        wingEffect.SetActive(true);
        verticalVelocity = jumpVelocity;
        biggestNum = birdPlayer.transform.localPosition.y;
        yield return new WaitForSeconds(autoJumpPace);
        jumped = false;
    }

    public void GetFirstLowValue()
    {
        if (!getFirstLowValue)
        {
            lowestNum = birdPlayer.transform.localPosition.y;
            getFirstLowValue = true;
        }
    }
    // public void GetFirstBigValue()
    // {
    //     if (!getFirstBigValue)
    //     {
    //         biggestNum = birdPlayer.transform.localPosition.y;
    //         getFirstBigValue = true;
    //     }
    // }

    public void BirdJump()
    {
        wingEffect.SetActive(false);
        wingEffect.SetActive(true);
        verticalVelocity = jumpVelocity;
    }

    public float deltaTime;
    public float gravityDelta;
    void SimulateGravity()
    {
        // Apply gravity
        verticalVelocity += gravity * Time.fixedDeltaTime;

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

        if (birdPlayer.transform.position.y < -screenHeight / 2)
        {

            GameOver();
            ResetGame();
        }
        // Move the bird vertically
        birdPlayer.transform.position += new Vector3(0f, verticalVelocity * Time.fixedDeltaTime, 0f);
    }

    public float biggestNum;
    public float num;
    public float lowestNum;

    public void GetBiggestNumber(float num)
    {
        if (num > biggestNum)
        {
            biggestNum = num;
        }
    }
    public void GetLowestNum(float num)
    {
        if (num < lowestNum)
        {
            lowestNum = num;
        }
    }

    // void StartApplyingGravity()
    // {
    //     // This function will be called after a short delay to start applying gravity
    //     effectedByGravity = true;
    // }




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
            RectTransform passGapRectTransform = obstacle.transform.Find("Pass").GetComponent<RectTransform>();
            RectTransform middleObstacleRectTransform = obstacle.transform.Find("Middle").GetComponent<RectTransform>();

            Vector3[] topObstacleCorners = new Vector3[4];
            Vector3[] bottomObstacleCorners = new Vector3[4];
            Vector3[] passGapCorners = new Vector3[4];
            Vector3[] middleCorners = new Vector3[4];

            topObstacleRectTransform.GetWorldCorners(topObstacleCorners);
            bottomObstacleRectTransform.GetWorldCorners(bottomObstacleCorners);
            passGapRectTransform.GetWorldCorners(passGapCorners);
            middleObstacleRectTransform.GetWorldCorners(middleCorners);

            Rect topObstacleRect = new Rect(topObstacleCorners[0], topObstacleCorners[2] - topObstacleCorners[0]);
            Rect bottomObstacleRect = new Rect(bottomObstacleCorners[0], bottomObstacleCorners[2] - bottomObstacleCorners[0]);
            Rect passObstacleRect = new Rect(passGapCorners[0], passGapCorners[2] - passGapCorners[0]);
            Rect middleObstacleRect = new Rect(middleCorners[0], middleCorners[2] - middleCorners[0]);

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
            else if (sensorRect.Overlaps(middleObstacleRect))
            {
                nextObstacles.Remove(obstacle);
                nextObstacle = nextObstacles[0];
            }
            else if (sensorRect.Overlaps(passObstacleRect) && !scoredObstacles.Contains(obstacle))
            {
                GetScore(obstacle);
            }
        }
    }

    public List<GameObject> nextObstacles;
    public GameObject nextObstacle;

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
    public Slider slider1;
    public Slider slider2;

    public void GameOver()
    {
        isPlaying = false;
    }

    private void ResetGame()
    {
        newGameButton.SetActive(true);
        nextObstacles.Clear();
        nextObstacle = null;
        dead = false;
        distance = 0;
        applyGravity = false;
        autoJumpPace = 0.452f;
        flashing.SetActive(false);
        birdPlayer.SetActive(false);
        ObstacleSpawner.instance.StopSpawning();
        score = 0;
        if (autoPlay) StartCoroutine(OnDieAutoPlay());
    }

    IEnumerator OnDieAutoPlay()
    {
        yield return new WaitForSeconds(1f);
        Invoke("OnStartGameClick", 0f);
        newGameButton.SetActive(false);
    }

    public GameObject takeControlInvite;
    public Toggle autoPlayToggle;

    public void OnStartGameClick()
    {
        dead = false;
        isPlaying = true;
        applyGravity = true;
        verticalVelocity = jumpVelocity;
        UpdateScoreDisplay();
        SpawnTheBird();
        ObstacleSpawner.instance.StartSpawnObstacle();
        if (autoPlay)
        {
            takeControlInvite.SetActive(true);
        }
        else
        {
            takeControlInvite.SetActive(false);
        }
    }

    public void OnUserTakeControl()
    {
        if (autoPlay)
        {
            autoPlayToggle.isOn = false;
            autoPlay = false;
            takeControlInvite.SetActive(false);
        }
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

    public void OnAutoPlayChanged()
    {
        if (autoPlay)
        {
            autoPlay = false;
            if (isPlaying) takeControlInvite.SetActive(false);
        }
        else
        {
            autoPlay = true;
            if (isPlaying) takeControlInvite.SetActive(true);
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

    public float adjustmentPaceUp;
    public float adjustmentPaceDown;
    public float autoJumpPace;
    public float distance;
    public float heightDifferent;

    public void AutoChangePace()
    {
        float nextHeight;
        float offsetHeight = nextObstacle.transform.Find("offset1").transform.position.y;
        float offsetHeight2 = nextObstacle.transform.Find("offset2").transform.position.y;

        distance = nextObstacle.transform.Find("Middle").transform.position.x - birdPlayer.transform.position.x;
        nextHeight = nextObstacle.transform.position.y;


        Debug.LogWarning("bird : " + birdPlayer.transform.position.y);
        Debug.LogWarning("nextOBST : " + nextObstacle.transform.position.y);

        heightDifferent = nextHeight - birdPlayer.transform.position.y;


        if (distance > 0.7f)
        {
            if (heightDifferent > 1.5f && distance > 1.5f)
            {
                autoJumpPace -= adjustmentPaceUp * heightDifferent * 4f;
                Debug.Log("<color=yellow>WAIT FOR SHARP UP -- : " + autoJumpPace + "</color>");
            }
            else if (heightDifferent > 1.5f && distance < 1.5f)
            {
                autoJumpPace -= adjustmentPaceUp * heightDifferent * 6f;
                Debug.Log("<color=yellow> SHARP UP -- : " + autoJumpPace + "</color>");

            }
            else if (heightDifferent < -1.5f && distance > 1.5f)
            {
                autoJumpPace += adjustmentPaceDown * (Math.Abs(heightDifferent) * 3f);
                Debug.Log("<color=yellow>WAIT FOR SHARP DOWN -- : " + autoJumpPace + "</color>");
                if (birdPlayer.transform.position.y <= offsetHeight)
                {
                    autoJumpPace = 0.13f;
                    Debug.Log("<color=white> GO STRAIGHT : " + autoJumpPace + "</color>");
                }
            }
            else if (heightDifferent < -1.5f && distance < 1.5f)
            {
                autoJumpPace = 1000f;
                // autoJumpPace = adjustmentPaceDown * (Math.Abs(heightDifferent) * 1000);
                Debug.Log("<color=yellow>SHARP DOWN -- : " + autoJumpPace + "</color>");
                if (birdPlayer.transform.position.y <= offsetHeight)
                {
                    autoJumpPace = 0.13f;
                    Debug.Log("<color=white> GO STRAIGHT : " + autoJumpPace + "</color>");
                }
            }
            else if (heightDifferent > 0.5f && heightDifferent < 1.5f && distance > 1.5f)
            {
                autoJumpPace -= adjustmentPaceUp * heightDifferent * 4f;
                Debug.Log("<color=green> Pace -- : " + autoJumpPace + "</color>");
            }
            else if (heightDifferent > 0.5f && heightDifferent < 1.5f && distance < 1.5f)
            {
                autoJumpPace -= adjustmentPaceUp * heightDifferent * 8f;
                Debug.Log("<color=green> Pace -- : " + autoJumpPace + "</color>");
            }
            else if (heightDifferent < -0.5f && heightDifferent > -1.5f && distance > 1.5f)
            {
                autoJumpPace += adjustmentPaceDown * (Math.Abs(heightDifferent) * 4f);
                Debug.Log("<color=red> Pace ++ : " + autoJumpPace + "</color>");
                if (birdPlayer.transform.position.y <= offsetHeight2)
                {
                    autoJumpPace = 0.13f;
                    Debug.Log("<color=white> GO STRAIGHT : " + autoJumpPace + "</color>");
                }
            }
            else if (heightDifferent < -0.5f && heightDifferent > -1.5f && distance < 1.5f)
            {
                autoJumpPace += adjustmentPaceDown * (Math.Abs(heightDifferent) * 6f);
                Debug.Log("<color=red> Pace ++ : " + autoJumpPace + "</color>");
                if (birdPlayer.transform.position.y <= offsetHeight2)
                {
                    autoJumpPace = 0.13f;
                    Debug.Log("<color=white> GO STRAIGHT : " + autoJumpPace + "</color>");
                }
            }
            else if (heightDifferent > 0f && heightDifferent < 0.5f && distance > 1.5f)
            {
                autoJumpPace = normalPace;
                Debug.Log("<color=white> GO STRAIGHT : " + autoJumpPace + "</color>");
            }
            else if (heightDifferent > 0f && heightDifferent < 0.5f && distance < 1.5f)
            {
                autoJumpPace = normalPace;
                Debug.Log("<color=white> GO STRAIGHT : " + autoJumpPace + "</color>");
            }
            else if (heightDifferent < 0f && heightDifferent < -0.5f && distance > 1.5f)
            {
                autoJumpPace = normalPace;
                Debug.Log("<color=white> GO STRAIGHT : " + autoJumpPace + "</color>");
                if (birdPlayer.transform.position.y <= offsetHeight2)
                {
                    autoJumpPace = 0.13f;
                    Debug.Log("<color=white> GO STRAIGHT : " + autoJumpPace + "</color>");
                }
                else
                {
                    autoJumpPace = normalPace;
                }
            }
            else if (heightDifferent < 0f && heightDifferent < -0.5f && distance < 1.5f)
            {
                Debug.Log("<color=white> GO STRAIGHT : " + autoJumpPace + "</color>");
                if (birdPlayer.transform.position.y <= offsetHeight2)
                {
                    autoJumpPace = 0.13f;
                    Debug.Log("<color=white> GO STRAIGHT : " + autoJumpPace + "</color>");
                }
                else
                {
                    autoJumpPace = normalPace;
                }
            }
            else
            {
                autoJumpPace = normalPace;
                Debug.Log("<color=white> GO STRAIGHT : " + autoJumpPace + "</color>");
            }
        }
        else
        {
            autoJumpPace = normalPace;
            Debug.Log("<color=white> GO STRAIGHT : " + autoJumpPace + "</color>");
        }

        // slider1.value = autoJumpPace;
        autoJumpPace = Mathf.Clamp(autoJumpPace, minPace, maxPace);
    }

    // private void OnDrawGizmos()
    // {
    //     // Draw Obstacle Bounds
    //     Gizmos.color = Color.red; // Color for the obstacles' bounds


    //     if (nextObstacle != null)
    //     {
    //         // Get RectTransforms of top and bottom obstacles
    //         // RectTransform topObstacleRectTransform = obstacle.transform.GetChild(2).GetComponent<RectTransform>();
    //         RectTransform nextObstaclesRectTransform = nextObstacle.transform.Find("Middle").GetComponent<RectTransform>();

    //         // Draw top obstacle bounds
    //         if (nextObstaclesRectTransform != null)
    //         {
    //             Vector3[] nextObstacleCorners = new Vector3[4];
    //             Transform targetChild = nextObstaclesRectTransform.transform; // Get the first child (you can change the index as needed)

    //             nextObstaclesRectTransform.GetWorldCorners(nextObstacleCorners);
    //             for (int i = 0; i < 4; i++)
    //             {
    //                 Gizmos.DrawLine(nextObstacleCorners[i], nextObstacleCorners[(i + 1) % 4]);
    //             }
    //             if (targetChild != null)
    //             {
    //                 Gizmos.color = Color.yellow; // Choose your desired color
    //                 Gizmos.DrawLine(birdPlayer.transform.position, targetChild.position);
    //             }
    //         }

    //         // Draw bottom obstacle bounds
    //     }

    // }
}


// if (birdPlayer != null)
// {
//     RectTransform birdRectTransform = birdPlayer.GetComponent<RectTransform>();
//     if (birdRectTransform != null)
//     {
//         Gizmos.color = Color.red; // Color for the bird's bounds
//         Vector3[] birdCorners = new Vector3[4];
//         birdRectTransform.GetWorldCorners(birdCorners);

//         // Draw lines connecting bird's corners
//         for (int i = 0; i < 4; i++)
//         {
//             Gizmos.DrawLine(birdCorners[i], birdCorners[(i + 1) % 4]);
//         }
//     }
// }

