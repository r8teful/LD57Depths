using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class CustomUI {
    public static Dictionary<Vector3, char> GetTextLetterPositions(TextMeshProUGUI text) {
        var letterPositions = new Dictionary<Vector3, char>();
        if (text != null) {
            TMP_TextInfo textInfo = text.textInfo;
            for (int i = 0; i < textInfo.characterCount; i++) {
               
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                // Get the position of the character
                if (!char.IsWhiteSpace(text.text[charInfo.index])) { 

                    float centerX = (charInfo.bottomLeft.x + charInfo.topRight.x) / 2f;
                float centerY = (charInfo.bottomLeft.y + charInfo.topRight.y) / 2f;

                Vector3 worldPosition = text.transform.TransformPoint(new Vector3(centerX, centerY, 0f));

                // Add the position and character to the dictionary
                letterPositions.Add(worldPosition, text.text[i]);
                }
                
            }
        }
        return letterPositions;
    }
}