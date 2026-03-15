using System;
using System.Buffers;
using System.Collections.Generic;
using RealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using RealtimeCSGExtensions.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace RealtimeCSGExtensions
{
    public class RCSGExtensionWindow : EditorWindow
    {
        // Call on tab change
        public static void ForceRepaint()
        {
            if (_instance != null)
            {
                ((EditorWindow)_instance).Repaint();
            }
        }
        
        [MenuItem ("Window/Realtime-CSG Extensions")]
        public static void  ShowWindow () {
            EditorWindow.GetWindow(typeof(RCSGExtensionWindow));
        }

        private static RCSGExtensionWindow _instance;
        private void OnEnable()
        {
            this.titleContent = new GUIContent("RCSG Extension Tools");
            _instance = this;
        }

        private void OnDisable()
        {
            _instance = null;
        }

        enum Tabs { Construction, Surfaces }
        Tabs curTab = Tabs.Construction;
        private static string[] tabStrings = new[] { "Construction", "Painting" };

        private Vector2 _mainScrollPos;
        static GUILayoutOption[] _expandHeight = new[] { GUILayout.ExpandHeight(true) };

        TabStateAnchor tabStateAnchor = new();
        TabStateSurfaces tabStateSurfaces = new();
        private void Awake()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            tabStateAnchor.OnAwake_AnchorTab();   
        }

        private void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            tabStateAnchor.OnDestroy_AnchorTab();
        }

        void OnGUI ()
        {
            curTab = (Tabs)GUILayout.Toolbar((int)curTab, tabStrings);
            EditorGUILayoutUtility.HorizontalLine();
            //EditorGUILayout.Separator();
            
            // Main body
            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos, false, false, _expandHeight);
            {
                switch (curTab)
                {
                    case Tabs.Construction:
                        OnGUI_Construction();
                        break;
                    case Tabs.Surfaces:
                        OnGUI_Surfaces();
                        break;
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void OnGUI_Surfaces()
        {
            tabStateSurfaces.OnGUI();
        }
        
        
        void OnGUI_Construction()
        {
            switch (curTab)
            {
                case Tabs.Construction:
                    tabStateAnchor.OnGUI_AnchorTab();
                    break;
            }
        }
        void OnSceneGUI(SceneView sceneView)
        {
            return;
        }

        class TabStateSurfaces
        {
            enum SurfaceTabs
            {
                DirectionalPaint /* Formerly Auto Paint */, ReplaceMaterials, AlignToWorld
            }
            static string[] tabStrings = new[] { "Directional Paint", "Replace Materials", "Align To World" };
            TabReplaceMaterials _tabReplaceMaterials = new ();
            TabDirectionalPaint _tabDirectionalPaint = new ();
            TabWorldUVs _tabAlignToWorld = new ();
            
            private SurfaceTabs curTab;
            Vector2 _mainScrollPos;
            
            public void OnGUI()
            {
                // Top toolbar
                curTab = (SurfaceTabs)GUILayout.Toolbar((int)curTab, tabStrings);
                EditorGUILayout.Separator();
                
                // Title
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(tabStrings[(int)curTab], EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.Separator();
                
                _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos, false, false, _expandHeight);
                switch (curTab)
                {
                    case SurfaceTabs.ReplaceMaterials:
                        _tabReplaceMaterials.OnGUI();
                        break;
                    case SurfaceTabs.DirectionalPaint:
                        _tabDirectionalPaint.OnGUI();
                        break;
                    case SurfaceTabs.AlignToWorld:
                        _tabAlignToWorld.OnGUI();
                        break;
                }
                EditorGUILayout.EndScrollView();
            }

            class TabReplaceMaterials
            {
                List<ReplacementOperation> _replacementOperations = new();
                private Vector2 _listScrollPosition;

                static void ExecuteReplacementOperation(List<ReplacementOperation> operations, EditorWindow editorWindow = null)
                {
                    var surfaces = RealtimeCSGExtensions.RCSGExtensionUtility.GetSelectedSurfacesAlloc();
                    if (surfaces == null)
                    {
                        EditModeManager.ShowMessage($"Can't replace materials in the CSG edit mode {EditModeManager.ActiveTool.GetType().Name} (bother sam about this!)");
                        return;
                    }
                            
                    // faster access, copy into arrays, skipping nulls. we'll over-allocate sometimes if there's a null but who cares
                    int finalOperationsCount = 0;
                    Material[] finalSrcMats = ArrayPool<Material>.Shared.Rent(operations.Count);
                    Material[] finalDstMats = ArrayPool<Material>.Shared.Rent(operations.Count);
                    for (int srcIdx = 0; srcIdx < operations.Count; srcIdx++)
                    {
                        // Skip if either from or to are null
                        if (operations[srcIdx].fromMat == null || operations[srcIdx].toMat == null) { continue; }
                        
                        // Increment our final operation count, fil;l our from + to array
                        finalSrcMats[finalOperationsCount] = operations[srcIdx].fromMat.value;
                        finalDstMats[finalOperationsCount] = operations[srcIdx].toMat.value;
                        finalOperationsCount++;
                    }
                    
                    Undo.IncrementCurrentGroup();
                    using (new UndoGroup(surfaces, "Replace surface materials"))
                    {
                        try
                        {
                            for (var i = 0; i < surfaces.Length; i++)
                            {
                                var brush			= surfaces[i].brush;
                                var surfaceIndex	= surfaces[i].surfaceIndex;
                                Shape shape = brush.Shape;
                                var texGenIndex		= shape.Surfaces[surfaceIndex].TexGenIndex;

                                for (int checkMatIdx = 0; checkMatIdx < finalOperationsCount; checkMatIdx++)
                                {
                                    if (shape.TexGens[texGenIndex].RenderMaterial == finalSrcMats[checkMatIdx])
                                    {
                                        // Found mat to replace! Swap it!
                                        shape.TexGens[texGenIndex].RenderMaterial = finalDstMats[checkMatIdx];
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                    
                    ArrayPool<Material>.Shared.Return(finalSrcMats);
                    ArrayPool<Material>.Shared.Return(finalDstMats);
                }
                public void OnGUI()
                {
                    if (_replacementOperations == null) { _replacementOperations = new(); }
                    {
                        string replaceString = _replacementOperations != null && _replacementOperations.Count > 1 ? "Execute all replacements on selected surfaces" : "Execute replacement on selected surfaces";
                        if (GUILayout.Button(replaceString))
                        {
                            ExecuteReplacementOperation(_replacementOperations, SceneView.currentDrawingSceneView);
                        }
                    }
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Clear")) { _replacementOperations.Clear(); }
                    if (GUILayout.Button("Add")) { _replacementOperations.Add(new()); }
                    GUILayout.EndHorizontal();
                    if (_replacementOperations.Count <= 0) {
                        _replacementOperations.Add(new());
                    }

                    _listScrollPosition = GUILayout.BeginScrollView(_listScrollPosition, false, true);
                    for (int i = 0; i < _replacementOperations.Count; i++) {
                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        _replacementOperations[i].OnGUI();
                        //GUILayout.Button("X", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();
                }

                class ReplacementOperation
                {
                    public SurfaceUtilityEx.MaterialRef fromMat = new(), toMat = new();

                    public void OnGUI()
                    {
                        Material result;
                        
                        GUILayout.BeginHorizontal();
                        SurfaceUtilityEx.PickableMaterialField(fromMat);
                        GUILayout.Label("->");
                        SurfaceUtilityEx.PickableMaterialField(toMat);
                        GUILayout.EndHorizontal();
                        
                    }
                }
            }
            
            class TabWorldUVs
            {
                public void OnGUI()
                {
                    if(GUILayout.Button("Do it"))
                    {
                        MakeSurfacesWorldSpace();
                    }
                }
                static void MakeSurfacesWorldSpace()
                {
                    HashSet<CSGBrush> brushesToUpdate = HashSetPool<CSGBrush>.Get();
                    brushesToUpdate.Clear();

                    SelectedBrushSurface[] selSurfaces = RealtimeCSGExtensions.RCSGExtensionUtility.GetSelectedSurfacesAlloc();
                    for(int i = 0; i < selSurfaces.Length; i++)
                    {
                        int surfaceIdx = selSurfaces[i].surfaceIndex;
                        CSGBrush brush = selSurfaces[i].brush;
                        brushesToUpdate.Add(brush);
                        Shape shape = brush.Shape;
                        Matrix4x4 brushToWorld = brush.transform.localToWorldMatrix;
                        Matrix4x4 worldToBrush = brushToWorld.inverse;
                        
                        // Pull surface orientation values
                        Vector3 surfaceBinormal = shape.Surfaces[surfaceIdx].BiNormal;
                        Vector3 surfaceNormal = shape.Surfaces[surfaceIdx].Plane.normal;
                        Vector3 worldNormal = brushToWorld.MultiplyVector(surfaceNormal);
                        
                        // Find the angle between the surface's 'up' (binormal) vector, and the idealized up vector aligned with the surface planne.
                        Quaternion worldIdealizedRotation = Quaternion.LookRotation(worldNormal, Vector3.up); // Get an idealized (world-up) rotation looking out of the surface normal
                        Vector3 worldIdealizedUp = worldIdealizedRotation * Vector3.up; // Turn that into an 'up' vector
                        Vector3 surfaceIdealizedUp = worldToBrush.MultiplyVector(worldIdealizedUp); // Transform that up vector back to brush-relative space

                        float angle = -Vector3.SignedAngle(surfaceBinormal, surfaceIdealizedUp, surfaceNormal); // Calculate the angle between the surface 'up' and the idealized 'up'
                        
                        int texGenIndex = shape.Surfaces[surfaceIdx].TexGenIndex;
                        TexGen copyTexGen = shape.TexGens[texGenIndex];
                        copyTexGen.RotationAngle = angle;
                        shape.TexGens[texGenIndex] = copyTexGen;
                    }
                    foreach(var brush in brushesToUpdate)
                    {
                        InternalCSGModelManager.CheckSurfaceModifications(brush);
                    }
                    HashSetPool<CSGBrush>.Release(brushesToUpdate);
                }
            }


            class TabDirectionalPaint
            {
                private SurfaceUtilityEx.MaterialRef floor = new(), ceiling = new(), wall = new();

                static void DirectionallyPaintSurfaces(Material upMat, Material lateralMat, Material downMat)
                {
                    const float ANGLE_THRESHOLD = .6f;
                    
                    var surfaces = RealtimeCSGExtensions.RCSGExtensionUtility.GetSelectedSurfacesAlloc();
                    if (surfaces == null)
                    {
                        EditModeManager.ShowMessage($"Can't edit surfaces in the CSG edit mode {EditModeManager.ActiveTool.GetType().Name} (bother sam about this!)");
                        return;
                    }

                    // caching this to avoid the implicit unity lifetime checks in the equality operator
                    bool _hasUp = upMat != null;
                    bool _hasDown = downMat != null;
                    bool _hasLateral = lateralMat != null;

                    Undo.IncrementCurrentGroup();
                    using (new UndoGroup(surfaces, "Directional paint surface materials"))
                    {
                        Debug.Log("going for it" + surfaces.Length);
                        for (int i = 0; i < surfaces.Length; i++)
                        {
                            SelectedBrushSurface surface = surfaces[i];
                            var brush			= surface.brush;
                            var surfaceIndex	= surface.surfaceIndex;
                            
                            Vector3 normal = brush.Shape.Surfaces[surfaceIndex].Plane.normal.normalized;
                            Vector3 transformNormal = brush.transform.TransformDirection(normal);
                            // SELECT
                            bool hasMat = _hasLateral;
                            Material directionalMat = lateralMat;
                            {
                                float upness = Vector3.Dot(transformNormal, Vector3.up);
                                float downness = Vector3.Dot(transformNormal, -Vector3.up);
                                if (math.max(upness, downness) > ANGLE_THRESHOLD)
                                {
                                    if (upness > downness)
                                    {
                                        directionalMat = upMat;
                                        hasMat = _hasUp;
                                    }
                                    else
                                    {
                                        directionalMat = downMat;
                                        hasMat = _hasDown;
                                    }
                                    
                                }
                                // else, lateralmat
                            }
                            
                            // APPLY
                            if(hasMat)
                            {
                                var texGenIndex		= brush.Shape.Surfaces[surfaceIndex].TexGenIndex;
                               
                                brush.Shape.TexGens[texGenIndex].RenderMaterial = directionalMat;
                            }
                        }
                    }
                }
                
                public void OnGUI()
                {
                    if (GUILayout.Button("Paint selected surfaces"))
                    {
                        DirectionallyPaintSurfaces(floor, wall, ceiling);
                    }
                    
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                        SurfaceUtilityEx.PickableMaterialField("Ceiling", ceiling);
                    GUILayout.EndVertical();
                    
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                        SurfaceUtilityEx.PickableMaterialField("Wall", wall, GUILayout.Height(30));    
                    GUILayout.EndVertical();
                    
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                        SurfaceUtilityEx.PickableMaterialField("Floor", floor, GUILayout.Height(30));    
                    GUILayout.EndVertical();
                }
            }
            
        }
        
        [System.Serializable]
        class TabStateAnchor
        {
            // persist this state in-editor across reloads
            private float hackManualGridPos;
            private float hackManualGridRot;

            //
            EditorGUILayoutUtility.ListState anchorsListState;
            PollingComponentFinder<RCSGGridAnchor> sceneAnchorFinder;
            EditorGUILayoutUtility.ListState modelsListState;
            public void OnAwake_AnchorTab()
            {
            }

            public void OnGUI_AnchorTab()
            {
                // Grid visibility toggles //
                EditorGUILayout.BeginHorizontal();
                {
                    //GUILayout.Label("Show grids: ", GUILayout.ExpandWidth(false));
                    GUILayout.Label("Grid Visibility", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

                    bool csgGridEnabled = RealtimeCSG.CSGSettings.GridVisible;
                    bool lastUnityGridEnabled = false;
                    var views = UnityEditor.SceneView.sceneViews;
                    {
                        // TODO: don't do this every frame?
                        for (int i = 0; i < views.Count; i++)
                        {
                            if(views[i] == null) continue;
                            SceneView sceneView = views[i] as SceneView;
                            if(sceneView == null) continue;
                            if (sceneView.showGrid)
                            {
                                lastUnityGridEnabled = true;
                                break;
                            }
                        }
                    }
                    
                    // Draw visibility Settings
                    EditorGUILayout.BeginVertical();
                    {

                    }
                    const float FIXED_WIDTH = 140;
                    // RealtimeCSG.CSGSettings.GridVisible = EditorGUILayoutUtility.ToggleButton("☐ CSG Grid", "☑ CSG Grid", RealtimeCSG.CSGSettings.GridVisible, GUILayout.Width(FIXED_WIDTH));
                    // bool newUnityGridEnabled = EditorGUILayoutUtility.ToggleButton("☐ Unity Grid", "☑ Unity Grid", lastUnityGridEnabled, GUILayout.Width(FIXED_WIDTH));
                    RealtimeCSG.CSGSettings.GridVisible = GUILayout.Toggle(RealtimeCSG.CSGSettings.GridVisible, "CSG Grid", GUILayout.Width(FIXED_WIDTH));
                    bool newUnityGridEnabled = GUILayout.Toggle(lastUnityGridEnabled, "Unity Grid", GUILayout.Width(FIXED_WIDTH));
                    EditorGUILayout.EndVertical();
                    // Apply Unity visible
                    {
                        bool changedShowingSceneViewGrid = lastUnityGridEnabled != newUnityGridEnabled;
                        // apply to all if changed
                        if (changedShowingSceneViewGrid)
                        {
                            
                            for (int i = 0; i < views.Count; i++)
                            {
                                if(views[i] == null) continue;
                                SceneView sceneView = views[i] as SceneView;
                                if(sceneView == null) continue;
                                sceneView.showGrid = newUnityGridEnabled;
                            }
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Separator();
                {
                    GUILayout.BeginHorizontal();
                    const float FIXED_WIDTH = 200;
                    GUILayout.Label("Additional Settings", EditorStyles.boldLabel);
                    GUILayout.BeginVertical();
                    CSGSettings.CanDragSelectMultipleModels = GUILayout.Toggle(CSGSettings.CanDragSelectMultipleModels, "Can drag-select multiple models");
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
                EditorGUILayoutUtility.HorizontalLine();
                GUILayout.Space(16);

                // ANCHOR STUFF //
                // Mode select //
#if false
                //GlobalGridAnchor.mode = (GlobalGridAnchor.GlobalGridMode)EditorGUILayout.EnumPopup("Grid Mode:", GlobalGridAnchor.mode);
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Grid Mode: ", GUILayout.ExpandWidth(false));
                    GlobalGridAnchor.mode = (GlobalGridAnchor.GlobalGridMode)GUILayout.Toolbar((int)GlobalGridAnchor.mode, GlobalGridAnchor.GlobalGridModeStrings);
                GUILayout.EndHorizontal();
                GUILayout.Space(8);
#else
                GlobalGridAnchor.mode = GlobalGridAnchor.GlobalGridMode.Anchored;//GUILayout.Toolbar((int)GlobalGridAnchor.mode, GlobalGridAnchor.GlobalGridModeStrings);
#endif
                // Context-dependent editor //
                switch (GlobalGridAnchor.mode)
                {
                    case GlobalGridAnchor.GlobalGridMode.Origin: break;
                    case GlobalGridAnchor.GlobalGridMode.NumberPunch:
                    {
                        // TODO: add undos here
                        hackManualGridPos = EditorGUILayout.FloatField("Grid Height:", hackManualGridPos);
                        hackManualGridRot = EditorGUILayout.FloatField("Grid Rotation:", hackManualGridRot);
                        GlobalGridAnchor.HackOverrideNumberPunchPositionAndRotation(new Vector3(0, hackManualGridPos, 0), Quaternion.Euler(0, hackManualGridRot, 0));
                    } break;
                    case GlobalGridAnchor.GlobalGridMode.Anchored:
                    {
                        GUILayout.Space(4);
                        EditorGUILayout.Separator();

                        // GRID ANCHOR LIST //
                        DrawAnchorList();

                        // MODEL LIST //
                        DrawModelList();
                            
                    } break;
                }
            }
            void DrawAnchorList() {

                RCSGGridAnchor activeGridAnchor = GlobalGridAnchor.GetActiveGridAnchor();
                string activeGridAnchorName = activeGridAnchor == null ? "None [Origin]" : activeGridAnchor.gameObject.name;
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Grid Anchors", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
                        GUILayout.FlexibleSpace();
                        
                        // CREATE BUTTON //
                        GUI.enabled = activeGridAnchor != null;
                        if (GUILayout.Button("RETURN TO ORIGIN", EditorStyles.toolbarButton)) {
                            GlobalGridAnchor.SetActiveGridPivot(null);
                        }
                        GUI.enabled = true;

                        if (GUILayout.Button("CREATE", EditorStyles.toolbarButton)) {
                            GlobalGridAnchor.CreateAnchor(null);
                            sceneAnchorFinder.ForceNextUpdate();
                        }
                    }

                    GUILayout.EndHorizontal();
                }

                sceneAnchorFinder.Tick();
                EditorGUILayoutUtility.BeginList(ref anchorsListState);
                {
                    Span<RCSGGridAnchor> sceneAnchors = sceneAnchorFinder.Get();
                    Color _bgCol = GUI.backgroundColor;
                    Color _bgColSelected = Color.Lerp(_bgCol, Color.lightGreen, .3f);

                    //// Draw origin case
                    //DrawAnchorListEntry(null, activeGridAnchor == null, _bgCol, _bgColSelected, true);
                    
                    // Draw normal anchors
                    for (int i = 0; i < sceneAnchors.Length; i++) {
                        RCSGGridAnchor lGridAnchor = sceneAnchors[i];
                        bool isActive = lGridAnchor == activeGridAnchor;
                        // if (isActive && DRAW_ACTIVE_FIRST) { continue; }

                        DrawAnchorListEntry(lGridAnchor, isActive, _bgCol, _bgColSelected);
                    }
                    GUI.backgroundColor = _bgCol;
                }
                EditorGUILayoutUtility.EndList();
            }

            static void DrawAnchorListEntry(RCSGGridAnchor lGridAnchor, bool isActive, Color bgColorNormal, Color bgColorSelected) {
                if (lGridAnchor == null) { return; }

                GUI.backgroundColor = isActive ? bgColorSelected : bgColorNormal;
                EditorGUILayoutUtility.BeginListElement();
                // Highlight active
                if (isActive) {
                    GUI.enabled = false;
                    GUILayout.Label("[Active]", EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
                    GUI.enabled = true;
                }

                GUILayout.Label(lGridAnchor.gameObject.name, isActive ? EditorStyles.boldLabel : EditorStyles.label);

                // Buttons
                // TODO: buttons should be consistently sized down the entire list in a better way than this
                GUILayout.BeginHorizontal(GUILayout.Width(200));
                { 
                    //if (isActive) {
                    //    if (GUILayout.Button("Deactivate")) {
                    //        GlobalGridAnchor.SetActiveGridPivot(null);
                    //    }
                    //}
                    {
                        GUI.enabled = (!isActive);
                        if (GUILayout.Button((!isActive) ? "Activate" : "Activated")) {
                            GlobalGridAnchor.SetActiveGridPivot(lGridAnchor);
                        }
                        GUI.enabled = true;
                    }

                    if (GUILayout.Button("Find")) {
                        SceneView.lastActiveSceneView.Frame(new Bounds(lGridAnchor.transform.position, Vector3.one * 7), false);
                        Selection.activeTransform = lGridAnchor.transform;
                    }
                   
                    
                }
                GUILayout.EndHorizontal();

                EditorGUILayoutUtility.EndListElement();
            }

            void DrawModelList() {
                // GRID ANCHOR LIST //
                EditorGUILayoutUtility.BeginListWithHeader(ref modelsListState, "Scene Models");
                {
                    Span<CSGModel> sceneModels = CSGModelManager.GetAllModels().AsSpan();
                    CSGModel activeModel = SelectionUtility.LastUsedModel;

                    Color _bgCol = GUI.backgroundColor;
                    Color _bgColSelected = Color.Lerp(_bgCol, Color.lightGreen, .3f);
                    for (int i = 0; i < sceneModels.Length; i++) {
                        CSGModel eModel = sceneModels[i];
                        bool isActive = eModel == activeModel;

                        GUI.backgroundColor = isActive ? _bgColSelected : _bgCol;
                        DrawModelListEntry(eModel, isActive);
                    }
                    GUI.backgroundColor = _bgCol;
                }
                EditorGUILayoutUtility.EndList();
            }
            static void DrawModelListEntry(CSGModel eModel, bool isActive) {
                if (eModel == null) { return; }
                string eModelName = eModel.gameObject.name;
                if(eModelName.StartsWith("[default-CSGModel]")) { return; }
                EditorGUILayoutUtility.BeginListElement();
                
                // Highlight active
                if (isActive) {
                    GUI.enabled = false;
                    // TODO: make renamable
                    GUILayout.Label("[Active]", EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
                    GUI.enabled = true;
                }

                GUILayout.Label(eModelName, isActive ? EditorStyles.boldLabel : EditorStyles.label);

                // TODO: buttons should be consistently sized down the entire list, this is a rough way of doing it
                GUILayout.BeginHorizontal(GUILayout.Width(200));
                {
                    // Buttons
                    GUI.enabled = !isActive;
                    if (GUILayout.Button(!isActive ? "Select" : "Selected")) {
                        Selection.activeGameObject = eModel.gameObject;
                        SelectionUtility.LastUsedModel = eModel;
                    }
                    GUI.enabled = true;
                    
                    if (GUILayout.Button("Find")) {
                        const float SIZE = 30;
                        SceneView.lastActiveSceneView.Frame(new Bounds(eModel.transform.position, new(SIZE, SIZE, SIZE)), false);
                        Selection.activeTransform = eModel.transform;
                    }
                }
                GUILayout.EndHorizontal();
                
                EditorGUILayoutUtility.EndListElement();
            }
            public void OverlayOnSceneGUI()
            {
                
            }

            public void OnDestroy_AnchorTab()
            {
                
            }

            void AddItemsToSceneHeaderContextMenu(GenericMenu menu, Scene scene)
            {
            }
        }


        public static void FlagForRepaint()
        {
            // TODO: make more robust
            GetWindow<RCSGExtensionWindow>()?.Repaint();
        }
    }
    
    namespace Editor {
        struct PollingComponentFinder<T> where T : UnityEngine.Object
        {
            T[] cache;
            double lastUpdateTime;
            public void ForceNextUpdate() {
                lastUpdateTime = -1;
            }
            // TODO: this is garbage:
            public bool Tick() {
                const double UPDATE_INTERVAL = .5;
                double curTime = EditorApplication.timeSinceStartup;
                if (cache == null || curTime - lastUpdateTime > UPDATE_INTERVAL) {
                    lastUpdateTime = curTime;
                    cache = GameObject.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
                    return true;
                }
                return false;
            }

            public Span<T> Get() {
                if(cache == null) { return default; }
                return cache.AsSpan();
            }

        }


        // TODO: move this somewhere?
        // https://gamedev.stackexchange.com/questions/167946/unity-editor-horizontal-line-in-inspector
        public static partial class EditorGUILayoutUtility
        {
            // such a stupid hack
            public static void BeginVisible(bool makeVisible) {
                Rect clipRect = makeVisible ? new(0, 0, float.MaxValue, float.MaxValue) : new(0, 0, 0, 0);
                GUI.BeginClip(clipRect);
            }
            public static void EndVisible() {
                GUI.EndClip();
            }

            public static bool ToggleButton(string labelActivate, string labelDeactivate, bool curVal, params GUILayoutOption[] options) {
                return ToggleButton(labelActivate, labelDeactivate, curVal, null, options);
            }
            public static bool ToggleButton(string labelActivate, string labelDeactivate, bool curVal, GUIStyle style = null, params GUILayoutOption[] options)
            {
                if(style == null) { style = GUI.skin.button; }
                string label = curVal ? labelDeactivate : labelActivate;
                if (GUILayout.Button(label, style, options))
                {
                    curVal = !curVal;
                }
                return curVal;
            }
        
            public static readonly Color DEFAULT_COLOR = new Color(0f, 0f, 0f, 0.3f);
            public static readonly Vector2 DEFAULT_LINE_MARGIN = new Vector2(2f, 2f);

            public const float DEFAULT_LINE_HEIGHT = 1f;

            public static void HorizontalLine(Color color, float height, Vector2 margin)
            {
                GUILayout.Space(margin.x);

                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, height), color);

                GUILayout.Space(margin.y);
            }
            public static void HorizontalLine(Color color, float height) => EditorGUILayoutUtility.HorizontalLine(color, height, DEFAULT_LINE_MARGIN);
            public static void HorizontalLine(Color color, Vector2 margin) => EditorGUILayoutUtility.HorizontalLine(color, DEFAULT_LINE_HEIGHT, margin);
            public static void HorizontalLine(float height, Vector2 margin) => EditorGUILayoutUtility.HorizontalLine(DEFAULT_COLOR, height, margin);

            public static void HorizontalLine(Color color) => EditorGUILayoutUtility.HorizontalLine(color, DEFAULT_LINE_HEIGHT, DEFAULT_LINE_MARGIN);
            public static void HorizontalLine(float height) => EditorGUILayoutUtility.HorizontalLine(DEFAULT_COLOR, height, DEFAULT_LINE_MARGIN);
            public static void HorizontalLine(Vector2 margin) => EditorGUILayoutUtility.HorizontalLine(DEFAULT_COLOR, DEFAULT_LINE_HEIGHT, margin);

            public static void HorizontalLine() => EditorGUILayoutUtility.HorizontalLine(DEFAULT_COLOR, DEFAULT_LINE_HEIGHT, DEFAULT_LINE_MARGIN);

#if UNITY_EDITOR
#endif
        }
    }

}