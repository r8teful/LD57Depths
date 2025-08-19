using UnityEngine;

// Right now I see a background object as being able to spawn on different layers, and having a solid color 
public interface IBackgroundObject {
    void BeforeDestroy();
    public void Init(Color backgroundColor,int layerIndex,int orderInLayer);
}