using UnityEngine;

public class TranchBackgroundSprite : MonoBehaviour {
    public int backgroundNumber;
    public Color BackgroundColor;
    public int OrderInLayer => _spriteRenderer.sortingOrder;
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
        _spriteRenderer.material.SetFloat("_PixelSize", 60 + (20 * backgroundNumber)); // Default 60, higher quality the further away we are to get a cool depth effect
        _spriteRenderer.material.SetFloat("_BaseWidth", settings.GetTrenchWidth() - (2*backgroundNumber)); 
        _spriteRenderer.material.SetFloat("_BaseWiden", settings.GetTrenchWiden() - (0.0001f* backgroundNumber));
        _spriteRenderer.material.SetFloat("_NoiseFreq", settings.GetTrenchEdgeFreq()* 10f);
        _spriteRenderer.material.SetFloat("_EdgeAmp", settings.GetTrenchEdgeNoiseAmp()* 0.13333f * (backgroundNumber*0.15f + 0.7f));
        _spriteRenderer.material.SetColor("_Color", BackgroundColor);
        _spriteRenderer.material.SetFloat("_Seed", backgroundNumber);//Random.Range(-1000,1000));
        _spriteRenderer.material.SetFloat("_PlantScale", 0.2f - (0.05f*backgroundNumber));//Random.Range(-1000,1000));
        _spriteRenderer.material.SetFloat("_CellDensity",0.8f + (0.2f*backgroundNumber));//Random.Range(-1000,1000));
        _spriteRenderer.material.SetFloat("_DecorationSpawnsMax", 0.3f - (0.05f*backgroundNumber));//Random.Range(-1000,1000));
        _spriteRenderer.sortingOrder -= backgroundNumber; 
    }

    private void Update() {
        _spriteRenderer.material.SetVector("_CamPos", Camera.main.transform.position);
    }

}