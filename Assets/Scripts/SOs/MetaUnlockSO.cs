using UnityEngine;

[CreateAssetMenu(fileName = "MetaUnlockSO", menuName = "ScriptableObjects/Other/MetaUnlockSO", order = 8)]
public class MetaUnlockSO : ScriptableObject, IIdentifiable {
    [SerializeField] private ushort _unlockID; 
    public ushort ID => _unlockID;

    public string displayID;

    public MetaUnlockStat requiredStat;
    public float targetValue; 
}