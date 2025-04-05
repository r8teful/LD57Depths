using UnityEngine;
using System.Text;

public class TextMeshWrapper : MonoBehaviour {
    [SerializeField] private TextMesh textMesh;  // Reference to TextMesh component
    [SerializeField] private int maxCharsPerLine = 20; // Max characters before wrapping

    private void Awake() {
        if (textMesh == null) {
            textMesh = GetComponent<TextMesh>();
        }
    }

    public void SetText(string inputText) {
        textMesh.text = WrapText(inputText, maxCharsPerLine);
    }

    private string WrapText(string text, int maxLineLength) {
        if (text.Length <= maxLineLength)
            return text; // No need to wrap if short enough

        StringBuilder wrappedText = new StringBuilder();
        string[] words = text.Split(' '); // Split words by spaces
        int currentLineLength = 0;

        foreach (string word in words) {
            // If adding this word exceeds line length, insert newline first
            if (currentLineLength + word.Length > maxLineLength) {
                wrappedText.Append("\n");
                currentLineLength = 0;
            }

            // Append word and space
            wrappedText.Append(word + " ");
            currentLineLength += word.Length + 1; // +1 for space
        }

        return wrappedText.ToString().TrimEnd(); // Remove trailing space
    }
}
