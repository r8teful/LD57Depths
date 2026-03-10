using DG.Tweening;
using r8teful;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ButtonMenuVisual;

public class MainMenu : MonoBehaviour {
    [SerializeField] private  Button _buttonPlay;
    [SerializeField] private  Button _buttonPlayBack;
    [SerializeField] private  Button _buttonNewGame;
    [SerializeField] private  Button _buttonContinueTab;
    [SerializeField] private  Button _buttonContinueGame;
    [SerializeField] private ButtonMenuVisual _buttonContinueTabVisual;
    [SerializeField] private  Button _buttonChallenge;
    [SerializeField] private  Button _buttonSettings;
    [SerializeField] private UISettings _settings; 
    [SerializeField] private Transform _cameraTrans; 
    [SerializeField] private Transform _logoTrans;
    
    [SerializeField] private GameObject _containerStartGame;

    [SerializeField] private TMP_InputField _seedField;

    private void OnEnable() {
        _buttonPlay.onClick.AddListener(OnPlayClicked);
        _buttonPlayBack.onClick.AddListener(OnPlayBackClicked);
        _buttonSettings.onClick.AddListener(OnSettingsClicked);
        _buttonNewGame.onClick.AddListener(OnStartNewGameClicked);
        _buttonContinueGame.onClick.AddListener(OnContinueGameClicked);
    }


    private void Start() {
        if(AudioController.Instance == null) {
            Debug.LogError("Can't find audiocntroller!");
            return;
        }
        AudioController.Instance.SetLoopAndPlay("MainMenu");
        _settings.Hide();
        _containerStartGame.SetActive(false);
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
        float targetZ = -14;
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
        _settings.Show(fromPause: false);
    }

    private void OnDisable() {
        _buttonPlay.onClick.RemoveListener(OnPlayClicked);
    }

    private void OnContinueGameClicked() {
        // ensure save data still exists
        Debug.Log("Continue clicked!!");
        if(App.SaveRunDataExists) {
            var seed = SaveManager.CurrentSave.worldData.Seed;
            var settings = new GameSettings(seed); // also other related things (like world pattern whatever) 
            settings.SaveToLoad = SaveManager.CurrentSave; // This should be valid if App.SaveRunDataExists is true
            GameManager.Instance.Begin(settings);
        } else {
            // can't load, go back?
            _buttonContinueTab.interactable = false;

        }
        
    }
    public void OnPlayClicked() {
        Debug.Log("play cliked");
        _containerStartGame.SetActive(true);
    }
    public void OnPlayBackClicked() {
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
    public void OnJoinClicked() {
        //SceneManager.LoadScene(1);
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
