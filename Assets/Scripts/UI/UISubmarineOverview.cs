using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISubmarineOverview : MonoBehaviour {
    [InfoBox("Order by zone index!")]
    [SerializeField] private List<Image> _images;
    public void SetIndex(int index) {
        Debug.Log(index);
        var incIndex = index-1;
        if(incIndex > _images.Count) {
            Debug.LogError("Index out of range!");
            return;
        }
        for (int i = 0; i < _images.Count; i++) {
            if (i == incIndex) {
                _images[i].gameObject.SetActive(true);
            }else {
                _images[i].gameObject.SetActive(false);
            }
        }
    }
}