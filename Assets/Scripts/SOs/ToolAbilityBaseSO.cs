using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ToolAbilitySO", menuName = "ScriptableObjects/ToolAbilitySO", order = 7)]
public class ToolAbilityBaseSO : ScriptableObject {
    public List<StatModifier> Modifiers;
    public float Duration;
}