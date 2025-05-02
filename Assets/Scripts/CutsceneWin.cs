using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneWin : MonoBehaviour{
    public GameObject YouWinText;
    public GameObject MainMenuButton;
    void Start() {
        YouWinText.SetActive(false);
        MainMenuButton.SetActive(false);
        StartCoroutine(WaitAndSpawn()); 
    }
    private IEnumerator WaitAndSpawn() {
        yield return new WaitForSeconds(4);
        YouWinText.SetActive(true);
        YouWinText.GetComponent<RectTransform>().DOAnchorPos(new Vector2(0, -39), 2).SetLoops(-1, LoopType.Yoyo);
        yield return new WaitForSeconds(2);
        MainMenuButton.SetActive(true);
    }
    public void MainMenuClicked() {
        SceneManager.LoadScene(0);
    }
}
