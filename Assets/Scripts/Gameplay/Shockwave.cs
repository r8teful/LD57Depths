public class Shockwave : ShootableAbilityBase {
  
    public override void Shoot() {
        // cast sphere around origin
        var tiles = MineHelper.GetCircle(
            WorldManager.Instance.MainTileMap, transform.position, 8,true);
        foreach (var tile in tiles) {
            _player.CmdRequestDamageTile(tile.CellPos, tile.DamageRatio * 20);
        }
    }
}