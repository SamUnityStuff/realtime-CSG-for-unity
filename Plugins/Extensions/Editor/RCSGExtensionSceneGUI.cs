using System.Collections.Generic;
using RealtimeCSG;
using RealtimeCSGExtensions;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSGExtensions.Editor
{
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
        private static System.Action<Material> _activeMaterialAction = null;
        public static List<RCSGGridPivot> framePivots = new();

        public static void Register(RCSGGridPivot activePivot)
        {
            framePivots.Add(activePivot);
        }
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
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    e.Use();
                }
                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    LegacyBrushIntersection intersection = null;
                    if (InternalRealtimeCSG.SceneQueryUtility.FindWorldIntersection(camera, e.mousePosition,
                            out intersection))
                    {
                        _activeMaterialAction?.Invoke(SurfaceUtilityEx.GetMaterialAtIntersection(intersection));
                    }
                    _activeMaterialAction = null;
                    e.Use();
                }
            }
            
            
            
            // TODO:
            // Grid Pivot Stuff
            {
                
                RCSGGridPivot selectedPivot = target?.GetComponent<RCSGGridPivot>();
                RCSGGridPivot activePivot = GlobalGridPivot.GetActiveGridPivot();
                bool hasSelectedPivot = selectedPivot != null;
                bool selectedPivotIsActivePivot = hasSelectedPivot && (selectedPivot == activePivot);

                {
                    //HandleUtility.pickGameObjectCustomPasses -= HandleUtilityOnPickGameObjectCustomPasses;
                    //HandleUtility.pickGameObjectCustomPasses += HandleUtilityOnPickGameObjectCustomPasses;
                }

                const float SizeInactive = .5f, SizeActive = 1f, SizeSelected = 1.5f;
                // Do active pivot
                if(activePivot != null)
                {
                    activePivot.transform.GetPositionAndRotation(out Vector3 activePosition, out Quaternion activeRotation);
                    float size = selectedPivotIsActivePivot ? SizeSelected : SizeActive;
                    Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), activePosition, activeRotation, size, e.type);
                }
                
                // Do selected pivot
                if(hasSelectedPivot)
                {   
                    selectedPivot.transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
                    //Handles.SphereHandleCap(EditorGUIUtility.GetControlID(SphereHandleHash, FocusType.Passive), position, rotation, .5f, e.type);
                    Vector3 guiOffset3D = HandleUtility.GetHandleSize(position) * 1.5f * Vector3.up;
                    Vector3 guiCenter3D = position + guiOffset3D;
                    Vector2 guiSize = new(200, 100);
                    Handles.DrawDottedLine(position, guiCenter3D, 4f);
                    {
                        DrawGridHandle(position, rotation, Color.white);
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
                                if (selectedPivotIsActivePivot)
                                {
                                    if (GUILayout.Button("Return Grid To Origin"))
                                    {
                                        GlobalGridPivot.SetActiveGridPivot(null);
                                    }
                                }
                                else
                                {
                                    if (GUILayout.Button("Set Active Grid Anchor"))
                                    {
                                        GlobalGridPivot.SetActiveGridPivot(selectedPivot);
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

        }

        static void DrawGridHandle(Vector3 position, Quaternion rotation, Color color)
        {
            var m = Handles.matrix;
            Color c = Gizmos.color;
            
            Gizmos.color = color;
            Handles.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
            Handles.DrawWireCube(Vector3.zero, new Vector3(1,0,1));
            Handles.DrawWireCube(Vector3.zero, new Vector3(2,0,2));
            Handles.DrawWireCube(Vector3.zero, new Vector3(3,0,3));
            Handles.DrawLine(new(-5,0,0), new(5,0,0));
            Handles.DrawLine(new(0,0,-5), new(0,0,5));
            Gizmos.color = c;
            Handles.matrix = m;
        }
        private static GameObject HandleUtilityOnPickGameObjectCustomPasses(Camera cam, int layers, Vector2 position, GameObject[] ignore, GameObject[] filter, out int materialIndex)
        {
            Debug.Log("running");
            bool hit = false;
            var ray = cam.ScreenPointToRay( position );
            RCSGGridPivot[] gridPivots = GameObject.FindObjectsByType<RCSGGridPivot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            GameObject hitSelection = null;
            float hitDist = float.MaxValue;
            //Debug.DrawLine( ray.origin , ray.origin+ray.direction*100f , Color.magenta , 1f );
            for( int i = 0; i < gridPivots.Length; i++ )
            {
                
                RCSGGridPivot testPivot = gridPivots[i];
                Transform testPivotTransform = testPivot.transform;
                Vector3 testPivotCenter = testPivotTransform.position;
                Bounds testPivotBounds = new Bounds(testPivotCenter, Vector3.one);
                bool testHit = testPivotBounds.IntersectRay(ray, out float testDist);
                if (testHit && testDist < hitDist)
                {
                    hitSelection = testPivot.gameObject;
                    hitDist = testDist;
                }
                Debug.DrawLine(testPivotBounds.min, testPivotBounds.max, Color.green, 1f);
                
            }

            materialIndex = -1;
            return hitSelection;

        }
    }
}