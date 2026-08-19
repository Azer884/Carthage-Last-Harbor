using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Wires up the hand-placed Main Menu scene: Play/Settings/Quit buttons and the two volume
/// sliders. Every object it references lives in the scene itself (see MainMenuSceneBuilder), so the menu
/// can be freely restyled in the Inspector without touching this script.</summary>
public class MainMenuController : MonoBehaviour
{
    private const string MusicVolumeKey = "CarthageDefense.MusicVolume";
    private const string SfxVolumeKey = "CarthageDefense.SfxVolume";

    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Awake()
    {
        if (playButton != null) playButton.onClick.AddListener(PlayGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (backButton != null) backButton.onClick.AddListener(CloseSettings);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        // Read back whatever was saved last time; falls back to whatever the slider was left at in the
        // Editor (its designed-in default) the very first time the menu ever runs.
        if (musicSlider != null)
        {
            musicSlider.value = PlayerPrefs.GetFloat(MusicVolumeKey, musicSlider.value);
            musicSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(MusicVolumeKey, v));
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat(SfxVolumeKey, sfxSlider.value);
            sfxSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(SfxVolumeKey, v));
        }

        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void PlayGame()
    {
        SfxManager.Instance?.PlayButtonClick();
        SceneManager.LoadScene(gameSceneName);
    }

    private void OpenSettings()
    {
        SfxManager.Instance?.PlayButtonClick();
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    private void CloseSettings()
    {
        SfxManager.Instance?.PlayButtonClick();
        PlayerPrefs.Save();
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
