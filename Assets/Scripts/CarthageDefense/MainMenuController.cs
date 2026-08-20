using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Wires up the hand-placed Main Menu scene: Play/Settings/Credits/Quit buttons and the two
/// volume sliders. Every object it references lives in the scene itself (see MainMenuSceneBuilder), so the
/// menu can be freely restyled in the Inspector without touching this script.</summary>
public class MainMenuController : MonoBehaviour
{
    private const string MusicVolumeKey = "CarthageDefense.MusicVolume";
    private const string SfxVolumeKey = "CarthageDefense.SfxVolume";

    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private TextAsset creditsFile;
    [SerializeField] private TextMeshProUGUI creditsText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button creditsBackButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Awake()
    {
        // MainMenu is the first scene loaded, so the audio singletons don't exist yet anywhere else —
        // spin them up here (DontDestroyOnLoad carries them into GameScene afterward).
        SfxManager.Ensure();
        MusicManager.Ensure().PlayMenuMusic();

        if (playButton != null) playButton.onClick.AddListener(PlayGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (backButton != null) backButton.onClick.AddListener(CloseSettings);
        if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);
        if (creditsBackButton != null) creditsBackButton.onClick.AddListener(CloseCredits);
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
        if (creditsPanel != null) creditsPanel.SetActive(false);
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

    private void OpenCredits()
    {
        SfxManager.Instance?.PlayButtonClick();
        if (creditsText != null && creditsFile != null) creditsText.text = FormatCredits(creditsFile.text);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    private void CloseCredits()
    {
        SfxManager.Instance?.PlayButtonClick();
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    // CREDITS.txt is plain text so it's easy to edit without touching the scene; a line written in ALL
    // CAPS is treated as a section heading and gets bolded/colored/spaced, everything else is body text.
    private static string FormatCredits(string raw)
    {
        var sb = new StringBuilder();
        string[] lines = raw.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmed = line.Trim();
            bool isHeading = trimmed.Length > 0 && IsHeading(trimmed);
            if (isHeading)
            {
                if (i > 0) sb.Append('\n');
                sb.Append("<color=#ECC874><b>").Append(trimmed).Append("</b></color>\n");
            }
            else
            {
                sb.Append(line).Append('\n');
            }
        }
        return sb.ToString();
    }

    private static bool IsHeading(string trimmed)
    {
        bool hasLetter = false;
        foreach (char c in trimmed)
        {
            if (char.IsLower(c)) return false;
            if (char.IsLetter(c)) hasLetter = true;
        }
        return hasLetter;
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
