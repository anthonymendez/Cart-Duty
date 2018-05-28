using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
public class FPSPlayerMovement : MonoBehaviour {

    [Tooltip("In N")][SerializeField] float movementForce = 200f;
    [SerializeField] float playerVelocityLimit = 10f;
    [Range(1f, 100f)] [SerializeField] float mouseSensitivity = 20f;
    [Range(1f, 1000f)] [SerializeField] float gamepadSensitivity = 100f;
    [Tooltip("Set the value the same as it is in the InputManager")][SerializeField] float gamepadDeadzone = 0.05f;
    [SerializeField] Transform playerBody;
    [SerializeField] CartController playerHands;

    Camera mainCamera;
    Rigidbody playerRigidBody;
    private Vector3 forceToApply;
    Vector3 upAndDownRotation;

    public Vector3 GetForceToApply() {
        return forceToApply;
    }

    public float GetPlayerVelocityLimit() {
        return playerVelocityLimit;
    }

    void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
    }

	// Use this for initialization
	void Start () {
        mainCamera = FindObjectOfType<Camera>();
        playerRigidBody = GetComponentInChildren<Rigidbody>();

        forceToApply = Vector3.zero;
    }
	
	// Update is called once per frame
	void Update () {
        ProcessTranslation();
        ProcessRotation();
	}

    // Apply Physics here
    void FixedUpdate() {
        ApplyTranslationForce();
        ClampVelocity();
    }

    // Apply Camera movement here
    void LateUpdate() {
        ApplyUpAndDownCameraRotation();
        ClampLookUpAndDownRotation();
    }

    private void ProcessTranslation() {
        // Get X and Y throw, factor in the speed and time since the last frame,
        // and calculate the force to apply on player
        float xThrow = CrossPlatformInputManager.GetAxis("Horizontal");
        float xOffset = xThrow * movementForce * Time.deltaTime;

        float zThrow = CrossPlatformInputManager.GetAxis("Vertical");
        float zOffset = zThrow * movementForce * Time.deltaTime;

        forceToApply = new Vector3(xOffset, 0f, zOffset);
    }

    private void ApplyTranslationForce() {
        playerRigidBody.AddRelativeForce(forceToApply);
    }
    
    private void ClampVelocity() {
        float playerVelocityMagnitude = playerRigidBody.velocity.magnitude;

        if (playerVelocityMagnitude > playerVelocityLimit) {
            playerRigidBody.velocity = Vector3.ClampMagnitude(playerRigidBody.velocity, playerVelocityLimit);
        }
    }

    private void ProcessRotation() {
        ProcessLookUpAndDownRotation();
        ProcessLeftAndRightRotation();
    }

    private void ProcessLookUpAndDownRotation() {
        // Move mainCamera X rot for up and down
        float xRotationMouse = -CrossPlatformInputManager.GetAxisRaw("Mouse Y");
        float xRotationGamepad = -CrossPlatformInputManager.GetAxisRaw("Joy Y");
        float xRotationGamepadNormalized = Mathf.Abs(xRotationGamepad);
        float xProperRotation;
        float xOffset = 0f;
        
        if (xRotationGamepadNormalized <= 1.0f && xRotationGamepadNormalized > gamepadDeadzone) {
            xProperRotation = xRotationGamepad;
            xOffset = xProperRotation * gamepadSensitivity;
        } else {
            xProperRotation = xRotationMouse;
            xOffset = xProperRotation * mouseSensitivity;
        }

        xOffset *= Time.deltaTime;
        upAndDownRotation = new Vector3(xOffset, 0f, 0f);
    }

    private void ApplyUpAndDownCameraRotation() {
        mainCamera.transform.Rotate(upAndDownRotation);
    }

    private void ClampLookUpAndDownRotation() {
        // Clamp X between 0 and 90 deg
        // or Clamp between 270f and 360f
        // When rotation goes below 0, it wraps around to 360f
        float xRotRaw = mainCamera.transform.localEulerAngles.x;
        float xRotFix;
        if (xRotRaw <= 90) {
            xRotFix = Mathf.Clamp(xRotRaw, 0f, 90f);
        } else {
            xRotFix = Mathf.Clamp(xRotRaw, 270f, 360f);
        }
        
        mainCamera.transform.localRotation = Quaternion.Euler(
            xRotFix,
            mainCamera.transform.localRotation.y,
            mainCamera.transform.localRotation.z
        );
    }

    private void ProcessLeftAndRightRotation() {
        // Move Player Y rot for left and right
        float yRotationMouse = CrossPlatformInputManager.GetAxisRaw("Mouse X");
        float yRotationGamepad = CrossPlatformInputManager.GetAxisRaw("Joy X");
        float yRotationGamepadNormalized = Mathf.Abs(yRotationGamepad);
        float yProperRotation;
        float yOffset = 0f;

        if(yRotationGamepadNormalized <= 1.0f && yRotationGamepadNormalized > gamepadDeadzone) {
            yProperRotation = yRotationGamepad;
            yOffset = yProperRotation * gamepadSensitivity;
        } else {
            yProperRotation = yRotationMouse;
            yOffset = yProperRotation * mouseSensitivity;
        }

        yOffset *= Time.deltaTime;
        Vector3 offsetRotation = new Vector3(0f, yOffset, 0f);

        playerBody.Rotate(offsetRotation);
    }
}