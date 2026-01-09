using System.Collections.Generic;
using RealtimeCSG;
using RealtimeCSGExtensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

namespace RealtimeCSGExtensions.Editor
{
    static class GUIStyles
    {
        private static GUIStyle labelAligned = new GUIStyle(GUI.skin.label);

        public static GUIStyle AlignedLabel(TextAnchor alignment)
        {
            labelAligned.alignment = alignment;
            return labelAligned;
        }
    }
    public static class SurfaceUtilityEx
    {
        public static Material GetMaterialAtIntersection(LegacyBrushIntersection intersection)
        {
            var brush = intersection.brush;
            var shape = brush.Shape;
            var surface = shape.Surfaces[intersection.surfaceIndex];
            int texGenIndex = surface.TexGenIndex;
            Material mat = shape.TexGens[texGenIndex].RenderMaterial;
            return mat;
        }
        
        // Editor Utilities
        public class MaterialRef
        {
            public Material value;

            public MaterialRef() { }
            public MaterialRef(Material mat) { value = mat; }
            public static implicit operator Material(MaterialRef matRef) { return matRef.value; }
        }

        public static void PickableMaterialField(MaterialRef _mat, params GUILayoutOption[] options)
        {
            PickableMaterialField(null, _mat, options);
        }
        public static void PickableMaterialField(string label, MaterialRef _mat, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(options);
            GUILayout.BeginHorizontal();
            {
                if (!string.IsNullOrEmpty(label)) { GUILayout.Label(label); }
            
                GUILayout.BeginVertical();
                {
                    Texture2D texture = AssetPreview.GetAssetPreview(_mat);


                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Pick", GUILayout.ExpandWidth(false)))
                        {
                            RCSGExtensionSceneGUI.InitiateMaterialPickOperation((newMat) => _mat.value = newMat);
                        }

                        _mat.value = (Material)EditorGUILayout.ObjectField(_mat, typeof(Material));

                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        //Rect previewRect = GUILayoutUtility.GetAspectRect(1f);
                        Rect previewRect = GUILayoutUtility.GetRect(64, 64);
                        //GUILayout.FlexibleSpace();
                        if (texture != null)
                        {
                            EditorGUI.DrawPreviewTexture(previewRect, texture);
                        }
                        else
                        {
                            GUI.Label(previewRect, "<no material selected>", EditorStyles.helpBox);
                        }
                    }
                    GUILayout.EndHorizontal();
                }        
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
                    
        }
    }
    public static class RCSGExtensionSceneGUI
    {
        //
        public static List<RCSGGridAnchor> framePivots = new();
        
        public static void Register(RCSGGridAnchor activeAnchor)
        {
            framePivots.Add(activeAnchor);
        }
        
        //
        private static System.Action<Material> _activeMaterialAction = null;
        public static void InitiateMaterialPickOperation(System.Action<Material> callback)
        {
            if (RealtimeCSG.CSGSettings.EnableRealtimeCSG == false)
            {
                _activeMaterialAction = null;
                return;
            }
            
            _activeMaterialAction = callback;
        }
        
        public static void UnderlayOnSceneGUI(SceneView sceneView)
        {
            var target = Selection.activeGameObject;   
            var e = Event.current;
            var camera = sceneView.camera;
            
            // Material picker stuff
            if(_activeMaterialAction != null)
            {
                // TODO: eyedropper
                EditorGUIUtility.AddCursorRect(new Rect(Vector2.zero, sceneView.position.size), MouseCursor.ArrowPlus);
                
                if (e.type == EventType.MouseDown && e.button == 0) { e.Use(); }
                if (e.type == EventType.MouseDrag && e.button == 0) { e.Use(); }
                if (e.type == EventType.MouseUp && e.button == 0) {
                    LegacyBrushIntersection intersection = null;
                    if (InternalRealtimeCSG.SceneQueryUtility.FindWorldIntersection(camera, e.mousePosition,
                            out intersection))
                    {
                        _activeMaterialAction?.Invoke(SurfaceUtilityEx.GetMaterialAtIntersection(intersection));
                        RCSGExtensionWindow.FlagForRepaint();
                    }
                    _activeMaterialAction = null;
                    e.Use();
                }
            }
            
            
            
            // TODO:
            // Grid Pivot Stuff
            #if true
            var _zTest = Handles.zTest;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            {
                RCSGGridAnchor selectedAnchor = target?.GetComponent<RCSGGridAnchor>();
                RCSGGridAnchor activeAnchor = GlobalGridAnchor.GetActiveGridPivot();
                bool hasSelectedAnchor = selectedAnchor != null;
                bool selectedAnchorIsActiveAnchor = hasSelectedAnchor && (selectedAnchor == activeAnchor);
                bool inAnchorMode = GlobalGridAnchor.mode == GlobalGridAnchor.GlobalGridMode.Anchored;

                {
                    //HandleUtility.pickGameObjectCustomPasses -= HandleUtilityOnPickGameObjectCustomPasses;
                    //HandleUtility.pickGameObjectCustomPasses += HandleUtilityOnPickGameObjectCustomPasses;
                }

                const float SizeInactive = 2f, SizeActive = 3f, SizeSelected = 3f;
                // Do general pivots
                {
                    var pivots = GlobalGridAnchor.editorGridAnchors;
                    foreach (var anchor in pivots)
                    {
                        if (anchor == selectedAnchor) { continue; }
                        anchor.transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

                        float size;
                        Color baseColor, topColor;
                        if (inAnchorMode && anchor == activeAnchor) {
                            size = SizeActive;
                            baseColor = new Color(.0f, .6f, .3f, .5f);
                            topColor = new Color(.3f, 1f, .5f, .5f);
                        } else {
                            size = SizeInactive;
                            baseColor = new Color(.0f, .5f, .5f, .5f);
                            topColor = new Color(.0f, .9f, .9f, .5f);
                        }
                        DrawGridHandle(position, rotation, size, baseColor);
                        DrawGridHandle(position + new Vector3(0, .05f, 0f), rotation, size, topColor);
                    }
                }
                
                // Do active pivot
                if(inAnchorMode && activeAnchor != null)
                {
                    activeAnchor.transform.GetPositionAndRotation(out Vector3 activePosition, out Quaternion activeRotation);
                    float size = selectedAnchorIsActiveAnchor ? SizeSelected : SizeActive;
                    if (!selectedAnchorIsActiveAnchor)
                    {
                        
                        Handles.Label(activePosition + (1.5f * Vector3.up), "[Active Grid Anchor]", GUIStyles.AlignedLabel(TextAnchor.MiddleCenter));
                        //Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), activePosition, activeRotation, size, e.type);
                    }

                    // Draw disc
                    {
                        Color _c = Handles.color;
                        Color highlightColor = new Color(.3f, 1f, .5f, .5f);
                        highlightColor.a *= .5f;
                        Handles.color = highlightColor;
                        Handles.DrawSolidDisc(activePosition + (Vector3.up*.05f), Vector3.up, size);
                        Handles.color = _c;
                    }
                }
                
                // Do selected pivot
                if(hasSelectedAnchor)
                {   
                    Transform selectedPivotTransform = selectedAnchor.transform;
                    selectedPivotTransform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
                    
                    Vector3 guiOffset3D = HandleUtility.GetHandleSize(position) * 1.5f * Vector3.up;
                    Vector3 guiCenter3D = position + guiOffset3D;
                    Vector2 guiSize = new(200, 100);
                    Handles.DrawDottedLine(position, guiCenter3D, 4f);
                    {
                        DrawGridHandle(position, rotation, SizeSelected, Color.white);
                        
                        // TODO: REMOVE THIS AFTER FIXING THE ACTUAL RCSG ROTATION THING
                        // HACK: ENFORCE DRAWING ROTATION
                        if(Tools.current == Tool.Rotate)
                        {
                            Color c = Handles.color;
                            Handles.color = Color.lightGreen;
                            selectedPivotTransform.rotation = rotation = Handles.Disc(rotation, position, Vector3.up, HandleUtility.GetHandleSize(position), true, 0);
                            Handles.color = c;
                        }
                    }
                    Handles.BeginGUI();
                    {
                        Vector3 guiPoint = HandleUtility.WorldToGUIPointWithDepth(guiCenter3D);
                        if (guiPoint.z >= 0)
                        {
                            Rect rect = new Rect(guiPoint.x - guiSize.x/2, guiPoint.y-guiSize.y/2, guiSize.x, guiSize.y);
                            GUILayout.BeginArea(new Rect(rect));
                            {
                                GUILayout.FlexibleSpace();
                                if (GlobalGridAnchor.mode == GlobalGridAnchor.GlobalGridMode.Anchored)
                                {
                                    if (selectedAnchorIsActiveAnchor)
                                    {
                                        if (GUILayout.Button("Return Grid To Origin"))
                                        {
                                            GlobalGridAnchor.SetActiveGridPivot(null);
                                            RCSGExtensionWindow.FlagForRepaint();
                                        }
                                    }
                                    else
                                    {
                                        if (GUILayout.Button("Set Active Grid Anchor"))
                                        {
                                            GlobalGridAnchor.SetActiveGridPivot(selectedAnchor);
                                            RCSGExtensionWindow.FlagForRepaint();
                                        }
                                    }

                                    if (!RealtimeCSG.CSGSettings.GridVisible)
                                    {
                                        GUILayout.Space(4);
                                        if (GUILayout.Button("FIX: Show CSG grid")) {
                                            RealtimeCSG.CSGSettings.GridVisible = true;
                                            RCSGExtensionWindow.FlagForRepaint();
                                        }
                                    }
                                }
                                else
                                {
                                    if (GUILayout.Button("Switch grid mode to Anchored"))
                                    {
                                        GlobalGridAnchor.mode = GlobalGridAnchor.GlobalGridMode.Anchored;
                                        RCSGExtensionWindow.FlagForRepaint();
                                    }
                                }
                                
                                GUILayout.FlexibleSpace();
                            }
                            GUILayout.EndArea();
                        }
                    }
                    Handles.EndGUI();
                }
            }
            Handles.zTest = _zTest;
            #endif
            GlobalGridAnchor.EditorResetPivotsAfterFrame();
        }

        static void DrawGridHandle(Vector3 position, Quaternion rotation, float scale, Color color)
        {
            var m = Handles.matrix;
            Color c = Handles.color;
            
            Handles.color = color;
            Handles.matrix = Matrix4x4.TRS(position, rotation, new Vector3(scale, scale, scale));
            Handles.DrawWireCube(Vector3.zero, new Vector3(.333f,0,.333f));
            Handles.DrawWireCube(Vector3.zero, new Vector3(.666f,0,.666f));
            Handles.DrawWireCube(Vector3.zero, new Vector3(1.00f,0,1.00f));
            Handles.DrawLine(new(-1.6f,0,0), new(1.6f,0,0));
            Handles.DrawLine(new(0,0,-1.6f), new(0,0,1.6f));
            Handles.color = c;
            Handles.matrix = m;
        }
        private static GameObject HandleUtilityOnPickGameObjectCustomPasses(Camera cam, int layers, Vector2 position, GameObject[] ignore, GameObject[] filter, out int materialIndex)
        {
            Debug.Log("running");
            bool hit = false;
            var ray = cam.ScreenPointToRay( position );
            RCSGGridAnchor[] gridPivots = GameObject.FindObjectsByType<RCSGGridAnchor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            GameObject hitSelection = null;
            float hitDist = float.MaxValue;
            //Debug.DrawLine( ray.origin , ray.origin+ray.direction*100f , Color.magenta , 1f );
            for( int i = 0; i < gridPivots.Length; i++ )
            {
                
                RCSGGridAnchor testAnchor = gridPivots[i];
                Transform testPivotTransform = testAnchor.transform;
                Vector3 testPivotCenter = testPivotTransform.position;
                Bounds testPivotBounds = new Bounds(testPivotCenter, Vector3.one);
                bool testHit = testPivotBounds.IntersectRay(ray, out float testDist);
                if (testHit && testDist < hitDist)
                {
                    hitSelection = testAnchor.gameObject;
                    hitDist = testDist;
                }
                Debug.DrawLine(testPivotBounds.min, testPivotBounds.max, Color.green, 1f);
                
            }

            materialIndex = -1;
            return hitSelection;

        }
    }
}