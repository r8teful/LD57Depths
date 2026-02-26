using Anarkila.DeveloperConsole;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIPauseScreen : MonoBehaviour {
    [SerializeField] private Transform _containerSettings;
    [SerializeField] private Transform _containerPause;
    [SerializeField] private GameObject _tint;

    public bool IsOpen => _tint.activeSelf; 

    private void Awake() {
        // Make sure initial state is correct
        _containerSettings.gameObject.SetActive(false);
        _containerPause.gameObject.SetActive(false);
        _tint.gameObject.SetActive(false);
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
