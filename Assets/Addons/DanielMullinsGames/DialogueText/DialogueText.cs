using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class DialogueText : MonoBehaviour
{
    public event System.Action<float> Shake;
    public event System.Action<string> Animation;
    public static event System.Action<bool> IsStopped; // just static so I can use it in UISpaceHint
    public event System.Action CompletedLine;
    public event System.Action ForceEndLine;
    public event System.Action<Emotion> EmotionChange;
    public event System.Action<string, int> DisplayCharacter;

    public bool PlayingMessage { get; private set; }
    public bool EndOfVisibleCharacters { get; private set; }
    public bool SpeedUpCharacters { get; set; }
    public float VoicePitchAdjust { get; private set; }
    private bool Active => uiText != null && uiText.gameObject.activeInHierarchy;

    [SerializeField]
    private TextMeshProUGUI uiText = default;

    private List<TextMeshProUGUI> spawnedLetters = new List<TextMeshProUGUI>();

    private float characterFrequency;
    private Color currentColor = Color.black;
    private bool skipToEnd;
    private float _prevTextPos;
    private const float DEFAULT_FREQUENCY = 7.5f;
#if UNITY_EDITOR
    //private const float DIALOGUE_SPEED = 0.1f;
    [SerializeField]
    public float DIALOGUE_SPEED = 0.1f;
#else
    private const float DIALOGUE_SPEED = 0.8f;
#endif

    [SerializeField]
    private Color defaultColor = Color.black;

    protected void ManagedInitialize()
    {
        currentColor = defaultColor;
    }

    public void PlayMessage(string message)
    {
        ResetToDefaults();

        if (Active)
        {
            StartCoroutine(PlayMessageSequence(message));
            
        }
    }

    public void Clear()
    {
        StopAllCoroutines();
        PlayingMessage = false;
        HideAllLetters();
    }

    public void SkipToEnd()
    {
        skipToEnd = true;
    }

    private void ResetToDefaults()
    {
        VoicePitchAdjust = 0f;
        currentColor = defaultColor;
        characterFrequency = DEFAULT_FREQUENCY;
        skipToEnd = false;

        EmotionChange?.Invoke(Emotion.Neutral);
    }

    private void InstantiateLetters(string unformattedMessage)
    {
        spawnedLetters.ForEach(x => Destroy(x.gameObject));
        spawnedLetters.Clear();

        uiText.text = unformattedMessage;
        Canvas.ForceUpdateCanvases();

        foreach (var pair in CustomUI.GetTextLetterPositions(uiText))
        {
            spawnedLetters.Add(SpawnLetter(pair.Value, pair.Key));
        }
        HideAllLetters();

        uiText.text = "";
    }

    private TextMeshProUGUI SpawnLetter(char c, Vector3 pos) {

        //pos.x = Mathf.Round(pos.x * 100) / 100;

        //_prevTextPos = pos.y;

        var obj = new GameObject(c.ToString());
        obj.transform.SetParent(uiText.transform.parent);
        obj.transform.position = pos;
        obj.transform.localScale = Vector3.one;
        obj.layer = uiText.gameObject.layer;

        var letter = obj.AddComponent<TextMeshProUGUI>();
        letter.text = c.ToString();
        letter.font = uiText.font;
        letter.fontSize = uiText.fontSize;
        letter.lineSpacing = uiText.lineSpacing;
        letter.alignment = TextAlignmentOptions.Midline;
        letter.raycastTarget = false;
        letter.color = currentColor;

        return letter;
    }

    private void RevealLettersUpToIndex(int index)
    {
        for (int i = 0; i < spawnedLetters.Count; i++)
        {
            spawnedLetters[i].gameObject.SetActive(i <= index);
        }
    }

    private void HideAllLetters()
    {
        RevealLettersUpToIndex(-1);
    }

    private IEnumerator PlayMessageSequence(string message)
    {
        PlayingMessage = true;
        EndOfVisibleCharacters = false;

        string unformattedMessage = DialogueParser.GetUnformattedMessage(message);
        string shownUnformatted = "";

        InstantiateLetters(unformattedMessage);

        int parsingIndex = 0;
        int letterIndex = 0;
        while (message.Length > parsingIndex)
        {
            string dialogueCode = DialogueParser.GetDialogueCode(message, parsingIndex);
            if (!string.IsNullOrEmpty(dialogueCode))
            {
                parsingIndex += dialogueCode.Length;
                yield return ConsumeCode(dialogueCode);
            }
            else
            {
                if (message[parsingIndex] != ' ')
                {
                    DisplayCharacter?.Invoke(message, parsingIndex);

                    RevealLettersUpToIndex(letterIndex);
                    letterIndex++;
                }
                
                shownUnformatted += message[parsingIndex];
                parsingIndex++;
                
                if (unformattedMessage == shownUnformatted)
                {
                    EndOfVisibleCharacters = true;
                }
                var speed = 1f;
                if (SpeedUpCharacters) speed = 0.5f;
                float adjustedFrequency = Mathf.Clamp(characterFrequency * 0.01f * DIALOGUE_SPEED * speed, 0.01f, 2f);
                float waitTimer = 0f;
                while (!skipToEnd && waitTimer < adjustedFrequency)
                {
                    waitTimer += Time.deltaTime;
                    yield return new WaitForEndOfFrame();
                }
            }
        }
        CompletedLine?.Invoke();
        PlayingMessage = false;
    }

    private IEnumerator ConsumeCode(string code)
    {
        if (code.StartsWith("[end"))
        {
            ForceEndLine?.Invoke();
        }
        else if (code.StartsWith("[e"))
        {
            string emotionValue = DialogueParser.GetStringValue(code, "e");
            var emotion = (Emotion)System.Enum.Parse(typeof(Emotion), emotionValue);
            EmotionChange?.Invoke(emotion);
        }
        else if (code.StartsWith("[w"))
        {

            var speed = 1f;
            if (SpeedUpCharacters) speed = 0.5f;
            float waitTimer = 0f;
            float waitLength = DialogueParser.GetFloatValue(code, "w") * DIALOGUE_SPEED * speed;

            while (!skipToEnd && waitTimer < waitLength)
            {
                waitTimer += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
        }
        else if (code.StartsWith("[t"))
        {
            characterFrequency = DialogueParser.GetFloatValue(code, "t");
            if (characterFrequency == 0f)
            {
                characterFrequency = DEFAULT_FREQUENCY;
            }
        }
        else if (code.StartsWith("[c"))
        {
            currentColor = DialogueParser.GetColorFromCode(code, defaultColor);
        }
        else if (code.StartsWith("[shake:"))
        {
            float intensity = DialogueParser.GetFloatValue(code, "shake");
            Shake?.Invoke(intensity);
        }
        else if (code.StartsWith("[anim:"))
        {
            string trigger = DialogueParser.GetStringValue(code, "anim");
            Animation?.Invoke(trigger);
        }
        else if (code.StartsWith("[p"))
        {
            VoicePitchAdjust = DialogueParser.GetFloatValue(code, "p");
        }
        else if (code.StartsWith("[stop]")) {
            IsStopped?.Invoke(true);
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Mouse1));
            IsStopped?.Invoke(false);
        }
    }
}
