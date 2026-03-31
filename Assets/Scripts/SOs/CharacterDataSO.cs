using UnityEngine;

[CreateAssetMenu(fileName = "CharacterInfoSO", menuName = "ScriptableObjects/Other/CharacterInfoSO")]
public class CharacterDataSO : ScriptableObject {
    public string characterNameKey;
    public string characterDescriptionKey;
    public string characterToolKey;
    public Sprite characterSprite;
    public MetaUnlockSO unlockSO;
}