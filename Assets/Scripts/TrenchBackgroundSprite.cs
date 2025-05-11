using UnityEngine;

public class BackgroundSprite : MonoBehaviour {
    public int backgroundNumber;
    public Color backgroundColor;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    void Start() {
        float screenHeight = Camera.main.orthographicSize * 2;
        float screenWidth = screenHeight * Camera.main.aspect;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float spriteHeight = sr.bounds.size.y;
        float spriteWidth = sr.bounds.size.x;
        transform.localScale = new Vector3(screenWidth / spriteWidth, screenHeight / spriteHeight, 1);

        Shader.SetGlobalVector("_CamPos", Camera.main.transform.position);
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetTrenchSettings(WorldGenSettingSO settings) {
        if(_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.material.SetFloat("_Parallax", backgroundNumber * 0.2f); 
        _spriteRenderer.material.SetFloat("_BaseWidth", settings.trenchBaseWidth - (2*backgroundNumber)); 
        _spriteRenderer.material.SetFloat("_BaseWiden", settings.trenchWidenFactor);
        _spriteRenderer.material.SetFloat("_NoiseFreq", settings.trenchEdgeNoiseFrequency * 10f);
        _spriteRenderer.material.SetFloat("_EdgeAmp", settings.trenchEdgeNoiseAmplitude * 0.8f * Random.Range(0.7f,1.2f));
        _spriteRenderer.material.SetColor("_Color", backgroundColor);
        _spriteRenderer.sortingOrder -= backgroundNumber; 
    }

    private void Update() {
        _spriteRenderer.material.SetVector("_CamPos", Camera.main.transform.position);
    }

}