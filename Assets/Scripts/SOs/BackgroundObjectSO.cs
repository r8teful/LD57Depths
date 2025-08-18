using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BackgroundObjectData", menuName = "ScriptableObjects/Other/BackgroundObjectData")]
public class BackgroundObjectSO : ScriptableObject {
    public GameObject prefab;
    [Range(0f, 1f)]
    public float spawnLikelihood;
    public List<BiomeType> biomes;
    public int maxInstances;
}