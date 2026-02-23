using UnityEngine;
using UnityEngine.EventSystems;

namespace BlindSignal.UI
{
    /// <summary>
    /// Mobile-friendly virtual joystick that works with Unity's UI Event System.
    ///
    /// SETUP in Unity Editor:
    ///  1. Create a Canvas (Screen Space – Overlay, scale with screen size).
    ///  2. Add a Panel as the joystick background (JoystickBackground).
    ///  3. Add a child Image as the draggable handle (JoystickHandle).
    ///  4. Attach this script to the JoystickBackground object.
    ///  5. Assign the Handle transform in the Inspector.
    ///  6. Ensure a PhysicsRaycaster / GraphicRaycaster and an EventSystem are
    ///     present in the scene.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------
        [Header("References")]
        [Tooltip("The draggable handle image inside the joystick background.")]
        [SerializeField] private RectTransform _handle;

        [Header("Settings")]
        [Tooltip("Maximum distance (in pixels) the handle can move from the centre.")]
        [SerializeField] private float _maxRadius = 60f;

        // -------------------------------------------------------------------------
        // Public output (read by PlayerMovement every frame)
        // -------------------------------------------------------------------------
        /// <summary>Normalised direction vector in [-1,1] range on both axes.</summary>
        public Vector2 Direction { get; private set; }

        /// <summary>Magnitude of the input in [0,1] range.</summary>
        public float Magnitude { get; private set; }

        // -------------------------------------------------------------------------
        // Private state
        // -------------------------------------------------------------------------
        private RectTransform _bgRect;
        private Vector2 _originLocalPoint;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            _bgRect = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            ResetHandle();
        }

        // -------------------------------------------------------------------------
        // IPointerDownHandler
        // -------------------------------------------------------------------------
        public void OnPointerDown(PointerEventData eventData)
        {
            // Reposition the joystick centre to where the finger landed (optional UX).
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _bgRect, eventData.position, eventData.pressEventCamera, out _originLocalPoint);

            OnDrag(eventData);
        }

        // -------------------------------------------------------------------------
        // IDragHandler
        // -------------------------------------------------------------------------
        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _bgRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

            Vector2 delta = localPoint - _originLocalPoint;
            float distance = delta.magnitude;

            // Clamp the handle to the defined radius.
            Vector2 clamped = distance > _maxRadius
                ? delta.normalized * _maxRadius
                : delta;

            if (_handle != null)
                _handle.anchoredPosition = clamped;

            Direction = clamped / _maxRadius;          // normalised, magnitude ≤ 1
            Magnitude = Mathf.Clamp01(distance / _maxRadius);
        }

        // -------------------------------------------------------------------------
        // IPointerUpHandler
        // -------------------------------------------------------------------------
        public void OnPointerUp(PointerEventData eventData)
        {
            ResetHandle();
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------
        private void ResetHandle()
        {
            Direction = Vector2.zero;
            Magnitude = 0f;
            if (_handle != null)
                _handle.anchoredPosition = Vector2.zero;
        }
    }
}
