using FishNet.Managing;
using FishNet.Transporting;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour {
    public AudioMixerGroup SFXMixer;
    public AudioMixerGroup MusicMixer;
    public Slider SFXSlider;
    public Slider MusicSlider;
    [SerializeField] NetworkManager _networkManager;
    [SerializeField] Button _buttonHost;
    [SerializeField] Button _buttonJoin;
    
    [SerializeField] private TMP_InputField _addressField;
    private void Start() {
        // Subscribe to client connection events to know when we are connected.
        _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
    }
    private void OnEnable() {
        _buttonHost.onClick.AddListener(OnHostClicked);
        _buttonJoin.onClick.AddListener(OnJoinClicked);
    }
    private void OnDisable() {
        _buttonHost.onClick.RemoveListener(OnHostClicked);
        _buttonJoin.onClick.RemoveListener(OnJoinClicked);
        // Unsubscribe from client connection events to avoid memory leaks.
        _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args) {
        if (args.ConnectionState == LocalConnectionState.Started) {
            // If we are the host/server, we need to load the scene.
            // The server will automatically tell connecting clients to do the same.
            if (_networkManager.IsServerStarted) {
                // This will load the scene on the server and all connected clients.
                //_networkManager.SceneManager.LoadGlobalScenes(new SceneLoadData("PlayScene"));
            }
        }
    }

    // Starting new hosting game
    public void OnHostClicked() {
        _networkManager.ServerManager.StartConnection();
        _networkManager.ClientManager.StartConnection();
        //SceneManager.LoadScene(1);
    }
    public void OnJoinClicked() {
        _networkManager.ClientManager.StartConnection(_addressField.text);
        //SceneManager.LoadScene(1);
    }
    public void OnSFXChanged(float v) {
        SFXMixer.audioMixer.SetFloat("sfx", Mathf.Log10(SFXSlider.value) * 20);
    }
    public void OnMuiscChanged(float v) {
        SFXMixer.audioMixer.SetFloat("music", Mathf.Log10(MusicSlider.value) * 20);
    }
    public void OnButtonYouTubeClick() {
        Application.OpenURL("https://www.youtube.com/@r8teful/featured");
    }
    public void OnButtonDiscordClick() {
        Application.OpenURL("https://discord.gg/A88Fg8cVm8");
    }
    public void OnButtonBlueSkyClick() {
        Application.OpenURL("https://bsky.app/profile/mouseandcatgames.bsky.social");
    }
    public void OnButtonWebsiteClick() {
        Application.OpenURL("https://mouseandcatgames.com");
    }
    public void OnButtonInstagramClick() {
        Application.OpenURL("https://www.instagram.com/mouseandcatgames/");
    }
    public void OnButtonSteamClick() {
        Application.OpenURL("https://store.steampowered.com/curator/44869972");
    }

}
