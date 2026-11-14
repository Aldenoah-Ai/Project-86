using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Runtime touch controls for the existing Juggernaut input-event pipeline.
/// Added automatically by InputManager on iOS/Android; desktop input is unchanged.
/// </summary>
public sealed class MobileTouchControls : MonoBehaviour
{
    [SerializeField] private float lookSensitivity = 1f;
    [SerializeField] private float joystickDeadZone = 0.12f;

    private Canvas _canvas;
    private RectTransform _stickHandle;
    private int _stickPointerId = int.MinValue;
    private int _lookPointerId = int.MinValue;

    private const float ButtonAlpha = 0.72f;

    private void Awake()
    {
        if (!Application.isMobilePlatform)
        {
            enabled = false;
            return;
        }

        EnsureEventSystem();
        BuildInterface();
    }

    private void Update()
    {
        if (_canvas != null && InputManager.Instance != null)
            _canvas.enabled = InputManager.Instance.IsJuggernautInputActive;
    }

    private void OnDestroy()
    {
        if (EventManager.Instance == null)
            return;

        SendMove(Vector2.zero);
        SendHeld("OnRun", false);
        SendHeld(Constants.TypedEvents.Inputs.OnJump, false);
        SendHeld("OnGrapplingThrow", false);
        SendHeld("OnPrimaryFire", false);
        SendHeld("OnSecondaryFire", false);
    }

    private void EnsureEventSystem()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            var go = new GameObject("Mobile EventSystem");
            eventSystem = go.AddComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            var oldModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
                oldModule.enabled = false;
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private void BuildInterface()
    {
        var canvasObject = new GameObject("Project86 Mobile Controls", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        _canvas = canvasObject.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Transparent right-side look surface. Buttons are drawn above it.
        var look = CreateImage("Look Area", canvasObject.transform, new Vector2(0.42f, 0.08f),
            new Vector2(1f, 0.94f), new Color(1, 1, 1, 0.001f));
        look.raycastTarget = true;
        look.gameObject.AddComponent<MobileLookSurface>().Initialize(this);

        // Movement joystick.
        var joystick = CreateImage("Movement Joystick", canvasObject.transform,
            new Vector2(0.15f, 0.205f), new Vector2(0.15f, 0.205f),
            new Color(0.08f, 0.1f, 0.13f, 0.42f));
        joystick.rectTransform.sizeDelta = new Vector2(170, 170);
        joystick.gameObject.AddComponent<MobileJoystick>().Initialize(this);

        var handle = CreateImage("Joystick Handle", joystick.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Color(0.72f, 0.82f, 0.92f, 0.72f));
        handle.rectTransform.sizeDelta = new Vector2(68, 68);
        handle.raycastTarget = false;
        _stickHandle = handle.rectTransform;

        // Movement and combat controls.
        AddHoldButton(canvasObject.transform, "RUN", new Vector2(0.15f, 0.39f), 58,
            () => SendHeld("OnRun", true), () => SendHeld("OnRun", false));
        AddHoldButton(canvasObject.transform, "FIRE", new Vector2(0.865f, 0.255f), 104,
            () => SendTap("OnPrimaryFire"), () => SendTap("OnPrimaryFire"));
        AddHoldButton(canvasObject.transform, "SUB", new Vector2(0.70f, 0.18f), 76,
            () => SendTap("OnSecondaryFire"), () => SendTap("OnSecondaryFire"));
        AddHoldButton(canvasObject.transform, "JUMP", new Vector2(0.91f, 0.47f), 72,
            () => SendHeld(Constants.TypedEvents.Inputs.OnJump, true),
            () => SendHeld(Constants.TypedEvents.Inputs.OnJump, false));
        AddHoldButton(canvasObject.transform, "GRAPPLE", new Vector2(0.75f, 0.46f), 72,
            () => SendHeld("OnGrapplingThrow", true),
            () => SendHeld("OnGrapplingThrow", false));

        AddTapButton(canvasObject.transform, "RELOAD", new Vector2(0.59f, 0.28f), 60,
            () => SendTap("OnReload"));
        AddTapButton(canvasObject.transform, "ZOOM +", new Vector2(0.60f, 0.52f), 54,
            () => SendValue("OnZoomIn", 1f));
        AddTapButton(canvasObject.transform, "ZOOM -", new Vector2(0.69f, 0.62f), 54,
            () => SendValue("OnZoomOut", -1f));
        AddTapButton(canvasObject.transform, "VIEW", new Vector2(0.84f, 0.66f), 54,
            () => SendTap(Constants.Events.Inputs.OnChangeView));
        AddTapButton(canvasObject.transform, "MAP", new Vector2(0.92f, 0.78f), 50,
            () => SendHeld(Constants.TypedEvents.Inputs.OnToggleMap, true),
            () => SendHeld(Constants.TypedEvents.Inputs.OnToggleMap, false));
        AddTapButton(canvasObject.transform, "OBJ", new Vector2(0.84f, 0.78f), 50,
            () => SendHeld(Constants.TypedEvents.Inputs.OnToggleObjective, true),
            () => SendHeld(Constants.TypedEvents.Inputs.OnToggleObjective, false));
        AddTapButton(canvasObject.transform, "PAUSE", new Vector2(0.94f, 0.91f), 52,
            () => InputManager.Instance.OnMobilePause());
    }

    private Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private void AddTapButton(Transform parent, string label, Vector2 anchor, float size, System.Action tap)
    {
        AddHoldButton(parent, label, anchor, size, tap, null);
    }

    private void AddHoldButton(Transform parent, string label, Vector2 anchor, float size,
        System.Action down, System.Action up)
    {
        var image = CreateImage(label + " Button", parent, anchor, anchor,
            new Color(0.08f, 0.12f, 0.17f, ButtonAlpha));
        image.rectTransform.sizeDelta = new Vector2(size, size);
        image.raycastTarget = true;

        var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(image.transform, false);
        var text = textObject.GetComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = Mathf.Clamp(Mathf.RoundToInt(size * 0.17f), 11, 18);
        text.color = Color.white;
        text.raycastTarget = false;
        var textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        image.gameObject.AddComponent<MobileTouchButton>().Initialize(down, up);
    }

    internal void SendMove(Vector2 value)
    {
        if (value.magnitude < joystickDeadZone)
            value = Vector2.zero;
        EventManager.TriggerEvent("OnMove", value);
    }

    internal void SendLook(Vector2 delta)
    {
        EventManager.TriggerEvent(Constants.TypedEvents.Inputs.OnLookAround, delta * lookSensitivity);
    }

    internal void SetStickHandle(Vector2 anchoredPosition)
    {
        if (_stickHandle != null)
            _stickHandle.anchoredPosition = anchoredPosition;
    }

    internal void SendTap(string eventName)
    {
        EventManager.TriggerEvent(eventName);
    }

    internal void SendHeld(string eventName, bool held)
    {
        EventManager.TriggerEvent(eventName, held);
    }

    internal void SendValue(string eventName, float value)
    {
        EventManager.TriggerEvent(eventName, value);
    }

    internal bool TryBeginStick(int pointerId)
    {
        if (_stickPointerId != int.MinValue)
            return false;
        _stickPointerId = pointerId;
        return true;
    }

    internal bool IsStickPointer(int pointerId) => _stickPointerId == pointerId;

    internal void EndStick(int pointerId)
    {
        if (!IsStickPointer(pointerId))
            return;
        _stickPointerId = int.MinValue;
        SetStickHandle(Vector2.zero);
        SendMove(Vector2.zero);
    }

    internal bool TryBeginLook(int pointerId)
    {
        if (_lookPointerId != int.MinValue)
            return false;
        _lookPointerId = pointerId;
        return true;
    }

    internal bool IsLookPointer(int pointerId) => _lookPointerId == pointerId;
    internal void EndLook(int pointerId)
    {
        if (IsLookPointer(pointerId))
            _lookPointerId = int.MinValue;
    }
}

public sealed class MobileTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private System.Action _down;
    private System.Action _up;
    private bool _pressed;

    public void Initialize(System.Action down, System.Action up)
    {
        _down = down;
        _up = up;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_pressed)
            return;
        _pressed = true;
        _down?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData) => Release();
    public void OnPointerExit(PointerEventData eventData) => Release();

    private void OnDisable() => Release();

    private void Release()
    {
        if (!_pressed)
            return;
        _pressed = false;
        _up?.Invoke();
    }
}

public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private MobileTouchControls _controls;
    private RectTransform _rect;
    private const float Radius = 68f;

    public void Initialize(MobileTouchControls controls)
    {
        _controls = controls;
        _rect = transform as RectTransform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_controls.TryBeginStick(eventData.pointerId))
            return;
        UpdateStick(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_controls.IsStickPointer(eventData.pointerId))
            UpdateStick(eventData);
    }

    public void OnPointerUp(PointerEventData eventData) => _controls.EndStick(eventData.pointerId);

    private void UpdateStick(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position,
                eventData.pressEventCamera, out var local))
            return;

        var value = Vector2.ClampMagnitude(local / Radius, 1f);
        _controls.SetStickHandle(value * Radius);
        _controls.SendMove(value);
    }
}

public sealed class MobileLookSurface : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private MobileTouchControls _controls;
    public void Initialize(MobileTouchControls controls) => _controls = controls;

    public void OnPointerDown(PointerEventData eventData)
    {
        _controls.TryBeginLook(eventData.pointerId);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_controls.IsLookPointer(eventData.pointerId))
            _controls.SendLook(eventData.delta);
    }

    public void OnPointerUp(PointerEventData eventData) => _controls.EndLook(eventData.pointerId);
}
