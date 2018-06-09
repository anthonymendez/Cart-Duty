using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    
	// Use this for initialization
	void Start () {
        outlined = false;
        isRollable = false;
        thisRigidBody = GetComponent<Rigidbody>();
        thisCollider = GetComponent<Collider>();
	}

    void FixedUpdate() {
        CheckIfRollable();
    }
    
    public bool IsRollable() {
        return isRollable;
    }

    public bool HasOutline() {
        return outlined;
    }

    public void ActivateOutline() {
        if (!outlined) {
            outlined = true;
            AddOutlineToCart();
        }
    }

    public void DeactivateOutline() {
        if (outlined) {
            outlined = false;
            RemoveOutlineFromCart();
        }
    }

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

    private void ApplyTorqueAndAngleOnWheels(float torqueOnEachWheel, float turnAngle) {
        foreach (WheelCollider cartWheel in cartWheels) {
            cartWheel.motorTorque = torqueOnEachWheel;
            cartWheel.steerAngle = turnAngle;
        }
    }

    private void CheckIfRollable() {
        int wheelsNotTouchingGround = 0;

        foreach (WheelCollider cartWheel in cartWheels) {
            if (!cartWheel.isGrounded)
                wheelsNotTouchingGround++;
        }

        isRollable = (wheelsNotTouchingGround >= wheelsToBeNotRollable);
    }

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

    // todo figure out why this isn't working
    void OnCollisionEnter(Collision otherCollision) {
        GameObject colliderGameObject = otherCollision.gameObject;
        bool colliderIsPlayerBody = colliderGameObject.CompareTag("PlayerBody");
        bool isKinematic = thisRigidBody.isKinematic;

        if (colliderIsPlayerBody && isKinematic) {
            Physics.IgnoreCollision(otherCollision.collider, thisCollider);
            print("Ignoring collision");
        }
    }

    void OnTriggerEnter(Collider otherCollider) {
        GameObject colliderGameObject = otherCollider.gameObject;
        bool colliderIsPlayerBody = colliderGameObject.CompareTag("PlayerBody");
        bool isKinematic = thisRigidBody.isKinematic;

        if (colliderIsPlayerBody && isKinematic) {
            Physics.IgnoreCollision(otherCollider, thisCollider);
            print("Ignoring collision");
        }
    }
}
