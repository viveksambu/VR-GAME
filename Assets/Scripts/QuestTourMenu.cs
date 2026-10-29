using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QuestTourMenu : MonoBehaviour
{
    private readonly string[] locationNames =
    {
        "Acad Block",
        "Entrance Steps",
        "Waiting Area"
    };

    private readonly int[] panoramaIndexes = { 0, 1, 2 };

    private PanoramaTour tour;
    private GameObject hudRoot;
    private GameObject menuRoot;
    private Button[] locationButtons;
    private InputAction toggleAction;
    private InputAction menuStickAction;
    private InputAction selectAction;
    private InputAction rightPositionAction;
    private InputAction rightRotationAction;
    private LineRenderer controllerPointer;
    private Button pointedButton;
    private bool stickHeld;
    private int selectedIndex;

    void Awake()
    {
        tour = GetComponent<PanoramaTour>();
        CreateControllerPointer();
        CreateHud();
        CreateMenu();
        ConfigureInput();
    }

    void OnDestroy()
    {
        toggleAction?.Dispose();
        menuStickAction?.Dispose();
        selectAction?.Dispose();
        rightPositionAction?.Dispose();
        rightRotationAction?.Dispose();
    }

    void Update()
    {
        UpdateControllerPointer();

        if (menuRoot == null || !menuRoot.activeSelf)
            return;

        float vertical = menuStickAction.ReadValue<Vector2>().y;
        if (!stickHeld)
        {
            if (vertical >= 0.75f)
            {
                SelectButton((selectedIndex - 1 + locationButtons.Length) % locationButtons.Length);
                stickHeld = true;
            }
            else if (vertical <= -0.75f)
            {
                SelectButton((selectedIndex + 1) % locationButtons.Length);
                stickHeld = true;
            }
        }
        else if (Mathf.Abs(vertical) < 0.35f)
        {
            stickHeld = false;
        }
    }

    private void ConfigureInput()
    {
        // Y is the Secondary button on the left Quest Touch controller.
        toggleAction = new InputAction("Toggle Tour Menu", InputActionType.Button);
        toggleAction.AddBinding("<XRController>{LeftHand}/{SecondaryButton}");
        toggleAction.AddBinding("<Keyboard>/m");
        toggleAction.performed += _ => ToggleMenu();
        toggleAction.Enable();

        menuStickAction = new InputAction("Menu Navigation", InputActionType.Value);
        menuStickAction.AddBinding("<XRController>/{Primary2DAxis}");
        menuStickAction.Enable();

        selectAction = new InputAction("Choose Tour Location", InputActionType.Button);
        selectAction.AddBinding("<XRController>{RightHand}/{PrimaryButton}");
        selectAction.AddBinding("<XRController>{RightHand}/triggerPressed");
        selectAction.AddBinding("<Keyboard>/enter");
        selectAction.performed += _ =>
        {
            if (!ClickPointedButton())
                ChooseSelectedLocation();
        };
        selectAction.Enable();

        rightPositionAction = new InputAction("Right Controller Aim Position", InputActionType.Value);
        rightPositionAction.AddBinding("<XRController>{RightHand}/pointerPosition");
        rightPositionAction.Enable();

        rightRotationAction = new InputAction("Right Controller Aim Rotation", InputActionType.Value);
        rightRotationAction.AddBinding("<XRController>{RightHand}/pointerRotation");
        rightRotationAction.Enable();
    }

    private void CreateControllerPointer()
    {
        GameObject pointerObject = new GameObject("Right Controller UI Pointer");
        pointerObject.transform.SetParent(transform, false);
        controllerPointer = pointerObject.AddComponent<LineRenderer>();
        controllerPointer.useWorldSpace = true;
        controllerPointer.positionCount = 2;
        controllerPointer.startWidth = 0.004f;
        controllerPointer.endWidth = 0.002f;
        controllerPointer.startColor = new Color(0.2f, 0.85f, 1f, 0.95f);
        controllerPointer.endColor = new Color(0.2f, 0.85f, 1f, 0.2f);
        controllerPointer.material = new Material(Shader.Find("Sprites/Default"));
        controllerPointer.enabled = false;
    }

    private void CreateHud()
    {
        Camera playerCamera = Camera.main;
        hudRoot = CreateCanvasRoot("Tour Controls", playerCamera, new Vector3(0f, -0.1f, 1.5f), new Vector2(1800f, 1000f));

        Button previous = CreateControlButton(hudRoot.transform, "Previous", new Vector2(700f, 180f), new Vector2(320f, 100f));
        previous.onClick.AddListener(tour.PreviousSpot);

        Button next = CreateControlButton(hudRoot.transform, "Next", new Vector2(700f, 0f), new Vector2(320f, 100f));
        next.onClick.AddListener(tour.NextSpot);

        Button menu = CreateControlButton(hudRoot.transform, "Menu", new Vector2(700f, -180f), new Vector2(320f, 100f));
        menu.onClick.AddListener(ToggleMenu);
    }

    private void CreateMenu()
    {
        menuRoot = CreateCanvasRoot("Quest Tour Menu", Camera.main, new Vector3(0f, 0f, 1.7f), new Vector2(760f, 760f));

        GameObject panel = CreateUiObject("Panel", menuRoot.transform, new Color(1f, 0.78f, 0.12f, 0.94f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        locationButtons = new Button[locationNames.Length];
        for (int i = 0; i < locationNames.Length; i++)
        {
            int buttonIndex = i;
            Button button = CreateLocationButton(panel.transform, locationNames[i], i);
            button.onClick.AddListener(() => ChooseLocation(buttonIndex));
            locationButtons[i] = button;
        }

        menuRoot.SetActive(false);
    }

    private GameObject CreateCanvasRoot(string name, Camera playerCamera, Vector3 position, Vector2 size)
    {
        if (playerCamera == null)
        {
            Debug.LogError("Quest menu could not find the XR camera.");
            return new GameObject(name);
        }

        GameObject canvasRoot = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasRoot.transform.SetParent(playerCamera.transform, false);
        canvasRoot.transform.localPosition = position;
        canvasRoot.transform.localRotation = Quaternion.identity;
        canvasRoot.transform.localScale = Vector3.one * 0.00115f;

        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = playerCamera;
        canvasRoot.GetComponent<RectTransform>().sizeDelta = size;
        return canvasRoot;
    }

    private Button CreateLocationButton(Transform parent, string label, int index)
    {
        GameObject buttonObject = CreateUiObject(label, parent, new Color(1f, 0.82f, 0.18f, 1f));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(45f, -90f - index * 105f);
        rect.sizeDelta = new Vector2(330f, 78f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.7f, 0.9f, 1f, 1f);
        colors.selectedColor = new Color(0.7f, 0.9f, 1f, 1f);
        button.colors = colors;
        AddButtonCollider(buttonObject, rect.sizeDelta);

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 34;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;

        return button;
    }

    private GameObject CreateUiObject(string name, Transform parent, Color color)
    {
        GameObject uiObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        uiObject.transform.SetParent(parent, false);
        uiObject.GetComponent<Image>().color = color;
        return uiObject;
    }

    private Button CreateControlButton(Transform parent, string label, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = CreateUiObject(label, parent, new Color(1f, 0.82f, 0.18f, 0.95f));
        buttonObject.layer = 5;
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Button button = buttonObject.AddComponent<Button>();
        AddButtonCollider(buttonObject, size);

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        Text text = textObject.GetComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 38;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        return button;
    }

    private void AddButtonCollider(GameObject buttonObject, Vector2 size)
    {
        buttonObject.layer = 5;
        BoxCollider collider = buttonObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(size.x, size.y, 8f);
    }

    private void UpdateControllerPointer()
    {
        if (controllerPointer == null || rightPositionAction == null || rightPositionAction.activeControl == null)
            return;

        Vector3 worldPosition = transform.TransformPoint(rightPositionAction.ReadValue<Vector3>());
        Quaternion worldRotation = transform.rotation * rightRotationAction.ReadValue<Quaternion>();
        Ray ray = new Ray(worldPosition, worldRotation * Vector3.forward);
        controllerPointer.enabled = true;
        controllerPointer.SetPosition(0, ray.origin);
        controllerPointer.SetPosition(1, ray.origin + ray.direction * 5f);

        pointedButton = null;
        if (Physics.Raycast(ray, out RaycastHit hit, 5f, 1 << 5))
        {
            pointedButton = hit.collider.GetComponent<Button>();
            controllerPointer.SetPosition(1, hit.point);
        }
    }

    private bool ClickPointedButton()
    {
        if (pointedButton == null || !pointedButton.gameObject.activeInHierarchy)
            return false;

        pointedButton.onClick.Invoke();
        return true;
    }

    private void ToggleMenu()
    {
        bool open = !menuRoot.activeSelf;
        menuRoot.SetActive(open);
        tour.controllerNavigationEnabled = !open;
        stickHeld = false;

        if (open)
            SelectButton(0);
    }

    private void SelectButton(int index)
    {
        selectedIndex = index;
        for (int i = 0; i < locationButtons.Length; i++)
        {
            Image image = locationButtons[i].GetComponent<Image>();
            image.color = i == selectedIndex
                ? new Color(1f, 0.58f, 0.02f, 1f)
                : new Color(1f, 0.82f, 0.18f, 1f);
        }
    }

    private void ChooseSelectedLocation()
    {
        if (menuRoot != null && menuRoot.activeSelf)
            locationButtons[selectedIndex].onClick.Invoke();
    }

    private void ChooseLocation(int buttonIndex)
    {
        tour.ShowLocation(panoramaIndexes[buttonIndex]);
        menuRoot.SetActive(false);
        tour.controllerNavigationEnabled = true;
    }
}
