﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour, IEventListener {

    // This script handles player controls including movement, aiming, and using skills

    private const float speed = 6f;
    private float movementAngle = 0f;
    private bool facingRight;
    private float aimingAngle = 0f;
    private bool leftStickMovement;
    private bool rightStickAiming = false;
    private Transform arm;
    private Transform head;
    private SpriteRenderer bodyRenderer;
    private SpriteRenderer armRenderer;
    private SpriteRenderer headRenderer;
    private Transform hand;
    private Rigidbody2D playerRigidBody;
    public ISkill[] equipedSkills = new ISkill[3];
    private ISkill activeSkill;
    private int activeSkillIndex;
    private bool stunned = false;
    private bool animationStunned = false;
    private bool focusingPosition = false;
    private bool usingSkillButton = false;
    private bool usingSkillPreviousFrame = false;

    [SerializeField] private Sprite armSprite;
    [SerializeField] private Sprite idleArmSprite;

    void Start() {
        facingRight = true;
        arm = transform.Find("Arm");
        hand = arm.Find("Hand");
        head = transform.Find("Head");
        bodyRenderer = GetComponent<SpriteRenderer>();
        armRenderer = arm.GetComponent<SpriteRenderer>();
        headRenderer = head.GetComponent<SpriteRenderer>();
        playerRigidBody = GetComponent<Rigidbody2D>();

        // Temporary
        equipedSkills[0] = GetComponent<UseSpiritShot>();
        equipedSkills[1] = GetComponent<UseFlamethrower>();
        equipedSkills[2] = GetComponent<UseLightBow>();

        activeSkillIndex = 0;
        activeSkill = equipedSkills[activeSkillIndex];
        EventMessanger.GetInstance().TriggerEvent(new SkillIconEvent(1));
    }

    void OnEnable() {
        EventMessanger.GetInstance().SubscribeEvent(typeof(PlayerVictoryEvent), this);
        EventMessanger.GetInstance().SubscribeEvent(typeof(PlayerDefeatEvent), this);
        EventMessanger.GetInstance().SubscribeEvent(typeof(ConversationStartEvent), this);
        EventMessanger.GetInstance().SubscribeEvent(typeof(ConversationEndEvent), this);
        EventMessanger.GetInstance().SubscribeEvent(typeof(EnemyStartingDataEvent), this);
    }

    void OnDisable() {
        EventMessanger.GetInstance().UnsubscribeEvent(typeof(PlayerVictoryEvent), this);
        EventMessanger.GetInstance().UnsubscribeEvent(typeof(PlayerDefeatEvent), this);
        EventMessanger.GetInstance().UnsubscribeEvent(typeof(ConversationStartEvent), this);
        EventMessanger.GetInstance().UnsubscribeEvent(typeof(ConversationEndEvent), this);
        EventMessanger.GetInstance().UnsubscribeEvent(typeof(EnemyStartingDataEvent), this);
    }

    void FixedUpdate () {
        if (!animationStunned) {
            CheckFocus();
            CheckSkillButton();
            if (!stunned) {
                MovementAndAiming();
            }
        }
    }

    void Update() {
        if (!animationStunned) {
            CheckSwapSkill();
        }
    }

    void MovementAndAiming() {
        // Movement
        // Get left-stick input
        float xInput = Input.GetAxis("Horizontal");
        float yInput = Input.GetAxis("Vertical");
        // Get angle of input
        float angleRad = 0f;
        leftStickMovement = true;
        if (xInput == 0) {
            if (yInput > 0) {
                angleRad = Mathf.PI / 2f;
            } else if (yInput < 0) {
                angleRad = Mathf.PI / -2f;
            } else {
                leftStickMovement = false;
            }
        } else if (yInput == 0 && xInput < 0) {
            angleRad = Mathf.PI;
        } else {
            angleRad = Mathf.Atan2(yInput, xInput);
        }
        // Only change movement angle if left stick has input
        if (leftStickMovement) {
            movementAngle = angleRad * 180 / Mathf.PI;
            // Used for tutorial
            EventMessanger.GetInstance().TriggerEvent(new TutorialPlayerMovedEvent());
        }
        // Get magnitude of input
        float magnitude = Mathf.Sqrt(xInput * xInput + yInput * yInput);
        if (magnitude > 1) magnitude = 1;
        // Move based on input
        if (!focusingPosition) {
            if (playerRigidBody != null) {
                playerRigidBody.AddForce(Vector2.right * magnitude * Mathf.Cos(angleRad) * speed * 100);
                playerRigidBody.AddForce(Vector2.up * magnitude * Mathf.Sin(angleRad) * speed * 100);
            } else {
                gameObject.transform.position += Vector3.right * magnitude * Mathf.Cos(angleRad) * speed * Time.deltaTime;
                gameObject.transform.position += Vector3.up * magnitude * Mathf.Sin(angleRad) * speed * Time.deltaTime;
            }
        }

        //Aiming
        // Get inputs
        float xRInput = Input.GetAxis("Horizontal_R");
        float yRInput = Input.GetAxis("Vertical_R");
        // Find angle
        angleRad = 0f;
        if (xRInput == 0 && yRInput == 0) {
            rightStickAiming = false;
        } else {
            rightStickAiming = true;
            if (xRInput == 0) {
                if (yRInput > 0) {
                    angleRad = Mathf.PI / 2f;
                } else if (yRInput < 0) {
                    angleRad = Mathf.PI / -2f;
                }
            } else if (yRInput == 0 && xRInput < 0) {
                angleRad = Mathf.PI;
            } else {
                angleRad = Mathf.Atan2(yRInput, xRInput);
            }
            aimingAngle = angleRad * 180 / Mathf.PI;
        }

        // Set left/right facing
        if (!rightStickAiming) {
            if (!focusingPosition) {
                // set to orthogonal direction from left stick
                if (movementAngle >= -157.5f && movementAngle < -112.5f) {
                    // facing down-left
                    aimingAngle = -135f;
                    facingRight = false;
                } else if (movementAngle >= -112.5f && movementAngle < -67.5f) {
                    // facing downward
                    aimingAngle = -90f;
                } else if (movementAngle >= -67.5f && movementAngle < -22.5f) {
                    // facing down-right
                    aimingAngle = -45f;
                    facingRight = true;
                } else if (movementAngle >= -22.5f && movementAngle < 22.5f) {
                    // facing right
                    aimingAngle = 0f;
                    facingRight = true;
                } else if (movementAngle >= 22.5f && movementAngle < 67.5f) {
                    // facing up-right
                    aimingAngle = 45f;
                    facingRight = true;
                } else if (movementAngle >= 67.5f && movementAngle < 112.5f) {
                    // facing up
                    aimingAngle = 90f;
                } else if (movementAngle >= 112.5f && movementAngle < 157.5f) {
                    // facing up-left
                    aimingAngle = 135f;
                    facingRight = false;
                } else if (movementAngle >= 157.5f || movementAngle < -157.5f) {
                    // facing left
                    aimingAngle = 180f;
                    facingRight = false;
                }
                // if not moving, snap aim to left or right
                if (!leftStickMovement) {
                    aimingAngle = facingRight ? 0f : 180f;
                }
            } else {
                // if focusing, use continuous angles.
                aimingAngle = movementAngle;
                facingRight = aimingAngle > 90f || aimingAngle < -90f ? false : true;
            }
        // determined by aiming angle
        } else {
            if (aimingAngle > -90 && aimingAngle < 90) {
                facingRight = true;
            } else {
                facingRight = false;
            }
        }
        // flip sprites and set values accordingly
        if (!facingRight) {
            gameObject.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            bodyRenderer.flipY = true;
            armRenderer.flipY = true;
            headRenderer.flipY = true;
            if (arm.localPosition.y > 0) {
                arm.localPosition = new Vector3(arm.localPosition.x, -arm.localPosition.y, arm.localPosition.z);
            }
            if (head.localPosition.y > 0) {
                head.localPosition = new Vector3(head.localPosition.x, -head.localPosition.y, head.localPosition.z);
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
         }



        // Use skill or aim
        if (rightStickAiming || usingSkillButton || focusingPosition) {
            // Rotate arm after determining direction
            arm.rotation = Quaternion.Euler(0f, 0f, aimingAngle);
            armRenderer.sprite = armSprite;
            if (rightStickAiming || usingSkillButton && !stunned) {
                activeSkill.UseSkill(hand);
                usingSkillPreviousFrame = true;
            } else if (!(rightStickAiming || usingSkillButton) && !stunned) {
                if (usingSkillPreviousFrame) {
                    activeSkill.ReleaseSkill();
                }
                // Used when the player is holding their position (to aim the bow)
                activeSkill.AimSkill(hand);
                usingSkillPreviousFrame = false;
            }
        } else {
            // Reset arm to idle pose when not aiming.
            ResetArmRenderer();
        }
    }

    void CheckFocus() {
        if (Input.GetButton("RB") || Input.GetButton("LB")) {
            focusingPosition = true;
        } else {
            focusingPosition = false;
        }
    }

    void CheckSkillButton() {
        if (Input.GetButton("A")) {
            usingSkillButton = true;
        } else {
            usingSkillButton = false;
        }
    }

    void CheckSwapSkill() {
        if (Input.GetButtonDown("X")) {
            ChangeActiveSkill();
            EventMessanger.GetInstance().TriggerEvent(new TutorialSkillSwapEvent());
        }
    }

    void ChangeActiveSkill() {
        activeSkill.SetInactiveSkill();
        activeSkillIndex++;
        activeSkillIndex %= equipedSkills.Length;
        activeSkill = equipedSkills[activeSkillIndex];
        activeSkill.SetActiveSkill();
        EventMessanger.GetInstance().TriggerEvent(new SkillIconEvent(activeSkillIndex + 1));
    }

    // Used to reset arm to idle position in event player is stunned
    private void ResetArmRenderer() {
        if (usingSkillPreviousFrame) {
            activeSkill.ReleaseSkill();
        }
        arm.rotation = Quaternion.Euler(0f, 0f, facingRight ? 0f : 180f);
        armRenderer.sprite = idleArmSprite;
        activeSkill.AimSkill(hand);
        usingSkillPreviousFrame = false;
    }

    private void AnimationLock() {
        this.animationStunned = true;
        float animationTimer = 2.75f;
        StartCoroutine(AnimationLockSubroutine(animationTimer));
    }

    private IEnumerator AnimationLockSubroutine(float duration) {
        while (duration > 0) {
            duration -= Time.deltaTime;
            yield return null;
        }
        this.animationStunned = false;
    }

    private void KO() {
        AudioManager.GetInstance().StopSound(Sound.FireLoop);
        StunPlayer();
        animationStunned = true;
        gameObject.layer = 8;
        bodyRenderer.color = new Color(1, 1, 1, 0f);
        armRenderer.color = new Color(1, 1, 1, 0f);
        headRenderer.color = new Color(1, 1, 1, 0f);
    }

    private void FightOver() {
        AudioManager.GetInstance().StopSound(Sound.FireLoop);
        StunPlayer();
        animationStunned = true;
        gameObject.layer = 8;
    }

    public void StunPlayer() {
        this.stunned = true;
        activeSkill.ReleaseSkill();
        ResetArmRenderer();
    }

    public void UnStunPlayer() {
        this.stunned = false;
    }

    public void ConsumeEvent(IEvent e) {
        if (e.GetType() == typeof(PlayerVictoryEvent)) {
            FightOver();
        } else if (e.GetType() == typeof(PlayerDefeatEvent)) {
            KO();
        } else if (e.GetType() == typeof(ConversationStartEvent)) {
            StunPlayer();
        } else if (e.GetType() == typeof(ConversationEndEvent)) {
            UnStunPlayer();
        } else if (e.GetType() == typeof(EnemyStartingDataEvent)) {
            AnimationLock();
        }
    }

}
