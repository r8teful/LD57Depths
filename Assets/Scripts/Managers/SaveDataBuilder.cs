using r8teful;
using UnityEngine;

public class SaveDataBuilder : MonoBehaviour {

    private void Save() {
        SaveData saveData = new SaveData();
        
        // Trigger save for monobehaviours

        // Finally write the save into memory
        SaveManager.Save(saveData);
    }
    public void TriggerSave() {
        Save();
    }
    
}