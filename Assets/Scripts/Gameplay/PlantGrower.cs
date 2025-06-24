using UnityEngine;

public class PlantGrower : MonoBehaviour {

    public Sprite[] treestages;
    private int _currentTreeSprite;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) {
            NextTreeSprite();
        }
    }
    private void NextTreeSprite() {
        _currentTreeSprite++;
        GetComponent<SpriteRenderer>().sprite = treestages[_currentTreeSprite];
    }
}
