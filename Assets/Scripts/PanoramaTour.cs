
using UnityEngine;
using UnityEngine.InputSystem;

public class PanoramaTour : MonoBehaviour
{
    [Header("Assign your PhotoSkybox material")]
    public Material skyboxMaterial;

    [Header("Add your 360 photos in order")]
    public Texture2D[] panoramas;

    [Header("Starting location")]
    public int currentIndex = 0;

    [Header("Quest controller navigation")]
    [Range(0.5f, 1f)] public float joystickThreshold = 0.75f;
    [HideInInspector] public bool controllerNavigationEnabled = true;

    private Material runtimeSkybox;
    private InputAction joystickAction;
    private bool joystickHeld;

    void OnEnable()
    {
        // The Primary2DAxis is the thumbstick on Quest Touch controllers.
        // This binding accepts either hand, so the user can use either stick.
        joystickAction = new InputAction("Panorama Navigation", InputActionType.Value);
        joystickAction.AddBinding("<XRController>/{Primary2DAxis}");
        joystickAction.Enable();
    }

    void OnDisable()
    {
        joystickAction?.Disable();
        joystickAction?.Dispose();
        joystickAction = null;
    }

    void Start()
    {
        if (skyboxMaterial == null || panoramas == null ||
            panoramas.Length == 0)
        {
            Debug.LogError("Assign a skybox and panorama images.");
            return;
        }

        runtimeSkybox = new Material(skyboxMaterial);
        RenderSettings.skybox = runtimeSkybox;

        currentIndex = Mathf.Clamp(
            currentIndex, 0, panoramas.Length - 1);

        ShowLocation(currentIndex);

        if (GetComponent<QuestTourMenu>() == null)
            gameObject.AddComponent<QuestTourMenu>();
    }

    void Update()
    {
        if (!controllerNavigationEnabled || joystickAction == null)
            return;

        float horizontal = joystickAction.ReadValue<Vector2>().x;

        // Change once per deliberate left/right push; centre the stick before
        // changing panoramas again.
        if (!joystickHeld)
        {
            if (horizontal >= joystickThreshold)
            {
                NextSpot();
                joystickHeld = true;
            }
            else if (horizontal <= -joystickThreshold)
            {
                PreviousSpot();
                joystickHeld = true;
            }
        }
        else if (Mathf.Abs(horizontal) < joystickThreshold * 0.5f)
        {
            joystickHeld = false;
        }
    }

    public void NextSpot()
    {
        if (panoramas == null || panoramas.Length == 0) return;

        currentIndex = (currentIndex + 1) % panoramas.Length;
        ShowLocation(currentIndex);
    }

    public void PreviousSpot()
    {
        if (panoramas == null || panoramas.Length == 0) return;

        currentIndex = (currentIndex - 1 + panoramas.Length)
                       % panoramas.Length;

        ShowLocation(currentIndex);
    }

    public void ShowLocation(int index)
    {
        if (runtimeSkybox == null || panoramas == null ||
            index < 0 || index >= panoramas.Length ||
            panoramas[index] == null)
            return;

        currentIndex = index;
        runtimeSkybox.SetTexture("_MainTex", panoramas[index]);
        DynamicGI.UpdateEnvironment();
    }
}
