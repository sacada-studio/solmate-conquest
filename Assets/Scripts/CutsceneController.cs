using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoEndTrigger : MonoBehaviour
{
    [Tooltip("Designer-assignable actions fired the first frame after the clip ends")]
    public UnityEvent OnVideoEnded;          // 👈 shows up in the Inspector

    private VideoPlayer _vp;

    void Awake ()
    {
        _vp = GetComponent<VideoPlayer>();
        _vp.loopPointReached += HandleVideoCompleted;
    }

    private void HandleVideoCompleted(VideoPlayer source)
    {
        // Fire every method that was wired up in the Inspector
        OnVideoEnded?.Invoke();
    }

    void OnDestroy ()
    {
        _vp.loopPointReached -= HandleVideoCompleted;
    }
}