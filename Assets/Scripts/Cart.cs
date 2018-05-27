using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cart : MonoBehaviour {

    [SerializeField] Material outline;

    public Cart cartInFront, cartBehind;

    private bool outlined;

	// Use this for initialization
	void Start () {
        outlined = false;
	}

    public bool hasOutline() {
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
}
