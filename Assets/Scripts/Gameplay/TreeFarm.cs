using UnityEngine;

public class TreeFarm : MonoBehaviour {
    public Sprite[] treestages;
    private int _currentTreeSprite;

    private void NextTreeSprite() {
        _currentTreeSprite++;
        GetComponent<SpriteRenderer>().sprite = treestages[_currentTreeSprite];
    }
}