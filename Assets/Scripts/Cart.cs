using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// <summary>
// Apply the force calculated in ProcessTranslation to the player.
// </summary>
public class Cart : MonoBehaviour {
    [Header("Outline Settings")]
    [SerializeField] Material outline;

    [Header("Wheel Settings")]
    [Tooltip("How many wheels need to be off the ground to be considered non-rollable")] [SerializeField] int wheelsToBeNotRollable = 2;
    [Tooltip("How much we multiply the force by. Recommended 4")] [SerializeField] float forceMultiplier = 4.0f;
    [Tooltip("Important, Front Wheels must be the first two elements!")][SerializeField] List<WheelCollider> cartWheels;
    [Tooltip("In Degrees, recommended setting minimum to 0 degrees.")] [SerializeField] float minTurnAngle = 0f, maxTurnAngle = 30f;

    [Header("Other")]
    public Cart cartInFront;
    public Cart cartBehind;
    [Tooltip("Do not change unless you know what you're doing.")] [SerializeField] GameObject playerBody;

    private Rigidbody thisRigidBody;
    private Collider thisCollider;
    private bool outlined;
    private bool isRollable;

    // <summary>
    // Initializes values for variables.
    // </summary>
    void Start () {
        outlined = false;
        isRollable = false;
        thisRigidBody = GetComponent<Rigidbody>();
        thisCollider = GetComponent<Collider>();
	}

    // <summary>
    // Checks if the cart is rollable.
    // </summary>
    void FixedUpdate() {
        CheckIfRollable();
    }

    // <summary>
    // Returns if the cart is rollable and 
    // can be pushed by the player.
    // </summary>
    public bool IsRollable() {
        return isRollable;
    }

    // <summary>
    // Returns if the cart is currently outlined.
    // </summary>
    public bool HasOutline() {
        return outlined;
    }

    // <summary>
    // Activates the outline shader of the cart.
    // </summary>
    public void ActivateOutline() {
        if (!outlined) {
            outlined = true;
            AddOutlineToCart();
        }
    }

    // <summary>
    // Deactivates the outline shader of the cart.
    // </summary>
    public void DeactivateOutline() {
        if (outlined) {
            outlined = false;
            RemoveOutlineFromCart();
        }
    }

    // <summary>
    // Calculates a force to apply on the cart wheels given a force from the
    // player and calls a function to apply the force on each wheel.
    // </summary>
    public void CalculateTorqueAndAngleOnWheels(Vector3 forceFromPlayer) {
        // Horizontal is X Axis - Turning
        // Forward/Back is Z Axis - Pushing
        float pushingForce = forceFromPlayer.z;
        float torqueOnEachWheel = pushingForce * forceMultiplier;
        float turningForce = forceFromPlayer.x;
        float turningRadians = Mathf.Atan(turningForce / pushingForce);
        float turningDegrees = Mathf.Rad2Deg * turningRadians;
        float turningDegreesFixed = Mathf.Clamp(turningDegrees, minTurnAngle, maxTurnAngle);

        bool turnAngleIsNegative = (turningForce * pushingForce) < 0;
        if (turnAngleIsNegative) {
            turningDegreesFixed *= -1;
        }

        ApplyTorqueAndAngleOnWheels(torqueOnEachWheel, turningDegreesFixed);
    }

    // <summary>
    // Applies the force calculated on each wheel and applies it.
    // </summary>
    private void ApplyTorqueAndAngleOnWheels(float torqueOnEachWheel, float turnAngle) {
        foreach (WheelCollider cartWheel in cartWheels) {
            cartWheel.motorTorque = torqueOnEachWheel;
            cartWheel.steerAngle = turnAngle;
        }
    }

    // <summary>
    // Checks the cart if it's rollable by checking how many
    // wheels are touching the ground.
    // </summary>
    private void CheckIfRollable() {
        int wheelsNotTouchingGround = 0;

        foreach (WheelCollider cartWheel in cartWheels) {
            if (!cartWheel.isGrounded)
                wheelsNotTouchingGround++;
        }

        isRollable = (wheelsNotTouchingGround >= wheelsToBeNotRollable);
    }

    // <summary>
    // Goes through each child on the cart and adds the outline material
    // to the children.
    // </summary>
    private void AddOutlineToCart() {
        for (int childIndex = 0; childIndex < transform.childCount; childIndex++) {
            Transform cartPiece = transform.GetChild(childIndex);
            MeshRenderer cartPieceRenderer = cartPiece.GetComponent<MeshRenderer>();
            Material[] matsOfCartPiece = cartPieceRenderer.materials;
            List<Material> matsOfCartPieceList = new List<Material>(matsOfCartPiece);
            matsOfCartPieceList.Add(outline);
            cartPieceRenderer.materials = matsOfCartPieceList.ToArray();
        }
    }

    // <summary>
    // Goes through each child on the cart and removes the outline material
    // from the children.
    // </summary>
    private void RemoveOutlineFromCart() {
        for (int childIndex = 0; childIndex < transform.childCount; childIndex++) {
            Transform cartPiece = transform.GetChild(childIndex);
            MeshRenderer cartPieceRenderer = cartPiece.GetComponent<MeshRenderer>();
            Material[] matsOfCartPiece = cartPieceRenderer.materials;
            List<Material> matsOfCartPieceList = new List<Material>(matsOfCartPiece);

            for (int index = 0; index < matsOfCartPieceList.Count; index++) {
                Material matInCartPiece = matsOfCartPieceList[index];
                if (matInCartPiece.shader.Equals(outline.shader))
                    matsOfCartPieceList.RemoveAt(index);
            }

            cartPieceRenderer.materials = matsOfCartPieceList.ToArray();
        }
    }
}
