using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {
    public void OnPlayClicked() {
        SceneManager.LoadScene(1);
    }
}
