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
    Cart cartInHands;
    Transform cartLastLookedAt;

    // Use this for initialization
    void Start () {
        mainCamera = FindObjectOfType<Camera>();
        screenCenter = new Vector3(0.5f, 0.5f, 0f);
    }
	
	// Update is called once per frame
	void Update () {
        ProcessGrabCart();
	}

    private void ProcessGrabCart() {
        grabButtonDown = CrossPlatformInputManager.GetButton("Fire1");
        releaseButtonDown = CrossPlatformInputManager.GetButton("Fire2");

        HandleRaycasting();
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

        HandleCartControls();
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
            if (grabButtonDown) {
                cartInHands = cartLastLookedAt.GetComponent<Cart>();
                isHoldingCart = true;

                if (isFallenOver) {
                    // Press R to rotate X and Z back to 0 deg
                } else if (isLookingAtHandlebars) {
                    // Child the object for now
                    if (cartLastLookedAt.parent == null) {
                        cartLastLookedAt.parent = playerBody;
                        print("Grabbing Cart");
                        cartLastLookedAt.position = playerBody.position + (playerBody.forward * distanceWhenGrabbed);
                        cartLastLookedAt.localRotation = Quaternion.Euler(new Vector3(0f, rotateCartAround, 0f));
                    }
                }

                cartLastLookedAt.GetComponent<Cart>().DeactivateOutline();
            }
            
        }

        if (releaseButtonDown) {
            isHoldingCart = false;
            if (cartLastLookedAt.parent != null) {
                cartLastLookedAt.parent = null;
                print("Releasing cart");
            }
        }
    }
}
