﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarkPlayerController : MonoBehaviour {

    private int phase;
    [SerializeField] private Transform hand;

    private SpriteRenderer bodyRenderer;
    private Transform arm;
    private SpriteRenderer armRenderer;
    private Transform head;
    private SpriteRenderer headRenderer;
    private Transform Satoration;
    private SpriteRenderer SatorationRenderer;
    private Animator animator;

    private bool facingRight = true;

    private const int SHOTS_PER_WAVE = 9;
    private const float STORM_SPREAD_ANGLE = 135f;

    [SerializeField] private GameObject spiritShotPrefab;
    [SerializeField] private GameObject darkBowPrefab;
    [SerializeField] private GameObject darkArrowPrefab;
    [SerializeField] private GameObject darkFlamePrefab;
    private int spiritShotIndex;
    private int darkBowIndex;
    private int darkArrowIndex;
    private int darkFlameIndex;
    

    private float stormTimer = 0;
    private float arrowTimer = 0f;
    private float warpTimer = 0f;
    private const float WARP_DURATION = 1f;

    void Start() {
        spiritShotIndex = ObjectPooler.instance.GetIndex(spiritShotPrefab);
        darkBowIndex = ObjectPooler.instance.GetIndex(darkBowPrefab);
        darkArrowIndex = ObjectPooler.instance.GetIndex(darkArrowPrefab);
        darkFlameIndex = ObjectPooler.instance.GetIndex(darkFlamePrefab);

        bodyRenderer = GetComponent<SpriteRenderer>();
        arm = transform.Find("DarkPlayer_Arm");
        armRenderer = arm.GetComponent<SpriteRenderer>();
        head = transform.Find("DarkPlayer_Head");
        headRenderer = head.GetComponent<SpriteRenderer>();
        Satoration = transform.Find("Satoration");
        SatorationRenderer = Satoration.GetComponent<SpriteRenderer>();

        animator = GetComponent<Animator>();
    }

    void Update() {
        arrowTimer -= Time.deltaTime;
        if (arrowTimer <= 0) {
            //StartCoroutine(Storm(10, -45));
            //Arrow();
            //StartCoroutine(ArcherStorm());
            //StartCoroutine(CrossFire());
            StartCoroutine(UnholyImmolation());
            arrowTimer = 10f;
        }

        warpTimer -= Time.deltaTime;
        if (warpTimer <= 0) {
            StartCoroutine(Warp());
            warpTimer = 5f;
        }

        if (!facingRight) {
            gameObject.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            bodyRenderer.flipY = true;
            armRenderer.flipY = true;
            headRenderer.flipY = true;
            SatorationRenderer.flipY = true;
            if (arm.localPosition.y > 0) {
                arm.localPosition = new Vector3(arm.localPosition.x, -arm.localPosition.y, arm.localPosition.z);
            }
            if (head.localPosition.y > 0) {
                head.localPosition = new Vector3(head.localPosition.x, -head.localPosition.y, head.localPosition.z);
            }
            if (Satoration.localPosition.y > 0) {
                Satoration.localPosition = new Vector3(Satoration.localPosition.x, -Satoration.localPosition.y, Satoration.localPosition.z);
            }
         } else {
            gameObject.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            bodyRenderer.flipY = false;
            armRenderer.flipY = false;
            headRenderer.flipY = false;
            if (arm.localPosition.y < 0) {
                arm.localPosition = new Vector3(arm.localPosition.x, -arm.localPosition.y, arm.localPosition.z);
            }
            if (head.localPosition.y < 0) {
                head.localPosition = new Vector3(head.localPosition.x, -head.localPosition.y, head.localPosition.z);
            }
            if (Satoration.localPosition.y < 0) {
                Satoration.localPosition = new Vector3(Satoration.localPosition.x, -Satoration.localPosition.y, Satoration.localPosition.z);
            }
        }
    }

    private IEnumerator Warp() {
        animator.SetTrigger("WarpOut");
        AudioManager.GetInstance().PlaySound(Sound.WarpOut);
        yield return new WaitForSeconds(WARP_DURATION);
        animator.SetTrigger("WarpIn");
        AudioManager.GetInstance().PlaySound(Sound.WarpIn);
        yield return null;
        animator.SetTrigger("Idle");
    }

    private IEnumerator Storm(int waves, float aimedAngle) {
        stormTimer = 10f;
        arm.rotation = Quaternion.Euler(0f, 0f, aimedAngle);
        for (int x = 0; x < waves; x++) {
            // Alternate patterns
            if (x % 2 == 0) {
                for (int y = 0; y < SHOTS_PER_WAVE; y++) {
                    GameObject spiritShot = ObjectPooler.instance.GetDanmaku(spiritShotIndex);
                    SpiritShotEnemy spiritShotScript = spiritShot.GetComponent<SpiritShotEnemy>();
                    spiritShot.transform.position = hand.position;
                    spiritShotScript.SetOwner(gameObject);
                    // Orient each shot
                    spiritShotScript.SetAngles(aimedAngle - (STORM_SPREAD_ANGLE / 2f) + (STORM_SPREAD_ANGLE * y / SHOTS_PER_WAVE));
                    spiritShot.SetActive(true);
                }
            } else {
                for (int y = 0; y < SHOTS_PER_WAVE - 1; y++) {
                    GameObject spiritShot = ObjectPooler.instance.GetDanmaku(spiritShotIndex);
                    SpiritShotEnemy spiritShotScript = spiritShot.GetComponent<SpiritShotEnemy>();
                    spiritShot.transform.position = hand.position;
                    spiritShotScript.SetOwner(gameObject);
                    // Orient each shot
                    spiritShotScript.SetAngles(aimedAngle - (STORM_SPREAD_ANGLE / 2f) + (STORM_SPREAD_ANGLE * y / (SHOTS_PER_WAVE - 1)));
                    spiritShot.SetActive(true);
                }
            }
            yield return new WaitForSeconds(0.4f);
        }
    }

    private void Arrow() {
        GameObject darkBow = ObjectPooler.instance.GetDanmaku(darkBowIndex);
        DarkBow darkBowScript = darkBow.GetComponent<DarkBow>();
        darkBowScript.SetOwner(gameObject);
        darkBow.transform.position = new Vector3(transform.position.x + 1.5f, transform.position.y + Random.Range(-1f, 1f), 0f);
        darkBow.SetActive(true);
        StartCoroutine(darkBowScript.AutoChargeAndFire());
        arrowTimer = 2f;
    }

    private IEnumerator ArcherStorm() {

        // Set up
        const int NUM_BOWS = 28;

        Vector3[] positions = new Vector3[NUM_BOWS];
        // calculate position based on number of bows
        for (int x = 0; x < NUM_BOWS; x++) {
            // find y position
            float yLevel = Mathf.FloorToInt(x / 2) - (NUM_BOWS / 4f) + .5f;
            float height = yLevel * 0.75f;
            // give x position based on x
            if (x % 4 == 0) {
                positions[x] = new Vector3(-7f, height, 0f);
            } else if (x % 4 == 1) {
                positions[x] = new Vector3(7f, height, 0f);
            } else if (x % 4 == 2) {
                positions[x] = new Vector3(-6.5f, height, 0f);
            } else if (x % 4 == 3) {
                positions[x] = new Vector3(6.5f, height, 0f);
            }
        }

        DarkBow[] bows = new DarkBow[NUM_BOWS];
        for (int x = 0; x < NUM_BOWS; x++) {
            GameObject bow = ObjectPooler.instance.GetDanmaku(darkBowIndex);
            if (bow == null) {
                Debug.LogError("Missing bow");
            }
            DarkBow bowScript = bow.GetComponent<DarkBow>();
            if (bowScript != null) {
                bows[x] = bowScript;
            } else {
                Debug.LogError("No DarkBow script.");
            }
            bowScript.SetOwner(gameObject);
            bow.transform.position = positions[x];
            if (bow.transform.position.x > 0) {
                bow.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            } else {
                bow.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
            bow.SetActive(true);
        }

        // Attack portion
        const float DURATION = 10f;
        const float COOLDOWN = 0.5f;

        for (int x = 0; x < DURATION / COOLDOWN; x++) {
            int randIndex;
            int attemptLimit = NUM_BOWS;
            bool attacked = false;
            do {
                randIndex = Random.Range(0, NUM_BOWS);
                if (!bows[randIndex].IsBusy()) {
                    StartCoroutine(bows[randIndex].AutoChargeAndFire());
                    attacked = true;
                }
                attemptLimit--;
            } while (!attacked && attemptLimit > 0);
            yield return new WaitForSeconds(0.5f);
        }

        // Cleanup
        for (int x = 0; x < NUM_BOWS; x++) {
            bows[x].Deactivate();
        }

    }

    private IEnumerator CrossFire() {
        const int NUM_FLAMES = 10;

        for (int x = 0; x < NUM_FLAMES; x++) {
            float xLevel = Mathf.FloorToInt(x / 2) - (NUM_FLAMES / 4f) + 0.5f;
            float xPos = xLevel * 3f;
            print (x + ": " + xPos);

            GameObject darkFlame = ObjectPooler.instance.GetDanmaku(darkFlameIndex);
            if (darkFlame == null) Debug.LogError("Dark flame not found in pool.");
            DarkFlame darkFlameScript = darkFlame.GetComponent<DarkFlame>();
            if (darkFlameScript == null) Debug.LogError("Dark flame script not found");
            darkFlameScript.SetOwner(gameObject);
            darkFlame.transform.position = new Vector3(xPos, 5f, 0f);
            if (x % 2 == 0) {
                darkFlameScript.SetFalling(2f, 1.5f);
            } else {
                darkFlameScript.SetFalling(2f, -1.5f);
            }
            darkFlame.SetActive(true);
        }

        yield return null;
    }

    private IEnumerator UnholyImmolation() {
        const int NUM_FLAMES = 11;
        Vector3 CENTER_POINT = Vector3.zero;
        float CONVERGE_SPEED = 1f;
        float ANGULAR_SPEED = 2f;

        // Set up
        GameObject[] flames = new GameObject[NUM_FLAMES];
        float randomOffset = Random.Range(0f, 2 * Mathf.PI);
        for (int x = 0; x < NUM_FLAMES; x++) {
            GameObject flame = ObjectPooler.instance.GetDanmaku(darkFlameIndex);
            if (flame == null) Debug.LogError("Dark flame not found in pool.");
            flames[x] = flame;
            DarkFlame darkFlameScript = flame.GetComponent<DarkFlame>();
            if (darkFlameScript == null) Debug.LogError("Dark flame script not found.");
            darkFlameScript.SetOwner(gameObject);
            darkFlameScript.SetIdle();

            float xPos = 6 * Mathf.Sin((x * 2 * Mathf.PI / (NUM_FLAMES + 1)) + randomOffset);
            float yPos = 6 * Mathf.Cos(x * 2 * Mathf.PI / (NUM_FLAMES + 1) + randomOffset);
            flame.transform.position = new Vector3(xPos, yPos, 0f);

            flame.SetActive(true);
        }

        yield return null;

        // Attack
        float timeOut = 20f;
        bool converged = false;
        while (!converged && timeOut > 0) {
            for (int x = 0; x < NUM_FLAMES; x++) {
                GameObject flame = flames[x];
                Vector3 distanceVector = flame.transform.position - CENTER_POINT;
                flame.transform.position -= distanceVector.normalized * CONVERGE_SPEED * Time.deltaTime;
                float distance = distanceVector.magnitude - CONVERGE_SPEED * Time.deltaTime;
                if (distance < 0.05f) converged = true;
                float angle = Vector3.Angle(distanceVector, Vector3.right);
                if (distanceVector.y < 0) angle = -1 * Mathf.Abs(angle);
                angle *= Mathf.Deg2Rad;
                angle -= ANGULAR_SPEED * Time.deltaTime;
                flame.transform.position = new Vector3(distance * Mathf.Cos(angle), distance * Mathf.Sin(angle), 0f);
            }
            yield return null;
            timeOut -= Time.deltaTime;

            //converged = true;
        }
        for (int x = 0; x < NUM_FLAMES; x++) {
            DarkFlame darkFlame = flames[x].GetComponent<DarkFlame>();
            darkFlame.Deactivate();
        }
    }

}