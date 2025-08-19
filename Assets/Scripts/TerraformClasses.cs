public class TerraformFactor {
    public TerraformType Type;
    public float Amount;

    public TerraformFactor(TerraformType type, float amount) {
        Type = type;
        Amount = amount;
    }
}
public enum TerraformType {
    Oxygen,
    Light,
    Polution
}