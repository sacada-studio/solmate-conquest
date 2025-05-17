using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;   // only needed if you prefer IPointerClickHandler

/// <summary>
/// Plays a one-shot SFX (via AudioManager) whenever the Button is clicked.
/// Attach this script to any GameObject that also has a UnityEngine.UI.Button.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSfx : MonoBehaviour
{
    [Tooltip("Clip that will be routed to AudioManager.Instance.PlaySfx()")]
    [SerializeField] private AudioClip audio;

    private Button _button;

    void Awake()
    {
        // Cache the Button reference and register the listener
        _button = GetComponent<Button>();
        _button.onClick.AddListener(PlaySound);
    }

    private void PlaySound()
    {
        // Safeguards: make sure the singleton and the clip exist
        if (AudioManager.Instance == null || audio == null) return;

        AudioManager.Instance.PlaySfx(audio);
    }

    void OnDestroy()
    {
        // Good practice: unregister the listener to avoid leaks in Edit mode
        if (_button != null)
            _button.onClick.RemoveListener(PlaySound);
    }
}