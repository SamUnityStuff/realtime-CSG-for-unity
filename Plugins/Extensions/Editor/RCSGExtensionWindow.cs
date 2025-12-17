using System;
using System.Buffers;
using System.Collections.Generic;
using RealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSGExtensions.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
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
            _instance = this;
        }

        private void OnDisable()
        {
            _instance = null;
        }

        enum Tabs
        {
            Anchors
        }

        private string[] tabStrings = new[] { "Grid Anchors" };
        Tabs curTab = Tabs.Anchors;
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
            switch (RealtimeCSG.CSGSettings.EditMode)
            {
                case ToolEditMode.Surfaces:
                    OnGUI_Surfaces();
                    break;
                case ToolEditMode.Edit:
                case ToolEditMode.Generate:
                case ToolEditMode.Place:
                    OnGUI_Construction();
                    break;
                default:
                    break;
            }
            
        }

        void OnGUI_Surfaces()
        {
            tabStateSurfaces.OnGUI();
        }
        
        
        void OnGUI_Construction()
        {
            curTab = (Tabs)GUILayout.Toolbar((int)curTab, tabStrings);
            EditorGUILayout.Separator();
            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos, false, false, _expandHeight);
            switch (curTab)
            {
                case Tabs.Anchors:
                    tabStateAnchor.OnGUI_AnchorTab();
                    break;
            }
            EditorGUILayout.EndScrollView();
        }
        void OnSceneGUI(SceneView sceneView)
        {
            return;
        }

        class TabStateSurfaces
        {
            enum SurfaceTabs
            {
                ReplaceMaterials, DirectionalPaint
            }
            static string[] tabStrings = new[] { "Replace Materials", "Directional Paint" };
            TabReplaceMaterials _tabReplaceMaterials = new ();
            TabDirectionalPaint _tabDirectionalPaint = new ();
            
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
                }
                EditorGUILayout.EndScrollView();
            }

            class TabReplaceMaterials
            {
                List<ReplacementOperation> _replacementOperations = new();
                private Vector2 _listScrollPosition;

                static void ExecuteReplacementOperation(List<ReplacementOperation> operations, EditorWindow editorWindow = null)
                {
                    SelectedBrushSurface[] surfaces = null;
                    {
                        EditModeSurface surfaceTool = EditModeManager.ActiveTool as EditModeSurface;
                        if (surfaceTool == null)
                        {
                            editorWindow?.ShowNotification(new GUIContent("Cannot change surfaces when not in surface mode! Bother Sam if this is an issue"));
                            return;
                        }
                        surfaces = surfaceTool.GetSelectedSurfaces();
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
                        string replaceString = "Execute replacements on selected surfaces";
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
            
            class TabDirectionalPaint
            {
                private SurfaceUtilityEx.MaterialRef floor = new(), ceiling = new(), wall = new();

                static void DirectionallyPaintSurfaces(Material upMat, Material lateralMat, Material downMat)
                {
                    const float ANGLE_THRESHOLD = .6f;
                    var editModeSurface = EditModeManager.ActiveTool as EditModeSurface;
                    if (editModeSurface == null)
                    {
                        EditModeManager.ShowMessage("Can't edit surfaces outside of surface mode yet (bother sam about this!)");
                    }
                    var editModePlace = EditModeManager.ActiveTool as EditModePlace;
                    //editModePlace.brushes.Length

                    // caching this to avoid the implicit unity lifetime checks in the equality operator
                    bool _hasUp = upMat != null;
                    bool _hasDown = downMat != null;
                    bool _hasLateral = lateralMat != null;

                    var surfaces = editModeSurface.GetSelectedSurfaces();
                    Undo.IncrementCurrentGroup();
                    using (new UndoGroup(surfaces, "Replace surface materials"))
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
                    if (GUILayout.Button("Auto-paint selected surfaces"))
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
            struct SceneAnchor
            {
                //private GlobalGridPivot sourceObject;
            }

            public void OnAwake_AnchorTab()
            {
            }

            static bool ToggleButton(string labelActivate, string labelDeactivate, bool curVal, params GUILayoutOption[] options)
            {
                string label = curVal ? labelDeactivate : labelActivate;
                if (GUILayout.Button(label, options))
                {
                    curVal = !curVal;
                }
                return curVal;
            }
            public void OnGUI_AnchorTab()
            {
                // Grid visibility toggles //
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Show grids: ", GUILayout.ExpandWidth(false));
                    
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

                    //GridVisibility startVisibility = GridVisibility.None;
                    //if (csgGridEnabled) { startVisibility |= GridVisibility.CSGGrid; }
                    //if (lastShowingSceneViewGrid) { startVisibility |= GridVisibility.UnityGrid; }
                    //startVisibility = (GridVisibility)EditorGUILayout.EnumFlagsField("Grid Visibility", startVisibility);

                    
                    // Draw visibility Settings
                    EditorGUILayout.BeginVertical();
                    const float FIXED_WIDTH = 140;
                    RealtimeCSG.CSGSettings.GridVisible = ToggleButton("☐ CSG Grid", "☑ CSG Grid", RealtimeCSG.CSGSettings.GridVisible, GUILayout.Width(FIXED_WIDTH));
                    bool newUnityGridEnabled = ToggleButton("☐ Unity Grid", "☑ Unity Grid", lastUnityGridEnabled, GUILayout.Width(FIXED_WIDTH));
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
                GUILayout.Space(16);
                
                // Mode select //
                GlobalGridPivot.mode = (GlobalGridPivot.GlobalGridMode)EditorGUILayout.EnumPopup("Grid Mode:", GlobalGridPivot.mode);
                GUILayout.Space(8);
                
                // Context-dependent editor //
                switch (GlobalGridPivot.mode)
                {
                    case GlobalGridPivot.GlobalGridMode.Origin: break;
                    case GlobalGridPivot.GlobalGridMode.Manual:
                    {
                        hackManualGridPos = EditorGUILayout.FloatField("Grid Height:", hackManualGridPos);
                        hackManualGridRot = EditorGUILayout.FloatField("Grid Rotation:", hackManualGridRot);
                        GlobalGridPivot.HackOverrideManualPositionAndRotation(new Vector3(0, hackManualGridPos, 0), Quaternion.Euler(0, hackManualGridRot, 0));
                    } break;
                    // case GlobalGridPivot.GlobalGridMode.Anchored:
                    // {
                    //     GUILayout.Label("Scene anchors are not supported yet!");
                    // } break;
                }
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
    }
}