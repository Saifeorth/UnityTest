using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class ControlsPanelUI : MonoBehaviour
{
    [Header("References")]
    public GameObject panel;            // The UI panel to toggle
    public Button restartButton;        // Button to restart scene
    public Image fadeImage;             // Black overlay image for fade effect

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.Tab; // Key to open/close
    public float fadeDuration = 1f;         // Time for fade effect

    private bool isOpen = false;

    void Start()
    {
        // Hide panel by default
        if (panel != null)
            panel.SetActive(false);

        // Restart button listener
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartScene);

        // Start fade out
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = Color.black;
            StartCoroutine(FadeOut());
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }
    }

    public void TogglePanel()
    {
        isOpen = !isOpen;

        if (panel != null)
            panel.SetActive(isOpen);

        // Optionally pause/unpause time when open
        Time.timeScale = isOpen ? 0f : 1f;
    }

    private void RestartScene()
    {
        Time.timeScale = 1f;
        StartCoroutine(FadeInAndRestart());
    }

    private IEnumerator FadeOut()
    {
        float t = 0f;
        Color c = fadeImage.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.gameObject.SetActive(false);
    }

    private IEnumerator FadeInAndRestart()
    {
        fadeImage.gameObject.SetActive(true);
        Color c = fadeImage.color;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
