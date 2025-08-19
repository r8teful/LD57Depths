using UnityEngine;

[CreateAssetMenu(fileName = "GrowthSO", menuName = "ScriptableObjects/Other/GrowthSO")]
public class GrowthSO : ScriptableObject {
    [Tooltip("The time in seconds it takes to advance through each stage")]
    public int[] StageSeconds;
    public Sprite[] StageSprites;
    public int TotalStages() {
        if (StageSprites.Length == 0) {
            Debug.LogError("StageSprites shoudl be longer than 1!");
            return 1;
        }
        return StageSprites.Length;
    }
}
