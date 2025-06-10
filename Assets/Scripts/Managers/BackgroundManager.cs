using System.Collections.Generic;
using UnityEngine;

public class BackgroundManager : MonoBehaviour {
    public GameObject trenchContainer;
    public SpriteRenderer _blackSprite;
    private List<BackgroundSprite> _trenchSprites= new List<BackgroundSprite>();
    internal void Init(WorldGenSettingSO worldGenSettings) {
        foreach(var t in trenchContainer.GetComponentsInChildren<BackgroundSprite>()) {
            t.SetTrenchSettings(worldGenSettings);
        }
        // Centre
        trenchContainer.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, 0);
        trenchContainer.transform.SetParent(Camera.main.transform);
        _blackSprite.enabled = false;
    }

    public void SetInteriorBackground(bool isInterior) {
        if (isInterior) {
            _blackSprite.enabled = true;
        } else {
            _blackSprite.enabled = false;
        }
    }
}