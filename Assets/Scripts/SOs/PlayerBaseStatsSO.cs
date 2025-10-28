using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBaseStatsSO", menuName = "ScriptableObjects/Other/PlayerBaseStatsSO")]
public class PlayerBaseStatsSO : ScriptableObject {
    public List<StatDefault> BaseStats;
}