using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace MalumMenu;

public class MenuUI : MonoBehaviour
{
    public static int windowHeight = 840;
    public static int windowWidth = 1280;
    private Rect _windowRect;

    public static bool isGUIActive = false;
    private static bool hasShownLoadingScreen = false;
    private bool isShowingLoadingScreen = false;
    private float loadingScreenTimer = 0f;
    private const float LOADING_SCREEN_DURATION = 3.5f;
    private Rect _loadingScreenRect;
    private List<Vector2> _fallingDots = new();
    private List<ITab> _tabs = new();
    private int _selectedTab;
    private int _lastSelectedTab = -1;
    private float _contentFade = 1f;
    public static float hue; // For RGB mode

    private void Start()
    {
        // Add all tabs on start
        _tabs.Add(new MovementTab());
        _tabs.Add(new ESPTab());
        _tabs.Add(new RolesTab());
        _tabs.Add(new ShipTab());
        _tabs.Add(new ChatTab());
        _tabs.Add(new AnimationsTab());
        _tabs.Add(new OverloadTab());
        _tabs.Add(new ConsoleTab());
        _tabs.Add(new HostOnlyTab());
        _tabs.Add(new PassiveTab());
        _tabs.Add(new ModesTab());
        _tabs.Add(new ConfigTab());
        _tabs.Add(new TeleportTab());
        // _tabs.Add(new HideNSeekTab());

        // Instantiate 2D area of MenuUI
        _windowRect = new(
            Screen.width / 2f - windowWidth / 2f,
            Screen.height / 2f - windowHeight / 2f,
            windowWidth,
            windowHeight
        );

        // Loading screen rect (400x400)
        _loadingScreenRect = new(
            Screen.width / 2f - 200f,
            Screen.height / 2f - 200f,
            400f,
            400f
        );

        // Initialize falling dots
        for (int i = 0; i < 15; i++)
        {
            _fallingDots.Add(new Vector2(Random.Range(_loadingScreenRect.x, _loadingScreenRect.x + _loadingScreenRect.width), 
                                         Random.Range(_loadingScreenRect.y - 50f, _loadingScreenRect.y)));
        }
    }

    public void InitStyles()
    {
        GUI.skin.toggle.fontSize = GUI.skin.button.fontSize = GUI.skin.label.fontSize = 13;
        GUI.skin.toggle.padding = new RectOffset { left = 6, right = 6, top = 4, bottom = 4 };
        GUI.skin.toggle.normal.textColor = Color.white;
        GUI.skin.toggle.onNormal.textColor = Color.green;
        GUI.skin.toggle.active.textColor = Color.green;
        GUI.skin.toggle.onActive.textColor = Color.green;
        GUI.skin.toggle.normal.background = Texture2D.grayTexture;
        GUI.skin.toggle.onNormal.background = Texture2D.whiteTexture;
        GUI.skin.toggle.active.background = Texture2D.grayTexture;
        GUI.skin.toggle.onActive.background = Texture2D.whiteTexture;
    }

    private void Update()
    {

        if (Input.GetKeyDown(Utils.StringToKeycode(MalumMenu.menuKeybind.Value)))
        {
            if (!isGUIActive)
            {
                // Show loading screen only on first menu open
                if (!hasShownLoadingScreen)
                {
                    isShowingLoadingScreen = true;
                    loadingScreenTimer = 0f;
                    hasShownLoadingScreen = true;
                    isGUIActive = false; // Hide main menu initially
                }
                else
                {
                    // Skip loading screen on subsequent opens
                    isGUIActive = true;
                }
            }
            else
            {
                // Close menu normally
                isGUIActive = false;
                isShowingLoadingScreen = false;
            }

            if (MalumMenu.menuOpenOnMouse.Value && isShowingLoadingScreen)
            {
                // Teleport the window to the mouse for immediate use
                Vector2 mousePosition = Input.mousePosition;
                _windowRect.position = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
            }
        }

        // Update loading screen timer
        if (isShowingLoadingScreen)
        {
            loadingScreenTimer += Time.deltaTime;
            if (loadingScreenTimer >= LOADING_SCREEN_DURATION)
            {
                isShowingLoadingScreen = false;
                isGUIActive = true; // Show main menu after loading
            }
        }

        if (CheatToggles.rgbMode)
        {
            hue += Time.deltaTime * 0.12f; // slower rainbow speed
            if (hue > 1f) hue -= 1f; // Loop hue back to 0 when it exceeds 1
        }

        if (CheatToggles.stealthMode != MalumMenu.inStealthMode)
        {
            MalumMenu.inStealthMode = CheatToggles.stealthMode;

            Scene scene = SceneManager.GetActiveScene();

            if (scene.name == "MainMenu" || scene.name == "MatchMaking")
            {
                SceneManager.LoadScene(scene.name);
            }
        }

        if (CheatToggles.panicMode) Utils.Panic();

        var stamp = ModManager.Instance.ModStamp;
        if (stamp) stamp.enabled = !(MalumMenu.inStealthMode || MalumMenu.isPanicked);

        if (CheatToggles.openConfig)
        {
            Utils.OpenConfigFile();
            CheatToggles.openConfig = false;
        }

        if (CheatToggles.reloadConfig)
        {
            MalumMenu.Plugin.Config.Reload();
            CheatToggles.reloadConfig = false;
        }

        if (CheatToggles.saveProfile)
        {
            CheatToggles.saveProfile = false; // Disable first to avoid saving it to profile
            CheatToggles.SaveTogglesToProfile();
        }

        if (CheatToggles.loadProfile)
        {
            CheatToggles.LoadTogglesFromProfile();
            CheatToggles.loadProfile = false;
        }

        // Some cheats only work if the LocalPlayer exists, so they are turned off if it does not
        if(!Utils.isPlayer)
        {
            CheatToggles.setFakeRole = false;
            CheatToggles.setFakeAlive = false;
            CheatToggles.killAll = false;
            CheatToggles.telekillPlayer = false;
            CheatToggles.killAllCrew = false;
            CheatToggles.killAllImps = false;
            CheatToggles.teleportPlayer = false;
            CheatToggles.spectate = false;
            CheatToggles.freecam = false;
            CheatToggles.killPlayer = false;
            CheatToggles.callMeeting = false;

            if (CheatToggles.runOverload)
            {
                OverloadUI.StopOverload();
                OverloadHandler.ClearCustomTargets();
            }
        }

        // Some cheats only work if the ship exists, so they are turned off if it does not
        if(!Utils.isShip)
        {
            CheatToggles.sabotageMap = false;
            CheatToggles.unfixableLights = false;
            CheatToggles.completeMyTasks = false;
            CheatToggles.kickVents = false;
            CheatToggles.reportBody = false;
            CheatToggles.closeMeeting = false;
            CheatToggles.reactorSab = false;
            CheatToggles.oxygenSab = false;
            CheatToggles.commsSab = false;
            CheatToggles.elecSab = false;
            CheatToggles.mushSab = false;
            CheatToggles.closeAllDoors = false;
            CheatToggles.openAllDoors = false;
            CheatToggles.spamCloseAllDoors = false;
            CheatToggles.spamOpenAllDoors = false;
            CheatToggles.mushSpore = false;

            MalumCheats.StopShipAnimCheats();
        }

        if(!Utils.isHost && !Utils.isFreePlay)
        {
            CheatToggles.killAll = false;
            CheatToggles.telekillPlayer = false;
            CheatToggles.killAllCrew = false;
            CheatToggles.killAllImps = false;
            CheatToggles.killPlayer = false;
            CheatToggles.ejectPlayer = false;
            CheatToggles.noKillCd = false;
            CheatToggles.killAnyone = false;
            CheatToggles.killVanished = false;
            CheatToggles.forceStartGame = false;
            CheatToggles.skipMeeting = false;
            CheatToggles.voteImmune = false;
            CheatToggles.noGameEnd = false;
            CheatToggles.showProtectMenu = false;
            CheatToggles.showRolesMenu = false;
            CheatToggles.noOptionsLimits = false;
        }

        // Some cheats only work if in a meeting, so they are turned off if it does not
        if (!Utils.isMeeting)
        {
            CheatToggles.skipMeeting = false;
            CheatToggles.ejectPlayer = false;
        }
    }

    public void OnGUI()
    {
        if (isShowingLoadingScreen)
        {
            DrawLoadingScreen();
            return;
        }

        if (!isGUIActive || MalumMenu.isPanicked) return;

        InitStyles();

        Color uiColor = UIHelpers.GetUIColor();
        Color windowBg = new Color(uiColor.r, uiColor.g, uiColor.b, 0.88f);
        Color previousBackground = GUI.backgroundColor;
        GUI.backgroundColor = windowBg;

        _windowRect = GUI.Window((int)WindowId.MenuUI, _windowRect, (GUI.WindowFunction)WindowFunction, "MalumMenu Unofficial v" + MalumMenu.malumVersion, GUIStylePreset.Window);

        GUI.backgroundColor = previousBackground;
    }

    private void DrawLoadingScreen()
    {

        
        // Loading screen background with dark color
        Color uiColor = UIHelpers.GetUIColor();
        Color loadingBg = new Color(uiColor.r, uiColor.g, uiColor.b, 0.95f);
        GUI.backgroundColor = loadingBg;
        GUI.Box(_loadingScreenRect, "", GUIStylePreset.Window);

        // Update and draw falling dots
        float dotSpeed = 150f;
        Color prevColor = GUI.color;
        for (int i = 0; i < _fallingDots.Count; i++)
        {
            Vector2 dotPos = _fallingDots[i];
            dotPos.y += dotSpeed * Time.deltaTime;

            // Reset dot to top when it falls off
            if (dotPos.y > _loadingScreenRect.y + _loadingScreenRect.height + 20f)
            {
                dotPos.y = _loadingScreenRect.y - 20f;
                dotPos.x = Random.Range(_loadingScreenRect.x + 20f, _loadingScreenRect.x + _loadingScreenRect.width - 20f);
            }
            _fallingDots[i] = dotPos;

            // Draw blurred white dot with glow effect
            float dotDiameter = Random.Range(4.5f, 8.5f);
            float dotRadius = dotDiameter * 0.5f;

            // Add horizontal drift  so its not line you know
            float drift = Mathf.Sin(Time.time * (0.7f + i * 0.05f) + i) * (8f + i * 0.2f);
            dotPos.x += drift * Time.deltaTime;
            dotPos.x = Mathf.Clamp(dotPos.x, _loadingScreenRect.x + 10f, _loadingScreenRect.x + _loadingScreenRect.width - 10f);
            _fallingDots[i] = dotPos;

            // Blur layers
            for (int blur = 3; blur >= 1; blur--)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.15f / blur);

                float blurDiameter = dotDiameter + blur * 2f;
                float blurRadius = blurDiameter * 0.5f;

                Rect blurRect = new Rect(dotPos.x - blurRadius, dotPos.y - blurRadius, blurDiameter, blurDiameter);
                GUI.Box(blurRect, "", GUIStylePreset.Separator);
            }

            // Main white dot
            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            Rect dotRect = new Rect(dotPos.x - dotRadius, dotPos.y - dotRadius, dotDiameter, dotDiameter);
            GUI.Box(dotRect, "", GUIStylePreset.Separator);
        }
        GUI.color = prevColor;

        // Loading screen content - centered
        GUILayout.BeginArea(_loadingScreenRect);
        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();

        // Center title and version
        GUIStyle centerStyle = new GUIStyle(GUIStylePreset.TabTitle)
        {
            alignment = TextAnchor.MiddleCenter
        };
        GUIStyle centerSubtitle = new GUIStyle(GUIStylePreset.TabSubtitle)
        {
            alignment = TextAnchor.MiddleCenter
        };

        GUILayout.Label("MalumMenu", centerStyle);
        GUILayout.Space(5f);
        GUILayout.Label("v" + MalumMenu.malumVersion, centerSubtitle);

        GUILayout.Space(25f);

        // Loading animation (rotating dots)
        float progress = (loadingScreenTimer / LOADING_SCREEN_DURATION);
        string loadingText = "Loading" + new string('.', (int)(progress * 4f) % 4);
        GUILayout.Label(loadingText, centerSubtitle);

        GUILayout.Space(12f);

        // Progress bar with percentage
        GUILayout.BeginHorizontal();
        GUILayout.Space(10f);
        Rect barRect = GUILayoutUtility.GetRect(300f, 12f);
        GUI.Box(barRect, "");
        Rect barFill = barRect;
        barFill.width = barRect.width * progress;
        GUI.Box(barFill, "", GUIStylePreset.Separator);
        GUILayout.EndHorizontal();

        GUILayout.Space(8f);

        // Percentage text - centered
        int percentage = Mathf.RoundToInt(progress * 100f);
        GUILayout.Label(percentage + "%", centerSubtitle);

        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    public void WindowFunction(int windowID)
    {
        if (_selectedTab != _lastSelectedTab)
        {
            _lastSelectedTab = _selectedTab;
            _contentFade = 0f;
        }

        _contentFade = Mathf.Clamp01(_contentFade + Time.deltaTime * 1.1f);
        float contentAlpha = Mathf.Lerp(0.5f, 1f, _contentFade);

        GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.Space(8f);

        GUILayout.BeginHorizontal();
        GUILayout.Space(8f);
        GUILayout.FlexibleSpace();
        GUILayout.Label("Hotkey: " + MalumMenu.menuKeybind.Value, GUIStylePreset.TabSubtitle);
        GUILayout.Space(8f);
        GUILayout.EndHorizontal();

        GUILayout.Space(10f);
        Color previousBackground = GUI.backgroundColor;
        Color previousColor = GUI.color;
        Color uiColor = UIHelpers.GetUIColor();
        Color tabBg = new Color(uiColor.r, uiColor.g, uiColor.b, 0.14f);
        
        // Apply fade to both tab row and content area
        GUI.color = new Color(1f, 1f, 1f, contentAlpha);
        GUI.backgroundColor = tabBg;
        GUILayout.BeginVertical(GUIStylePreset.TabRow, GUILayout.ExpandWidth(true));

        int columns = _tabs.Count;
        int rows = 1;
        for (int row = 0; row < rows; row++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            int start = row * columns;
            int end = Mathf.Min(_tabs.Count, start + columns);
            for (int i = start; i < end; i++)
            {
                if (_selectedTab == i)
                {
                    GUI.backgroundColor = new Color(0.18f, 0.35f, 0.72f, 0.92f);
                    if (GUILayout.Button(_tabs[i].name, GUIStylePreset.TabButtonSelected, GUILayout.Height(40f), GUILayout.Width(92f)))
                        _selectedTab = i;
                }
                else
                {
                    GUI.backgroundColor = new Color(0.15f, 0.15f, 0.18f, 0.78f);
                    if (GUILayout.Button(_tabs[i].name, GUIStylePreset.TabButton, GUILayout.Height(40f), GUILayout.Width(92f)))
                        _selectedTab = i;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (row < rows - 1)
                GUILayout.Space(4f);
        }

        GUILayout.Space(8f);
        GUILayout.EndVertical();

        GUILayout.Space(10f);
        Color previousContentBackground = GUI.backgroundColor;
        Color contentBg = new Color(uiColor.r, uiColor.g, uiColor.b, 0.1f);
        GUI.backgroundColor = contentBg;
        GUILayout.BeginVertical(GUIStylePreset.ContentBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.Space(6f);

        if (_selectedTab >= 0 && _selectedTab < _tabs.Count)
        {
            GUILayout.Label(_tabs[_selectedTab].name, GUIStylePreset.TabTitle);
            GUILayout.Space(4f);
            GUILayout.Box("", GUIStylePreset.Separator, GUILayout.Height(1f), GUILayout.ExpandWidth(true));
            GUILayout.Space(8f);
            _tabs[_selectedTab].Draw();
        }

        GUILayout.Space(6f);
        GUILayout.EndVertical();
        GUI.color = previousColor;
        GUI.backgroundColor = previousBackground;

        GUILayout.Space(8f);
        GUILayout.EndVertical();

        GUI.DragWindow();
    }
}
