using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIPauseScreen : MonoBehaviour {
    [SerializeField] private Transform _containerSettings;
    [SerializeField] private Transform _containerPause;
    [SerializeField] private GameObject _tint;
    [SerializeField] private Button _buttonResume;
    [SerializeField] private Button _buttonSettings;
    [SerializeField] private Button _buttonExit;
    [SerializeField] private Button _buttonSettingBack;
    [SerializeField] private UISettings _settings;

    public bool IsOpen => _tint.activeSelf; 

    private void Awake() {
        ResetToDefault();
        _buttonSettings.onClick.AddListener(OnSettingButtonClick);
        _buttonResume.onClick.AddListener(OnResumeButtonClick);
        _buttonExit.onClick.AddListener(OnExitButtonClick);
        _buttonSettingBack.onClick.AddListener(OnSettingBack);
    }

 

    // Make sure initial state is correct
    private void ResetToDefault() {
        _containerSettings.gameObject.SetActive(false);
        _containerPause.gameObject.SetActive(false);
        _tint.SetActive(false);

    }

    public void OnPauseClose() {
        ResetToDefault(); // This closes everything nicelly
    }
    private void OnSettingButtonClick() {
        Debug.Log("Click!");
        OnSettingOpen();
    }
    private void OnExitButtonClick() {
        //todo
    }

    private void OnResumeButtonClick() {
        OnPauseClose();
    }

    public void OnPauseOpen() {
        _tint.SetActive(true);
        _containerPause.gameObject.SetActive(true);
    }

    public void OnSettingOpen() {
        _containerPause.gameObject.SetActive(false);
        _containerSettings.gameObject.SetActive(true);
    }
    public void OnSettingBack() {
        _containerPause.gameObject.SetActive(true);
        _containerSettings.gameObject.SetActive(false);
        _settings.OnBack();
    }

    private void OnApplicationPause(bool pause) {
        
    }


    public void OnMenuClicked() {
        SceneManager.LoadScene(0);
    }
    public void OnRestartClicked() {
        SceneManager.LoadScene(1);
    }
    public void OnPauseCloseClicked() {
        Debug.LogWarning("No pause logic!");
        //UIMenuManager.Instance.OnPauseClose();
    }

}
