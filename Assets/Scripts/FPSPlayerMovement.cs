using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
public class FPSPlayerMovement : MonoBehaviour {

    [Range(1f, 1000f)][SerializeField] float speed = 20f;
    [Range(1f, 100f)] [SerializeField] float mouseSensitivity = 20f;
    [Range(1f, 1000f)] [SerializeField] float gamepadSensitivity = 100f;
    [Tooltip("Set the value the same as it is in the InputManager")][SerializeField] float gamepadDeadzone = 0.05f;
    [SerializeField] Transform playerBody;
    [SerializeField] CartController playerHands;

    Camera mainCamera;
    Rigidbody playerRigidBody;

    void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
    }

	// Use this for initialization
	void Start () {
        mainCamera = FindObjectOfType<Camera>();
        playerRigidBody = GetComponent<Rigidbody>();
    }
	
	// Update is called once per frame
	void Update () {
        ProcessTranslation();
        ProcessRotation();
	}

    private void ProcessTranslation() {
        // Get X and Y throw, factor in the speed and time since the last frame,
        // and translate the Player
        float xThrow = CrossPlatformInputManager.GetAxis("Horizontal");
        float xOffset = xThrow * speed * Time.deltaTime;

        float zThrow = CrossPlatformInputManager.GetAxis("Vertical");
        float zOffset = zThrow * speed * Time.deltaTime;

        playerBody.Translate(
            xOffset,
            0f,
            zOffset
        );
    }

    private void ProcessRotation() {
        ProcessLookUpAndDownRotation();
        ClampLookUpAndDownRotation();
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
            print("Processing X Joystick");
            xProperRotation = xRotationGamepad;
            xOffset = xProperRotation * gamepadSensitivity;
        } else {
            print("Processing X Mouse");
            xProperRotation = xRotationMouse;
            xOffset = xProperRotation * mouseSensitivity;
        }

        xOffset *= Time.deltaTime;
        Vector3 offsetRotation = new Vector3(xOffset, 0f, 0f);
        mainCamera.transform.Rotate(offsetRotation);
    }

    private void ClampLookUpAndDownRotation() {
        // Clamp X between 0 and 90 deg
        // or Clamp between 270f and 360f
        // When rotation goes below 0, it wraps around to 360f
        float xRotRaw = mainCamera.transform.localEulerAngles.x;
        float xRotFix;
        if (xRotRaw <= 90)
            xRotFix = Mathf.Clamp(xRotRaw, 0f, 90f);
        else
            xRotFix = Mathf.Clamp(xRotRaw, 270f, 360f);

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
            print("Processing Y Joystick");
            yProperRotation = yRotationGamepad;
            yOffset = yProperRotation * gamepadSensitivity;
        } else {
            print("Processing Y Mouse");
            yProperRotation = yRotationMouse;
            yOffset = yProperRotation * mouseSensitivity;
        }

        yOffset *= Time.deltaTime;
        Vector3 offsetRotation = new Vector3(0f, yOffset, 0f);

        playerBody.Rotate(offsetRotation);
    }
}