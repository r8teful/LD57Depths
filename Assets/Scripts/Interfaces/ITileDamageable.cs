using System;

public interface ITileDamageable  {
    public event Action<DamageContainer> OnTileDamaged;
}