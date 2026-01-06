using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class CosmeticData {
    public CosmeticCategory Category;
    public Sprite Sprite; // This is a bit more complicated because cosmatics will have more than just one sprite, like animations,
}
public enum CosmeticCategory {
    Head,
    Body,
    Feet,
    Arms,
    Back,
    Holding
}