using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
public class FPSPlayerMovement : MonoBehaviour {

    // todo fix camera jitteryness when near 270f or 90f

    [Range(1f, 1000f)][SerializeField] float speed = 20f;
    [Range(1f, 100f)] [SerializeField] float sensitivity = 20f;

    Camera mainCamera;

	// Use this for initialization
	void Start () {
        mainCamera = GetComponentInChildren<Camera>();
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

}
