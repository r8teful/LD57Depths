using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class GameCutsceneManager : Singleton<GameCutsceneManager> {
    
    // There wont be many cutscenes so we just define each here for now
    public void StartSubUpgradeCutscene(Transform target, Action onUpgradeAction) {
        StartCoroutine(SubUpgradeCutscene(target, onUpgradeAction));
    }    

    private IEnumerator SubUpgradeCutscene(Transform target, Action onUpgradeAction) {
        var player = PlayerManager.Instance;
        if (player == null) yield break;
        var mainCamera = player.PlayerCamera;
        player.PlayerMovement.ChangeState(PlayerMovement.PlayerState.None); // dissable movement
        // Make sure Ui is closed
        player.UiManager.TryOpenCloseUpgradeUI(false, out bool wasUiOpen);

        // Save original camera position to return later
        Vector3 originalCamPos = mainCamera.transform.position;
        float time = 1.5f;
        player.PlayerCamera.SetCameraPosRelative(target.position,time);
        yield return new WaitForSeconds(time);
        
        // callback that will change the actual sprite
        onUpgradeAction?.Invoke();
        // move camera back

        player.PlayerCamera.SetCameraPosRelative(originalCamPos,time*0.5f);
        yield return new WaitForSeconds(time * 0.5f);
        
        // Try to open ui again if it was open
        if (wasUiOpen) {
            player.UiManager.TryOpenCloseUpgradeUI(true, out var _);
        }
        yield return null;
        player.PlayerMovement.ChangeState(PlayerMovement.PlayerState.Grounded); 
        GameSequenceManager.Instance.AdvanceSequence();
    }
}