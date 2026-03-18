using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static MineHelper;

public class LazerChainSpawner : MonoBehaviour, IInitializableAbility, IValueModifiable {
    private MiningLazerNew _lazer;
    private PlayerManager _player;
    private float _chanceBase;
    private float _chance;
    private DamageContainer _damageContainer;
    private float _damage;
    private float _damageBase;
    private float _length;
    private float _lengthBase;
    public LineRenderer LineRenderVisualPrefab;

    public void Init(AbilityInstance instance, PlayerManager player) {
        
        var lazer = player.PlayerAbilities.GetAbilityInstance(ResourceSystem.LazerEffectID);
        if (lazer != null && lazer.Object != null && lazer.Object.TryGetComponent<MiningLazerNew>(out var lazerScript)) {
            Debug.Log("Found lazer script! subscribing...");
            _lazer = lazerScript;
            _lazer.OnTileDamaged += OnLazerTileDamaged; // oh my god but it works like this! 
        }
        _player = player;
        _chanceBase = 0.01f;
        _chance = _chanceBase;
        _damageContainer = new DamageContainer();
        Register();
    }
    private void OnDestroy() {
        if (_lazer != null) {
            _lazer.OnTileDamaged -= OnLazerTileDamaged;
        }
    }
    private void OnLazerTileDamaged(DamageContainer dmg) {
        if (Random.value > 0.1f) return; // fail
        var steps = 7;
        var stepLength =1 ;
        float deviationAngle = 60f;
        var tiles = MineHelper.GetLightningBolt(WorldManager.Instance.MainTileMap, dmg.tile, dmg.hitDirection, steps, stepLength, deviationAngle);
        _damageContainer.damage = 10;
        foreach (var tile in tiles) {
            _damageContainer.tile = tile.CellPos;
            _player.RequestDamageTile(_damageContainer);
        }
        StartCoroutine(PlayLightningEffect(WorldManager.Instance.MainTileMap, tiles,dmg, LineRenderVisualPrefab));
    }
    public IEnumerator PlayLightningEffect(
    Tilemap map,
    List<TileDamageData> damageData,
    DamageContainer sourceDamage,
    LineRenderer lineRendererPrefab,
    float segmentDelay = 0.02f,
    float holdDuration = 0.20f,
    float fadeDuration = 0.2f) {
        if (damageData == null || damageData.Count == 0 || lineRendererPrefab == null)
            yield break;


        var lineRenderer = Instantiate(lineRendererPrefab);
        // --- Setup ---
        lineRenderer.gameObject.SetActive(true);
        lineRenderer.positionCount = 0;

        // Cache the original colors so we can restore or fade from them
        Color startColor = lineRenderer.startColor;
        Color endColor = lineRenderer.endColor;

        // --- Reveal phase: add one point per segment with a delay ---
        for (int i = 0; i < damageData.Count; i++) {
            Vector3 worldPos;
            
            if(i == 0 && sourceDamage.exactHitPoint != null) {
                worldPos = sourceDamage.exactHitPoint;
            } else {
                worldPos = map.GetCellCenterWorld(damageData[i].CellPos);
            }

            lineRenderer.positionCount = i + 1;
            lineRenderer.SetPosition(i, worldPos);

            // tint each point's brightness by the damage ratio
            Color tinted = Color.Lerp(Color.clear, startColor, damageData[i].DamageRatio);
            lineRenderer.startColor = tinted;
            lineRenderer.endColor = Color.Lerp(Color.clear, endColor, damageData[i].DamageRatio);

            yield return new WaitForSeconds(segmentDelay);
        }

        // Restore full colors for the hold phase
        lineRenderer.startColor = startColor;
        lineRenderer.endColor = endColor;

        // --- Hold phase: bolt stays fully visible ---
        yield return new WaitForSeconds(holdDuration);

        // --- Fade phase: lerp alpha to zero ---
        float elapsed = 0f;
        while (elapsed < fadeDuration) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            lineRenderer.startColor = FadeAlpha(startColor, 1f - t);
            lineRenderer.endColor = FadeAlpha(endColor, 1f - t);

            yield return null;
        }

        Destroy(lineRenderer);
    }
    private Color FadeAlpha(Color color, float alphaScale) {
        color.a *= alphaScale;
        return color;
    }
    public float GetValueBase(ValueKey key) {
        if (key == ValueKey.LazerChainLength) return _lengthBase;
        if (key == ValueKey.LazerChainChance) return _chanceBase;
        if (key == ValueKey.LazerChainDamage) return _damageBase;
        return 1;
    }

    public float GetValueNow(ValueKey key) {
        if (key == ValueKey.LazerChainLength) return _length;
        if (key == ValueKey.LazerChainChance) return _chance;
        if (key == ValueKey.LazerChainDamage) return _damage;
        return 0;
    }
    public void ModifyValue(ValueModifier modifier) {
        if (modifier.Key == ValueKey.LazerChainLength) _length = UpgradeCalculator.CalculateNewUpgradeValue(_length,modifier);
        if (modifier.Key == ValueKey.LazerChainChance) _chance = UpgradeCalculator.CalculateNewUpgradeValue(_chance,modifier);
        if (modifier.Key == ValueKey.LazerChainDamage) _damage = UpgradeCalculator.CalculateNewUpgradeValue(_damage,modifier);
    }

    public void Register() {
        PlayerManager.Instance.UpgradeManager.RegisterValueModifierScript(ValueKey.LazerChainLength, this);
        PlayerManager.Instance.UpgradeManager.RegisterValueModifierScript(ValueKey.LazerChainChance, this);
        PlayerManager.Instance.UpgradeManager.RegisterValueModifierScript(ValueKey.LazerChainDamage, this);

    }

    public void ReturnValuesToBase() {
        _length = _lengthBase;
        _chance = _chanceBase;
        _damage = _damageBase;

    }


}