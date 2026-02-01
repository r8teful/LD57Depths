using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "StructureSO", menuName = "ScriptableObjects/WorldGen/StructureSO")]
public class StructureSO : ScriptableObject, IIdentifiable {
    [SerializeField] private ushort _structureID; 
    public ushort ID => _structureID;
    
    public bool tileIsOreLayer; // treat tiles as being on the ore map, it will take the base layer from surroundings

    public GameObject sourcePrefab; // We'll read the tilemap from here so we can get the tiles and size

    // Used by worldgen
    [Header("Generated from source (don't edit)")] 
    public Vector2Int Size;
    public List<TileBase> tiles;
}