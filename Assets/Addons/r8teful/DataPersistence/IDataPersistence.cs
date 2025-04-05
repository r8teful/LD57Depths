public interface IDataPersistence
{
    void LoadData(MasterGameData data);

    // The 'ref' keyword was removed from here as it is not needed.
    // In C#, non-primitive types are automatically passed by reference.
    void SaveData(MasterGameData data);
}
