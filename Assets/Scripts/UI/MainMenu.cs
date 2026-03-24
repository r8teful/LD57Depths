using DG.Tweening;
using HierarchyDecorator;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ButtonMenuVisual;

public class MainMenu : MonoBehaviour {
    [FoldoutGroup("Buttons")]
    [SerializeField] private  Button _buttonPlay;
    [FoldoutGroup("Buttons")]
    [SerializeField] private  Button _buttonPlayBack;
    [FoldoutGroup("Buttons")]
    [SerializeField] private  Button _buttonCharacterBack;
    [FoldoutGroup("Buttons")]
    [SerializeField] private  Button _buttonNewGame;
    [FoldoutGroup("Buttons")]
    [SerializeField] private  Button _buttonContinueTab;
    [FoldoutGroup("Buttons")]
    [SerializeField] private  Button _buttonContinueGame;
    [FoldoutGroup("Buttons")]
    [SerializeField] private  Button _buttonExit;
    [FoldoutGroup("Buttons")]
    [SerializeField] private  Button _buttonChallenge;
    [FoldoutGroup("Buttons")]
    [SerializeField] private  Button _buttonSettings;
    [FoldoutGroup("Buttons")]
    [SerializeField] private  Button _buttonLanguage;
    [FoldoutGroup("Buttons")]
    [SerializeField] private  Button _buttonLanguageBack;
    [SerializeField] private ButtonMenuVisual _buttonContinueTabVisual;
    [SerializeField] private UISettings _settings; 
    [SerializeField] private Transform _cameraTrans; 
    [SerializeField] private Transform _logoTrans;
    [FoldoutGroup("Containers")]
    [SerializeField] private GameObject _containerStartGame;
    [FoldoutGroup("Containers")]
    [SerializeField] private GameObject _containerLanguage;
    [FoldoutGroup("Containers")]
    [SerializeField] private GameObject _containerChooseCharacter;
    [FoldoutGroup("Containers")]
    [SerializeField] private GameObject _containerMainPage;

    [SerializeField] private TMP_InputField _seedField;
    [FoldoutGroup("MenuCameraPositions")]
    [SerializeField] private Transform _mainPageCameraPos;
    [FoldoutGroup("MenuCameraPositions")]    
    [SerializeField] private Transform _settingsCameraPos;
    [FoldoutGroup("MenuCameraPositions")]
    [SerializeField] private Transform _playGameCameraPos;

    private void OnEnable() {
        _buttonPlay.onClick.AddListener(OnPlayClicked);
        _buttonCharacterBack.onClick.AddListener(OnCharacterBack);
        _buttonPlayBack.onClick.AddListener(OnPlayBackClicked);
        _buttonSettings.onClick.AddListener(OnSettingsClicked);
        _buttonNewGame.onClick.AddListener(OnStartNewGameClicked);
        _buttonContinueGame.onClick.AddListener(OnContinueGameClicked);
        _buttonLanguage.onClick.AddListener(OnLanguageClicked);
        _buttonLanguageBack.onClick.AddListener(OnLanguageBackClicked);
#if !UNITY_WEBGL
        _buttonExit.onClick.AddListener(OnExitClicked);
#else
        _buttonExit.gameObject.SetActive(false);

#endif
    }

    private void OnDisable() {
        _buttonPlay.onClick.RemoveListener(OnPlayClicked);
        _buttonCharacterBack.onClick.RemoveListener(OnCharacterBack);
        _buttonPlayBack.onClick.RemoveListener(OnPlayBackClicked);
        _buttonSettings.onClick.RemoveListener(OnSettingsClicked);
        _buttonNewGame.onClick.RemoveListener(OnStartNewGameClicked);
        _buttonContinueGame.onClick.RemoveListener(OnContinueGameClicked);
        _buttonLanguage.onClick.RemoveListener(OnLanguageClicked);
        _buttonLanguageBack.onClick.RemoveListener(OnLanguageBackClicked);
#if !UNITY_WEBGL
        _buttonExit.onClick.RemoveListener(OnExitClicked);
#endif
    }

    private void OnExitClicked() {
        Application.Quit();
    }

    private void Start() {
        if(AudioController.Instance == null) {
            Debug.LogError("Can't find audiocntroller!");
            return;
        }
        AudioController.Instance.SetLoopAndPlay("MainMenu");
        StartCoroutine(App.Backdrop.Release());
        _settings.Hide();
        _containerStartGame.SetActive(false);
        _containerChooseCharacter.SetActive(false);
        ButtonContinueState(App.SaveRunDataExists);
        _buttonChallenge.interactable = false;
        IntroAnimation();
    }
    private void ButtonContinueState(bool interactable) {
        _buttonContinueTab.interactable = interactable;
        _buttonContinueTabVisual.ChangeStateColor(interactable?  ButtonColor.Green : ButtonColor.Dissabled);
    }

    private void IntroAnimation() {
        Sequence introSeq = DOTween.Sequence();
        float targetZ = -10.5f;
        float targetY = 8.17f;
        // Camera 
        var p = _cameraTrans.position;
        p.z = 13;
        _cameraTrans.position = p;

        // Logo
        var l = _logoTrans.position;
        l.y = 18.93f;
        _logoTrans.position = l;
        introSeq.Append(_cameraTrans.DOLocalMoveZ(targetZ, 7).SetEase(Ease.OutCubic));
        // Insert lets us have the logo be placed while the camera is still moving without having to have a coroutine to delay the call
        introSeq.Insert(3,_logoTrans.DOLocalMoveY(targetY, 4).SetEase(Ease.OutBack));

    }

    private void OnSettingsClicked() {
        Debug.Log("Setting Click");
        _cameraTrans.DOMove(_settingsCameraPos.position, 2);
        _settings.Show(fromPause: false);
    }

    // Unity event from inspector
    public void OnSettingBack(bool isOpen) {
        if (isOpen) return;
        _cameraTrans.DOMove(_mainPageCameraPos.position, 2);
    }

    private void OnContinueGameClicked() {
        // ensure save data still exists
        Debug.Log("Continue clicked!!");
        if(App.SaveRunDataExists && SaveManager.TryLoad(out var saveData)) {  // We have to reload the save data here if we try and load again after quiting 
            
            var seed = saveData.worldData.Seed;
            var settings = new GameSettings(seed); // also other related things (like world pattern whatever) 
            settings.SaveToLoad = saveData; // This should be valid if App.SaveRunDataExists is true
            GameManager.Instance.Begin(settings);
        } else {
            // can't load, go back?
            _buttonContinueTab.interactable = false;

        }
        
    }
    public void OnPlayClicked() {
        Debug.Log("play cliked");
        //_containerStartGame.SetActive(true);
        _containerMainPage.SetActive(false);
        _containerChooseCharacter.SetActive(true);
    }
    public void OnPlayBackClicked() {
        _containerStartGame.SetActive(false);
    }
    public void OnCharacterBack() {
        _containerMainPage.SetActive(true);
        _containerChooseCharacter.SetActive(false);
        _containerStartGame.SetActive(false);
    }
    private void OnStartNewGameClicked() {
        if(_seedField.text != string.Empty) {
            if(int.TryParse(_seedField.text,out var seed)){
                var s = new GameSettings(seed);
                GameManager.Instance.Begin(s);
                return;
            }
        }
        var settings = new GameSettings(true); // Creates a random seed for us
        GameManager.Instance.Begin(settings);
    }

    private void OnLanguageBackClicked() {
        _containerLanguage.SetActive(false);
    }
    private void OnLanguageClicked() {
        _containerLanguage.SetActive(true);
    }

    public void OnButtonYouTubeClick() {
        Application.OpenURL("https://www.youtube.com/@r8teful/featured");
    }
    public void OnButtonDiscordClick() {
        Application.OpenURL("https://discord.gg/A88Fg8cVm8");
    }
    public void OnButtonBlueSkyClick() {
        Application.OpenURL("https://bsky.app/profile/mouseandcatgames.bsky.social");
    }
    public void OnButtonWebsiteClick() {
        Application.OpenURL("https://mouseandcatgames.com");
    }
    public void OnButtonInstagramClick() {
        Application.OpenURL("https://www.instagram.com/mouseandcatgames/");
    }
    public void OnButtonSteamClick() {
        Application.OpenURL("https://store.steampowered.com/curator/44869972");
    }

}