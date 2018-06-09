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

    [Header("Cart Pull-Guide Settings")]
    [Tooltip("Do we use Mathf.Lerp to determine the dampening towards the center.")] [SerializeField] bool useLerpDampeningForMovement = true;
    [Tooltip("Only works when useLerpDampeningForMovement is disabled. How much we effect the cart's pull force towards the push guide.")]
        [SerializeField]
        float mDampeningFactor = 1f;
    [Tooltip("How much we effect the cart's pull force towards the push guide.")]
        [SerializeField]
        float mDampeningDivisor = 1f;

    [Header("Cart Rotation-Guide Settings")]
    [Tooltip("Do we use Mathf.Lerp to determine the dampening for rotation back to the player's rigidbody.")] [SerializeField] bool useLerpDampeningForRotation = true;
    [Tooltip("Only works when useLerpDampeningForRotation is disabled. How much we effect the cart's torque towards the player's rigidbody's rotation.")]
        [SerializeField]
        float rDampeningDivisor = 1f;
    [Tooltip("How much we effect the cart's torque towards the player's rigidbody's rotation.")]
        [SerializeField]
        float rDampeningFactor = 1f;

    [Header("Throwing Settings")]
    [Tooltip("In Newtons")] [SerializeField] float throwingForce = 500f;

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
    Transform liftGuide, pushGuide;

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
        liftGuide = GetComponentsInChildren<Transform>()[1];
        pushGuide = GetComponentsInChildren<Transform>()[2];
    }
	
	// Update is called once per frame
	void Update () {
        ProcessCartControls();
        MoveLiftGuide();
	}

    void FixedUpdate() {
        HandleRaycasting();
        HandleCartControls();
        ApplyCartForces();
        ClampCartVelocity();
    }

    private void ProcessCartControls() {
        grabButtonDown = CrossPlatformInputManager.GetButton("Fire1");
        releaseButtonDown = CrossPlatformInputManager.GetButton("Fire2");
        rotateCartAroundYAxis = CrossPlatformInputManager.GetAxisRaw("Mouse ScrollWheel");
    }

    private void MoveLiftGuide() {
        Vector3 camPosition = mainCamera.transform.position;
        Vector3 direction = playerLookRay.direction;
        Vector3 newPosition = camPosition + direction;
        liftGuide.position = newPosition;
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
            AddCartMassToPlayer();
        } else if (isLookingAtHandlebars) {
            GrabCartHandlebars();
            AddCartMassToPlayer();
        }

        cartLastLookedAtCart.DeactivateOutline();
    }

    private void PickUpCart() {
        print("Lifting Cart");
        liftingCart = true;
        cartInHandsRigidBody.useGravity = false;
        cartInHandsRigidBody.detectCollisions = true;
        cartInHandsRigidBody.isKinematic = false;
        cartInHands.parent = liftGuide;
        cartInHands.position = liftGuide.position;
        //cartInHands.position = playerBody.transform.position + (playerBody.transform.forward * 1.5f);
    }

    private void GrabCartHandlebars() {
        print("Grabbing Cart");
        grabbingCartHandlebars = true;
        cartInHands.parent = pushGuide;
        cartInHands.position = playerBody.position + (playerBody.forward * distanceWhenGrabbed);
        cartInHands.rotation = Quaternion.Euler(
            playerBody.eulerAngles.x,
            playerBody.eulerAngles.y + rotateCartAround,
            playerBody.eulerAngles.z
        );
    }

    private void ReleaseCart() {
        isHoldingCart = false;

        if (liftingCart || grabbingCartHandlebars) {
            RemoveCartMassFromPlayer();
        }

        liftingCart = false;
        grabbingCartHandlebars = false;
        cartInHandsRigidBody.useGravity = true;
        cartInHandsRigidBody.detectCollisions = true;
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
        if(cartInHandsRigidBody != null /*&& isRollable*/) {
            if (liftingCart) {
                LiftCart();
            } else if (grabbingCartHandlebars) {
                PushCart();
            }
        }
    }
    
    private void PushCart() {

        /* Cart Movement */
        Vector3 pushGuidePosition = pushGuide.position;
        Vector3 cartPosition = cartInHands.position;
        Vector3 cartToPushGuideHeading = pushGuidePosition - cartPosition;
        cartToPushGuideHeading.y = 0f;
        float lerpDampening = Mathf.Lerp(cartPosition.magnitude,
                                            pushGuidePosition.magnitude,
                                            cartInHandsRigidBody.velocity.magnitude * Time.fixedDeltaTime);
        Vector3 forceOnCart = cartToPushGuideHeading / mDampeningDivisor;
        if (useLerpDampeningForMovement) {
            forceOnCart *= lerpDampening;
        } else {
            forceOnCart *= mDampeningFactor;
        }

        bool isForceOnCartValid = (
            forceOnCart.x != Mathf.Infinity && forceOnCart.x != Mathf.NegativeInfinity &&
            forceOnCart.y != Mathf.Infinity && forceOnCart.y != Mathf.NegativeInfinity &&
            forceOnCart.z != Mathf.Infinity && forceOnCart.z != Mathf.NegativeInfinity
            );
        if (isForceOnCartValid) {
            cartInHandsRigidBody.AddForce(forceOnCart, ForceMode.VelocityChange);
            Debug.Log("forceOnCart is valid");
        } else {
            Debug.LogError("forceOnCart has Inf/NegInf value: " + forceOnCart);
            Debug.LogError("dampeningFactor: " + mDampeningFactor);
            Debug.LogError("dampeningDivisor: " + mDampeningDivisor);
        }

        /* Cart Rotation */

        //todo remove and replace with something better
        //Vector3 directionToGo = playerBody.eulerAngles;
        //directionToGo.y -= 180f;
        //cartInHandsRigidBody.MoveRotation(Quaternion.Euler(directionToGo));

        /*  Cool flippy doo thing with the cart
         * Quaternion playerBodyEulerAngles = playerBody.rotation;
        Quaternion cartEulerAngles = cartInHands.rotation;
        Vector3 cartToPlayerBodyEulerAngles = Quaternion.Euler(playerBodyEulerAngles.eulerAngles - cartEulerAngles.eulerAngles).eulerAngles;
        lerpDampening = Mathf.Lerp(cartEulerAngles.eulerAngles.magnitude, playerBodyEulerAngles.eulerAngles.magnitude, cartToPlayerBodyEulerAngles.magnitude * Time.fixedDeltaTime);
        Vector3 rotationOnCart = cartToPlayerBodyEulerAngles / dampeningRotatingDivisor;
        if (useLerpDampeningForRotation) {
            rotationOnCart *= lerpDampening;
        } else {
            rotationOnCart *= dampeningRotatingFactor;
        }
        */
    }

    private void LiftCart() {
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
