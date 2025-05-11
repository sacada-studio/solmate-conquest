using UnityEngine;

namespace YourGame.Utilities
{
    /// <summary>
    /// Scales a SpriteRenderer so that its sprite exactly fills the camera's viewport.
    /// Drop this on any GameObject with a SpriteRenderer and it will "fit to camera."
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class FitToCamera : MonoBehaviour
    {
        [Tooltip("Which camera to use for sizing. If left empty, will default to Camera.main.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("If true, preserves the sprite aspect ratio (letterboxing/trimming may occur).")]
        [SerializeField] private bool preserveAspect = false;

        private SpriteRenderer _spriteRenderer;

        private void OnEnable()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            ApplyFit();
        }

#if UNITY_EDITOR
        // In the editor, recalc whenever values change
        private void OnValidate()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            ApplyFit();
        }
#endif

        private void LateUpdate()
        {
            ApplyFit();
        }

        private void ApplyFit()
        {
            if (_spriteRenderer == null || _spriteRenderer.sprite == null)
                return;

            // resolve camera
            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null || !cam.orthographic)
                return;

            // world-space size of camera viewport
            float camHeight = cam.orthographicSize * 2f;
            float camWidth  = camHeight * cam.aspect;

            // sprite size in world units
            Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;

            // compute required scale
            float scaleX = camWidth  / spriteSize.x;
            float scaleY = camHeight / spriteSize.y;

            Vector3 newScale;
            if (preserveAspect)
            {
                // Use max scale to ensure sprite fully covers screen (no margins)
                float uniformScale = Mathf.Max(scaleX, scaleY);
                newScale = new Vector3(uniformScale, uniformScale, 1f);
            }
            else
            {
                newScale = new Vector3(scaleX, scaleY, 1f);
            }

            transform.localScale = newScale;

            // optionally, ensure the sprite is centered
            Vector3 camPos = cam.transform.position;
            transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
        }
    }
}
