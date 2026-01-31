using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class UIXPBar : MonoBehaviour {
    [SerializeField] private Image xpFillImage;
    [SerializeField] private Animator barAnimator;
    [SerializeField] private TextMeshProUGUI xpText;

    [Header("Settings")]
    [SerializeField] private float fillSpeed = 5f;

    private float targetFillAmount = 0f;
    private bool isLevelingUp = false;

    private void OnEnable() {
        RewardEvents.OnXPChanged += UpdateBar;
        RewardEvents.OnLevelUpReady += HandleLevelUpReady;
    }

    private void OnDisable() {
        RewardEvents.OnXPChanged -= UpdateBar;
        RewardEvents.OnLevelUpReady -= HandleLevelUpReady;
    }

    private void Update() {
        // Smoothly animate the bar filling up
        if (xpFillImage.fillAmount != targetFillAmount) {
            xpFillImage.fillAmount = Mathf.Lerp(xpFillImage.fillAmount, targetFillAmount, Time.deltaTime * fillSpeed);
        }
    }

    // Called when XP changes or after a Level Up is committed
    private void UpdateBar(float ratio, int currentXP) {
        // If we were animating a level up, stop it now because we just committed/reset
        if (isLevelingUp) {
            isLevelingUp = false;
            if (barAnimator) barAnimator.SetBool("IsFull", false);
            xpFillImage.fillAmount = 0f;
        }
        targetFillAmount = Mathf.Clamp01(ratio);
        xpText.text = currentXP.ToString();
    }

    // Called when the Manager says "Wait for player input"
    private void HandleLevelUpReady(int newLevel) {
        isLevelingUp = true;

        // Ensure visual is totally full
        targetFillAmount = 1f;

        if (barAnimator) barAnimator.SetBool("IsFull", true);
    }
}