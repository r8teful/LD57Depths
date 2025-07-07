using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIUpgradeNode : MonoBehaviour, IPopupInfo, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private Button _buttonBig;
    [SerializeField] private Button _buttonSmall;
    public bool IsBig;
    private UpgradeRecipeSO upgradeData;
    public event Action PopupDataChanged;
    public event Action<IPopupInfo, bool> OnPopupShow;

    internal void Init(UpgradeRecipeSO upgradeRecipeSO) {
        upgradeData = upgradeRecipeSO;

        if (_buttonBig != null && _buttonSmall != null) {
            _buttonBig.onClick.RemoveAllListeners();
            _buttonBig.onClick.AddListener(OnUpgradeButtonClicked);

            _buttonSmall.onClick.RemoveAllListeners();
            _buttonSmall.onClick.AddListener(OnUpgradeButtonClicked);
        }
    }
    public void OnPointerEnter(PointerEventData eventData) {
        OnPopupShow?.Invoke(this, true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        OnPopupShow?.Invoke(this, false);
    }
    private void OnUpgradeButtonClicked() {
       // UICraftingManager.Instance.AttemptCraft(upgradeData, null, null);
    }

    public PopupData GetPopupData(GameObject obj = null) {
        return null;
        //new PopupData(upgradeData.displayName, upgradeData.description, upgradeData.GetIngredientStatuses());
    }
}
