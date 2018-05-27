using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
public class CartController : MonoBehaviour {

    [Tooltip("In Meters")] [SerializeField] float maxDistanceGrab = 1f;
    [Tooltip("In Meters")] [SerializeField] float distanceWhenGrabbed = 1f;
    [Tooltip("In Degrees, best set to -90f deg")] [SerializeField] float rotateCartAround = -90f;
    [Tooltip("LayerMask Name of Carts")] [SerializeField] string cartLayerName = "Carts";
    [SerializeField] Transform playerBody;

    Camera mainCamera;
    bool grabButtonDown, releaseButtonDown;
    Vector3 screenCenter;
    Ray playerLookDirection;
    RaycastHit hitCart;
    int layerMask;
    bool isLookingAtCart;
    bool isHoldingCart;
    bool isFallenOver;
    bool isLookingAtHandlebars;
    bool liftingCart;
    Transform cartInHands;
    Rigidbody cartInHandsRigidBody;
    Transform cartLastLookedAt;
    FPSPlayerMovement playerBodyFPS;

    // Use this for initialization
    void Start () {
        mainCamera = FindObjectOfType<Camera>();
        screenCenter = new Vector3(0.5f, 0.5f, 0f);
        playerBodyFPS = playerBody.GetComponentInParent<FPSPlayerMovement>();
    }
	
	// Update is called once per frame
	void Update () {
        ProcessGrabCart();
	}

    void FixedUpdate() {
        HandleRaycasting();
        HandleCartControls();
        ApplyCartForces();
        ClampCartVelocity();
    }

    private void ProcessGrabCart() {
        grabButtonDown = CrossPlatformInputManager.GetButton("Fire1");
        releaseButtonDown = CrossPlatformInputManager.GetButton("Fire2");
    }

    private void HandleRaycasting() {
        playerLookDirection = mainCamera.ViewportPointToRay(screenCenter);
        layerMask = LayerMask.GetMask(cartLayerName);
        isLookingAtCart = Physics.Raycast(playerLookDirection, out hitCart, maxDistanceGrab, layerMask);
        Debug.DrawLine(playerLookDirection.origin, hitCart.point);

        if (isLookingAtCart) {
            cartLastLookedAt = hitCart.transform;
            if(!isHoldingCart)
                cartLastLookedAt.GetComponent<Cart>().ActivateOutline();
        } else if (cartLastLookedAt != null) {
            cartLastLookedAt.GetComponent<Cart>().DeactivateOutline();
        }
    }

    private void HandleCartControls() {
        if (cartLastLookedAt != null) {
            isFallenOver = (cartLastLookedAt.localEulerAngles.x >= 45 || cartLastLookedAt.localEulerAngles.x <= -45) || (cartLastLookedAt.localEulerAngles.z >= 45 || cartLastLookedAt.localEulerAngles.z <= -45);
            if (isLookingAtCart)
                isLookingAtHandlebars = hitCart.collider.CompareTag("Handlebar");
            else
                isLookingAtHandlebars = false;
        } else {
            isFallenOver = false;
            isLookingAtHandlebars = false;
        }

        if (isLookingAtCart) {
            if (grabButtonDown && !isHoldingCart) {
                HandleCartGrabbing();
            }
        }

        if (isHoldingCart && releaseButtonDown) {
            ReleaseCart();
        }
    }

    private void HandleCartGrabbing() {
        cartInHands = cartLastLookedAt;
        cartInHandsRigidBody = cartInHands.GetComponentInChildren<Rigidbody>();
        isHoldingCart = true;

        if ( !liftingCart && (isFallenOver || !isLookingAtHandlebars) ) {
            // Press R to rotate X and Z back to 0 deg
            PickUpCart();
        } else if (isLookingAtHandlebars) {
            GrabCart();
        }

        cartInHands.GetComponent<Cart>().DeactivateOutline();
    }

    private void PickUpCart() {
        print("Lifting Cart");
        liftingCart = true;
        cartInHands.parent = transform;
        AddCartMassToPlayer();
    }

    private void GrabCart() {
        print("Grabbing Cart");
        cartInHands.position = playerBody.position + (playerBody.forward * distanceWhenGrabbed);
        cartInHands.rotation = Quaternion.Euler(
            playerBody.eulerAngles.x,
            playerBody.eulerAngles.y + rotateCartAround,
            playerBody.eulerAngles.z
            );
    }

    private void ReleaseCart() {
        isHoldingCart = false;
        if (liftingCart) {
            liftingCart = false;
            RemoveCartMassFromPlayer();
        }
        
        cartInHands.parent = null;
        cartInHands = null;
        cartInHandsRigidBody = null;
        print("Releasing cart");
    }

    private void AddCartMassToPlayer() {
        Rigidbody playerRigidBody = playerBody.GetComponent<Rigidbody>();
        cartInHandsRigidBody = cartInHands.GetComponent<Rigidbody>();
        playerRigidBody.mass += cartInHandsRigidBody.mass;
    }

    private void RemoveCartMassFromPlayer() {
        Rigidbody playerRigidBody = playerBody.GetComponent<Rigidbody>();
        cartInHandsRigidBody = cartInHands.GetComponent<Rigidbody>();
        playerRigidBody.mass -= cartInHandsRigidBody.mass;
    }

    private void ApplyCartForces() {
        if(cartInHandsRigidBody != null) {
            Vector3 cartForceToApply = playerBodyFPS.GetForceToApply();
            cartInHandsRigidBody.AddRelativeForce(new Vector3(
                cartForceToApply.x,
                cartForceToApply.y,
                cartForceToApply.z
                ));
        }
    }

    private void ClampCartVelocity() {
        if (cartInHandsRigidBody != null) {
            float cartVelocityMagnitude = cartInHandsRigidBody.velocity.magnitude;

            if (cartVelocityMagnitude > playerBodyFPS.GetPlayerVelocityLimit()) {
                cartInHandsRigidBody.velocity = Vector3.ClampMagnitude(cartInHandsRigidBody.velocity, playerBodyFPS.GetPlayerVelocityLimit());
            }
        }
    }
}
