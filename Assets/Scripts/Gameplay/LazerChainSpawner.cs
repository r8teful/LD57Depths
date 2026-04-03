using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static MineHelper;
using static UnityEditor.Experimental.GraphView.GraphView;

public class LazerChainSpawner : MonoBehaviour, IInitializableAbility{
    private ITileDamageable _lazer;
    private PlayerManager _player;
    private float _chanceBase;
    private float _chance;
    [SerializeField] private ValueModifiableComponent _values;
    private DamageContainer _damageContainer;
    public LineRenderer LineRenderVisualPrefab;

    public void Init(AbilityInstance instance, PlayerManager player) {
        
        var lazer = player.PlayerAbilities.GetAbilityInstance(ResourceSystem.LazerEffectID);
        if (lazer != null && lazer.Object != null && lazer.Object.TryGetComponent<ITileDamageable>(out var lazerScript)) {
            Debug.Log("Found lazer script! subscribing...");
            _lazer = lazerScript;
            _lazer.OnTileDamaged += OnLazerTileDamaged; // oh my god but it works like this! 
        }
        _player = player;
        _values.Register();
        _chanceBase = 0.01f;
        _chance = _chanceBase;
        _damageContainer = new DamageContainer();
    }
    private void OnDestroy() {
        if (_lazer != null) {
            _lazer.OnTileDamaged -= OnLazerTileDamaged;
        }
    }
    private void OnLazerTileDamaged(DamageContainer dmg) {
        var chance = _values.GetValueNow(ValueKey.LazerChainChance);
        if (Random.value > chance) return; // fail
        SpawnChain(dmg);
    }
    public void SpawnChain(DamageContainer dmg) {
        float length = _values.GetValueNow(ValueKey.LazerChainLength);
        var damage = _values.GetValueNow(ValueKey.LazerChainDamage);
        SpawnChainInternal(dmg, length, damage);
    }
    public void SpawnChainOverride(DamageContainer dmg,float length) {
        SpawnChainInternal(dmg, length,dmg.damage);
    }
    private void SpawnChainInternal(DamageContainer dmg,float length, float damage) {
        var steps = Mathf.FloorToInt(length); // Length is just the steps for now
        var stepLength = 1;
        float deviationAngle = 89f;
        var tiles = MineHelper.GetLightningBolt(WorldManager.Instance.MainTileMap, dmg.tile, dmg.hitDirection, steps, stepLength, deviationAngle, checkSolid: true);
        _damageContainer.damage = damage;

        _player.PlayerCamera.Shake(steps*0.02f);
        StartCoroutine(PlayLightningEffect(WorldManager.Instance.MainTileMap, tiles, dmg, LineRenderVisualPrefab));
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
            
            if(i == 0 && sourceDamage.exactHitPoint != null && sourceDamage.exactHitPoint != Vector2.zero) {
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

            // DAMAGE 
            _damageContainer.tile = damageData[i].CellPos;
            _player.RequestDamageTile(_damageContainer);
            


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
}