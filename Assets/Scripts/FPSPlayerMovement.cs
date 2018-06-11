using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

// <summary>
// Handles player movement and looking controls
// </summary>
public class FPSPlayerMovement : MonoBehaviour {

    [Header("Player Movement")]
    [Tooltip("Movement force applied on player. Never exact, but will be around. In Newtons")][SerializeField] float movementForce = 50f;
    [Tooltip("Limit of the player's velocity magnitude. In Meters/Second")] [SerializeField] float playerVelocityLimit = 10f;

    [Header("Camera Rotation")]
    [Tooltip("Multiplier for how sensitive the mouse looking is")][Range(1f, 100f)] [SerializeField] float mouseSensitivity = 20f;
    [Tooltip("Multiplier for how sensitive the gamepad looking is")] [Range(1f, 1000f)] [SerializeField] float gamepadSensitivity = 100f;
    [Tooltip("Set the value the same as it is in the InputManager")][SerializeField] float gamepadDeadzone = 0.05f;

    [Header("Other")]
    [SerializeField] Transform playerBody;
    [SerializeField] CartController playerHands;

    Camera mainCamera;
    Rigidbody playerRigidBody;
    private Vector3 forceToApply;
    private Vector3 forceOnPlayer;
    Vector3 upAndDownRotation;
    bool isTranslationButtonDown;

    // <summary>
    // Returns the force being applied onto the player
    // </summary>
    public Vector3 GetForceToApply() {
        return forceToApply;
    }

    // <summary>
    // Returns the velocity limit of the player
    // </summary>
    public float GetPlayerVelocityLimit() {
        return playerVelocityLimit;
    }

    // <summary>
    // Locks the cursor to the center of the screen
    // </summary>
    void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // <summary>
    // Assigns an initial value to some variables
    // </summary>
    void Start () {
        mainCamera = FindObjectOfType<Camera>();
        playerRigidBody = GetComponentInChildren<Rigidbody>();
        forceToApply = Vector3.zero;
        forceOnPlayer = Vector3.zero;
        isTranslationButtonDown = false;
    }

    // <summary>
    // Track the given inputs from the player.
    // </summary>
    void Update () {
        ProcessTranslation();
        ProcessRotation();
	}

    // <summary>
    // Applies any Physics based interactions.
    // </summary>
    void FixedUpdate() {
        ApplyTranslationForce();
        ClampVelocity();
    }
    
    // <summary>
    // Applies can camera rotation changes.
    // </summary>
    void LateUpdate() {
        ClampLookUpAndDownRotation();
        ApplyUpAndDownCameraRotation();
    }

    // <summary>
    // Tracks the movement input from the player, and computes into a 
    // force that will be applied on the player and later used on the cart.
    // </summary>
    private void ProcessTranslation() {
        float xThrow = CrossPlatformInputManager.GetAxis("Horizontal");
        float zThrow = CrossPlatformInputManager.GetAxis("Vertical");
        isTranslationButtonDown = (xThrow != Mathf.Epsilon) || (zThrow != Mathf.Epsilon);

        // If we're not translating, no need to process any of this then
        if (isTranslationButtonDown) {
            // Get X and Y throw, factor in the speed and time since the last frame,
            // and calculate the force to apply on player
            float xOffset = xThrow * movementForce * Time.deltaTime;
            float zOffset = zThrow * movementForce * Time.deltaTime;

            forceToApply = new Vector3(xOffset, 0f, zOffset);

            // If the player is grabbing a cart, we won't apply their horizontal movement,
            // because the carts do not have 4 wheel steering
            if (playerHands.IsGrabbingCartHandlebars())
                forceOnPlayer = new Vector3(0f, 0f, zOffset);
            else
                forceOnPlayer = forceToApply;
        }
    }

    // <summary>
    // Apply the force calculated in ProcessTranslation to the player.
    // </summary>
    private void ApplyTranslationForce() {
        playerRigidBody.AddRelativeForce(forceOnPlayer, ForceMode.VelocityChange);
    }

    // <summary>
    // Clamp the player velocity to the set velocity limit.
    // Does not apply to the Y axis.
    // </summary>
    private void ClampVelocity() {
        // If we're not pressing the move buttons, then we don't clamp our speed.
        if(isTranslationButtonDown) {
            float playerVelocityMagnitude = playerRigidBody.velocity.magnitude;
            // We keep the same Y velocity as before in case we're falling
            float playerVelocityY = playerRigidBody.velocity.y;

            if (playerVelocityMagnitude > playerVelocityLimit) {
                playerRigidBody.velocity = Vector3.ClampMagnitude(playerRigidBody.velocity, playerVelocityLimit);
                playerRigidBody.velocity = new Vector3(playerRigidBody.velocity.x, playerVelocityY, playerRigidBody.velocity.z);
            }
        }
    }

    // <summary>
    // Initiate rotation process for the player.
    // AKA: Calculating where the player is looking
    // </summary>
    private void ProcessRotation() {
        ProcessLookUpAndDownRotation();
        ProcessLeftAndRightRotation();
    }

    // <summary>
    // We track the inputs of the player, determine whether it's the gamepad or mouse,
    // and compute a value for how much the player is rotating their view.
    // </summary>
    private void ProcessLookUpAndDownRotation() {
        // Move mainCamera X rot for up and down
        float xRotationMouse = -CrossPlatformInputManager.GetAxisRaw("Mouse Y");
        float xRotationGamepad = -CrossPlatformInputManager.GetAxisRaw("Joy Y");
        float xRotationGamepadNormalized = Mathf.Abs(xRotationGamepad);
        float xProperRotation;
        float xOffset = 0f;
        
        // Determine whether or not we're using a gamepad or mouse. 
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

    // <summary>
    // Prevents the player from looking up more than +90 degrees, 
    // or looking down more than -90 degrees.
    // </summary>
    private void ClampLookUpAndDownRotation() {
        // Clamp X between 0 and 90 deg
        // or Clamp between 270f and 360f
        // When rotation goes below 0, it wraps around to 360f
        float xRotOrig = mainCamera.transform.localEulerAngles.x;
        float xRotOffset = upAndDownRotation.x;
        float xRotRaw = xRotOrig + xRotOffset;
        float xRotFix = xRotRaw;
        float xRotFinal;

        // Because of the way Unity does it's EulerAngles,
        // we have to change the rotation to be 
        // between -180 deg and 180 deg
        while(xRotFix > 180f) {
            xRotFix -= 360f;
        }
        while(xRotFix < -180f) {
            xRotFix += 360f;
        }

        xRotFinal = Mathf.Clamp(xRotFix, -90f, 90f);

        upAndDownRotation.x = xRotFinal - mainCamera.transform.localEulerAngles.x;
    }

    // <summary>
    // Apply the rotation value for looking up and down.
    // </summary>
    private void ApplyUpAndDownCameraRotation() {
        mainCamera.transform.Rotate(upAndDownRotation);
    }

    // <summary>
    // Track the input for the player looking left and right, determine
    // whether the player is using a gamepad or mouse, and calculate a
    // rotation value and apply it.
    // </summary>
    private void ProcessLeftAndRightRotation() {
        // Move Player Y rot for left and right
        float yRotationMouse = CrossPlatformInputManager.GetAxisRaw("Mouse X");
        float yRotationGamepad = CrossPlatformInputManager.GetAxisRaw("Joy X");
        float yRotationGamepadNormalized = Mathf.Abs(yRotationGamepad);
        float yProperRotation;
        float yOffset = 0f;

        // Determine whether we're using a gamepad or mouse
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