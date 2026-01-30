using System.Collections;
using UnityEngine;

public class Chest : MonoBehaviour {
    public void Init(StructurePlacementResult data) {
        transform.position = new(data.bottomLeftAnchor.x, data.bottomLeftAnchor.y, 0);

    }

}