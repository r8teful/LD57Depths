using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeTierSO", menuName = "ScriptableObjects/Upgrades/UpgradeTierSO")]
public class UpgradeTierSO : ScriptableObject {
    public List<ItemData> Items; 
}