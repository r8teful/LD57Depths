using UnityEngine;
using System.Collections;

public class TilePlant : ExteriorObject, ITileChangeReactor {

    public void OnTileChangedNearby(Vector3Int cellPosition, int newTileID) {
        if (newTileID == 0) {
            if(cellPosition == transform.position) {
                Destroy(gameObject);
            }
        }
    }
}