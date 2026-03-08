using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIPauseScreen : MonoBehaviour {
    [SerializeField] private Transform _containerPause;
    [SerializeField] private GameObject _tint;
    [SerializeField] private Button _buttonResume;
    [SerializeField] private Button _buttonSettings;
    [SerializeField] private Button _buttonExit;
    [SerializeField] private Button _buttonSettingBack;
    [SerializeField] private UISettings _settings;
    [SerializeField] private TextMeshProUGUI _seedNumber;

    public bool IsOpen => _tint.activeSelf; 

    private void Awake() {
        ResetToDefault();
        _buttonSettings.onClick.AddListener(OnSettingButtonClick);
        _buttonResume.onClick.AddListener(OnResumeButtonClick);
        _buttonExit.onClick.AddListener(OnExitButtonClick);
        _buttonSettingBack.onClick.AddListener(OnSettingBack);
    }
    private void Start() {
        if (GameSetupManager.Instance == null) return;
        _seedNumber.text = GameSetupManager.Instance.WorldGenSettings.seed.ToString();
    }


    // Make sure initial state is correct
    private void ResetToDefault() {
        _containerPause.gameObject.SetActive(false);
        _settings.Hide();
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
        // cool animation etc..
        if (UIManager.Instance == null) {
            Debug.LogError("uimanager null!");
            return;
        }
        UIManager.Instance.Unpause();
        SceneManager.LoadScene(0);
    }

    private void OnResumeButtonClick() {
        UIManager.Instance.Unpause(); // Will call on close
    }

    public void OnPauseOpen() {
        _tint.SetActive(true);
        _containerPause.gameObject.SetActive(true);
    }

    public void OnSettingOpen() {
        transform.SetAsLastSibling(); // ensures we are on top
        _containerPause.gameObject.SetActive(false);
        _settings.Show(fromPause: true);
    }
    public void OnSettingBack() {
        _containerPause.gameObject.SetActive(true);
        _settings.Hide();
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
