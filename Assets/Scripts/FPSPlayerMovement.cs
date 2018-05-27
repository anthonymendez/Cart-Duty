using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
public class FPSPlayerMovement : MonoBehaviour {

    // todo fix camera jitteryness when near 270f or 90f

    [Range(1f, 1000f)][SerializeField] float speed = 20f;
    [Range(1f, 100f)] [SerializeField] float sensitivity = 20f;
    [SerializeField] Material outline;

    Camera mainCamera;
    Rigidbody playerRigidBody;
    bool grabButtonDown, releaseButtonDown;
    Vector3 screenCenter;
    Ray playerLookDirection;
    RaycastHit hitCart;
    [Tooltip("In Meters")][SerializeField] float maxDistanceGrab = 2f;
    [Tooltip("Tag")][SerializeField] String cartLayerName = "Carts";
    int layerMask;
    bool isLookingAtCart;
    bool isHoldingCart;
    Cart cartInHands;
    Transform cartLastLookedAt;
    Rigidbody cartRigidBody;

    void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
    }

	// Use this for initialization
	void Start () {
        mainCamera = GetComponentInChildren<Camera>();
        playerRigidBody = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
        ProcessTranslation();
        ProcessRotation();
        ProcessGrabCart();
	}

    private void ProcessTranslation() {
        // Get X and Y throw, factor in the speed and time since the last frame,
        // and translate the Player
        float xThrow = CrossPlatformInputManager.GetAxis("Horizontal");
        float xOffset = xThrow * speed * Time.deltaTime;

        float zThrow = CrossPlatformInputManager.GetAxis("Vertical");
        float zOffset = zThrow * speed * Time.deltaTime;

        transform.Translate(
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
        float xRot = -CrossPlatformInputManager.GetAxisRaw("Mouse Y");
        float xOffset = xRot * sensitivity * Time.deltaTime;

        mainCamera.transform.Rotate(
            xOffset,
            0f,
            0f
        );
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
        float yRot = CrossPlatformInputManager.GetAxisRaw("Mouse X");
        float yOffset = yRot * sensitivity * Time.deltaTime;

        transform.Rotate(
            0f,
            yOffset,
            0f
        );
    }

    private void ProcessGrabCart() {
        grabButtonDown = CrossPlatformInputManager.GetButton("Fire1");
        releaseButtonDown = CrossPlatformInputManager.GetButton("Fire2");

        HandleRaycasting();
    }

    private void HandleRaycasting() {
        screenCenter = new Vector3(0.5f, 0.5f, 0f);
        playerLookDirection = mainCamera.ViewportPointToRay(screenCenter);
        maxDistanceGrab = 1f;
        layerMask = LayerMask.GetMask(cartLayerName);
        isLookingAtCart = Physics.Raycast(playerLookDirection, out hitCart, maxDistanceGrab, layerMask);
        Debug.DrawLine(playerLookDirection.origin, hitCart.point);

        if (isLookingAtCart) {
            HandleCartControls();
            cartLastLookedAt.GetComponent<Cart>().ActivateOutline();
        } else {
            if(cartLastLookedAt != null)
                cartLastLookedAt.GetComponent<Cart>().DeactivateOutline();
        }
    }

    private void HandleCartControls() {
        cartLastLookedAt = hitCart.transform;
        cartRigidBody = cartLastLookedAt.GetComponent<Rigidbody>();

        bool isFallenOver = (cartLastLookedAt.localEulerAngles.x >= 45 || cartLastLookedAt.localEulerAngles.x <= -45) || (cartLastLookedAt.localEulerAngles.z >= 45 || cartLastLookedAt.localEulerAngles.z <= -45);
        bool isLookingAtHandlebars = hitCart.collider.CompareTag("Handlebar");

        if (grabButtonDown) {
            cartInHands = cartLastLookedAt.GetComponent<Cart>();
            isHoldingCart = true;

            if (isFallenOver) {
                // Press R to rotate X and Z back to 0 deg
            } else if (isLookingAtHandlebars) {

            }
        } else if (releaseButtonDown) {
            isHoldingCart = false;
        }
    }
}