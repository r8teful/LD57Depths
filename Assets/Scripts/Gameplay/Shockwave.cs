using System.Collections;
using UnityEngine;
using DG.Tweening;
public class Shockwave : ShootableAbilityBase {
    [SerializeField] private SpriteRenderer _spriteVisual;
    public override void Shoot() {
        // cast sphere around origin
        var damage = _abilityInstance.GetEffectiveStat(StatType.MiningDamage);
        var size = _abilityInstance.GetEffectiveStat(StatType.Size);
        StartCoroutine(ShootRoutine(size, damage));
    }
    private void ActuallyShoot(float size, float damage) {
        var tiles = MineHelper.GetCircle(
            WorldManager.Instance.MainTileMap, transform.position, size, true);
        foreach (var tile in tiles) {
            _player.RequestDamageTile(tile.CellPos, tile.DamageRatio * damage);
        }
    }
    private IEnumerator ShootRoutine(float radius, float damage) {
        Vector2 spriteSize = _spriteVisual.sprite.bounds.size;
        float desiredDiameter = radius * 2f;
        float s = desiredDiameter / Mathf.Max(spriteSize.x, spriteSize.y);

        _spriteVisual.transform.localScale = new(0.01f, 0.01f);
        _spriteVisual.material.SetFloat("_NoiseStrength", 0.06f);
        //_spriteVisual.color = Color.white; // Show fully
        Sequence mySequence = DOTween.Sequence();
        // Add a movement tween at the beginning
        mySequence.Append(_spriteVisual.transform.DOScale(s, 0.8f).SetEase(Ease.InQuad));
        mySequence.Insert(0.5f, _spriteVisual.material.DOFloat(0, "_NoiseStrength", 0.8f));
        mySequence.Append(_spriteVisual.transform.DOScale(0, 0f));

        yield return new WaitForSeconds(0.4f);
        ActuallyShoot(radius, damage);

    }
    void Animate(float radius) {

        //transform.localScale = new Vector3(s, s, 1f);

    }
}