using System.Collections;
using UnityEngine;

public class MusicManager : StaticInstance<MusicManager> {

    protected override void Awake() {
        base.Awake();
        GameSetupManager.OnSetupComplete += SetupComplete;
    }
    private void OnDestroy() {
        GameSetupManager.OnSetupComplete -= SetupComplete;
    }

    // BEWARE!! if you directly start playing a song when the main menu music is still fading out it breaks for some reason
    private void SetupComplete() {
        StartCoroutine(PlaySongRandomDelay());    
    }

    private IEnumerator PlaySongRandomDelay() {
        
        yield return new WaitForSeconds(UnityEngine.Random.Range(60, 120));
        //yield return new WaitForSeconds(4f);
        //yield return null;
        PlayRandomSong();
    }

    private void PlayRandomSong() {
        AudioController.Instance.SetLoopAndPlay("Undersolver");
    }
}