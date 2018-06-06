using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
public class CartController : MonoBehaviour {

    [Header("Player Grab Settings")]
    [Tooltip("In Meters")] [SerializeField] float maxDistanceGrab = 1f;
    [Tooltip("In Meters")] [SerializeField] float distanceWhenGrabbed = 1f;
    [Tooltip("Offset for rotation when we grab the cart")] [SerializeField] float rotateCartAround = 180f;
    [Tooltip("How many degree ticks do we rotate the cart when we're lifting it.")] [SerializeField] float degreesToRotateCartWhenLifted = 20f;

    [Header("Other")]
    [Tooltip("LayerMask Name of Carts")] [SerializeField] string cartLayerName = "Carts";
    [SerializeField] Transform playerBody;

    Camera mainCamera;
    bool grabButtonDown, releaseButtonDown;
    Vector3 screenCenter;
    Ray playerLookRay;
    RaycastHit hitCart;
    int layerMask;
    bool isLookingAtCart;
    bool isHoldingCart;
    bool isRollable;
    bool isLookingAtHandlebars;
    bool liftingCart;
    bool grabbingCartHandlebars;
    float rotateCartAroundYAxis;
    Transform playerHands;
    Transform cartInHands;
    Rigidbody cartInHandsRigidBody;
    Cart cartInHandsCart;

    Transform cartLastLookedAt;
    Cart cartLastLookedAtCart;

    Rigidbody playerBodyRigidBody;
    FPSPlayerMovement playerBodyFPS;

    public bool IsLiftingCart() {
        return liftingCart;
    }

    public bool IsGrabbingCartHandlebars() {
        return grabbingCartHandlebars;
    }

    // Use this for initialization
    void Start () {
        mainCamera = FindObjectOfType<Camera>();
        screenCenter = new Vector3(0.5f, 0.5f, 0f);
        playerBodyFPS = playerBody.GetComponentInParent<FPSPlayerMovement>();
        playerBodyRigidBody = playerBody.GetComponentInChildren<Rigidbody>();
        playerHands = transform;
    }
	
	// Update is called once per frame
	void Update () {
        ProcessCartControls();
	}

    void FixedUpdate() {
        HandleRaycasting();
        HandleCartControls();
        ApplyCartForces();
        ClampCartVelocity();

        //This will print what the angles are reporting to be
        //print(cartLastLookedAt.eulerAngles);

        //This will tell you if you're looking at the handlebars or not
        print("Looking at Handlebars --> " + isLookingAtHandlebars);

        //This will tell you what the value of cartLastLookedAt is
        print("Cart Last Looked At -- > " + cartLastLookedAt);
    }

    private void ProcessCartControls() {
        grabButtonDown = CrossPlatformInputManager.GetButton("Fire1");
        releaseButtonDown = CrossPlatformInputManager.GetButton("Fire2");
        rotateCartAroundYAxis = CrossPlatformInputManager.GetAxisRaw("Mouse ScrollWheel");
    }

    private void HandleRaycasting() {
        playerLookRay = mainCamera.ViewportPointToRay(screenCenter);
        layerMask = LayerMask.GetMask(cartLayerName);
        isLookingAtCart = Physics.Raycast(playerLookRay, out hitCart, maxDistanceGrab, layerMask);
        Debug.DrawLine(playerLookRay.origin, hitCart.point);

        if (isLookingAtCart) {
            cartLastLookedAt = hitCart.transform;
            cartLastLookedAtCart = cartLastLookedAt.GetComponent<Cart>();
            if(!isHoldingCart)
                cartLastLookedAtCart.ActivateOutline();
        } else if (cartLastLookedAt != null) {
            cartLastLookedAtCart.DeactivateOutline();
        }
    }

    private void HandleCartControls() {
        if (cartLastLookedAt != null) {
            isRollable = cartLastLookedAtCart.IsRollable();
            if (isLookingAtCart)
                isLookingAtHandlebars = hitCart.collider.CompareTag("Handlebar");
        } else {
            isRollable = false;
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
        cartInHandsCart = cartInHands.GetComponentInChildren<Cart>();
        isHoldingCart = true;
        print(!liftingCart + " && " + (isRollable + " || " + !isLookingAtHandlebars));
        if ( !liftingCart && (isRollable || !isLookingAtHandlebars) ) {
            // Press R to rotate X and Z back to 0 deg
            PickUpCart();
        } else if (isLookingAtHandlebars) {
            GrabCartHandlebars();
        }

        cartLastLookedAtCart.DeactivateOutline();
    }

    private void PickUpCart() {
        print("Lifting Cart");
        liftingCart = true;
        cartInHands.parent = playerHands.transform;
        cartInHands.position = playerBody.transform.position + (playerBody.transform.forward * 1.5f);
        cartInHandsRigidBody.useGravity = false;
        cartInHandsRigidBody.isKinematic = true;
        AddCartMassToPlayer();
    }

    private void GrabCartHandlebars() {
        print("Grabbing Cart");
        grabbingCartHandlebars = true;
        cartInHands.parent = transform;
        cartInHands.position = playerBody.position + (playerBody.forward * distanceWhenGrabbed);
        cartInHands.rotation = Quaternion.Euler(
            playerBody.eulerAngles.x,
            playerBody.eulerAngles.y + rotateCartAround,
            playerBody.eulerAngles.z
        );
        AddCartMassToPlayer();
    }

    private void ReleaseCart() {
        isHoldingCart = false;

        if (liftingCart || grabbingCartHandlebars) {
            RemoveCartMassFromPlayer();
        }

        liftingCart = false;
        grabbingCartHandlebars = false;

        cartInHandsRigidBody.useGravity = true;
        cartInHandsRigidBody.isKinematic = false;
        cartInHands.parent = null;
        cartInHands = null;
        cartInHandsRigidBody = null;
        cartInHandsCart = null;

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
            if (liftingCart) {
                LiftCart();
            } else if (grabbingCartHandlebars) {
                PushCart();
            }
        }
    }

    private void PushCart() {
        //Vector3 playerPushForce = -playerBodyFPS.GetForceToApply();
        //cartInHandsCart.CalculateTorqueAndAngleOnWheels(playerPushForce);
        Vector3 oldCartPostion = cartInHands.position;
        Vector3 newCartPosition = (playerBodyRigidBody.position + (playerLookRay.direction * 2.0f));
        Vector3 positionDifference = (newCartPosition - oldCartPostion) * Time.deltaTime;
        newCartPosition += positionDifference;

        Vector3 newCartRotation = playerBody.rotation.eulerAngles;
        newCartPosition.y = cartInHands.position.y;
        newCartRotation.y -= 180f;

        cartInHandsRigidBody.MovePosition(newCartPosition);
        cartInHandsRigidBody.MoveRotation(Quaternion.Euler(newCartRotation));
    }

    private void LiftCart() {
        Vector3 newPosition = (playerBodyRigidBody.position + (playerLookRay.direction * 1.5f));
        cartInHandsRigidBody.MovePosition(newPosition);

        cartInHandsRigidBody.angularVelocity = Vector3.zero;
        cartInHandsRigidBody.velocity = Vector3.zero;

        cartInHands.eulerAngles = new Vector3(0f, cartInHands.eulerAngles.y, 0f);
        cartInHands.Rotate(0f, rotateCartAroundYAxis * degreesToRotateCartWhenLifted, 0f);
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
