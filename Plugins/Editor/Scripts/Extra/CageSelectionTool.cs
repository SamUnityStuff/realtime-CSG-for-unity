using RealtimeCSG.Components;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace RealtimeCSG
{
    static class CageSelection
    {

#if false
        // Check for a separating plane
        static bool hasSeparatingPlane(in float3 axis, in OBB box1, in OBB box2) {
            float projection1 =
                box1.halfSize.x * math.abs(math.dot(axis, box1.axisX)) +
                box1.halfSize.y * math.abs(math.dot(axis, box1.axisY)) +
                box1.halfSize.z * math.abs(math.dot(axis, box1.axisZ));

            float projection2 =
                box2.halfSize.x * math.abs(math.dot(axis, box2.axisX)) +
                box2.halfSize.y * math.abs(math.dot(axis, box2.axisY)) +
                box2.halfSize.z * math.abs(math.dot(axis, box2.axisZ));

            float distance = math.abs(math.dot(axis, box2.center - box1.center));
            return distance > (projection1 + projection2);
        }

        // Test for collision between two OBBs
        public static unsafe bool checkOBBIntersection(in OBB box1, in OBB box2) {
            fixed(float3* box1Axes = &box1.axisX, box2Axes = &box2.axisX) {
                // Test axes: all face normals and cross products of edges
                for (int i = 0; i< 3; ++i) {
                    if (hasSeparatingPlane(box1Axes[i], box1, box2)) return false;
                    if (hasSeparatingPlane(box2Axes[i], box1, box2)) return false;
                }
                //if (hasSeparatingPlane(box1.axisX, box1, box2)) { return false; }
                //if (hasSeparatingPlane(box2.axisX, box1, box2)) { return false; }
                //if (hasSeparatingPlane(box1.axisY, box1, box2)) { return false; }
                //if (hasSeparatingPlane(box2.axisY, box1, box2)) { return false; }
                //if (hasSeparatingPlane(box1.axisZ, box1, box2)) { return false; }
                //if (hasSeparatingPlane(box2.axisZ, box1, box2)) { return false; }

                for (int i = 0; i< 3; ++i) {
                    for (int j = 0; j< 3; ++j) {
                        float3 crossAxis = math.cross(box1Axes[i], box2Axes[j]);
                        if (math.length(crossAxis) > 0.00001f && hasSeparatingPlane(crossAxis, box1, box2)) { return false; }
                    }
                }
                return true;
            }
        }
#endif
        public static void Select(Vector3 center, Vector3 size, List<GameObject> selectedGameObjects) {//, Quaternion rotation) {
            Span<CSGBrush> brushes = InternalCSGModelManager.Brushes.AsSpanUnchecked();
            Bounds selectBounds = new(center, size);

            Handles.color = Color.green;
            Handles.matrix = Matrix4x4.identity;

            bool ignorePrefab = CSGSettings.Temp_SelectionIgnorePrefabs;
            for (int brushIdx = 0; brushIdx < brushes.Length; brushIdx++) {
                var brush = brushes[brushIdx];
                var bMesh = brush.ControlMesh;
                Transform brushTransform = brush.transform;
                Matrix4x4 brushMatrix = brushTransform.localToWorldMatrix;
                Bounds brushBounds = new(brushMatrix.GetPosition(), default);
                {
                    var bVerts = bMesh.Vertices;
                    for (int i = 0; i < bVerts.Length; ++i) {
                        Vector3 result = brushMatrix.MultiplyPoint3x4(bVerts[i]);
                        brushBounds.Encapsulate(in result);
                    }

                    if(brushBounds.Intersects(in selectBounds)) {
                        GameObject foundGO = brushTransform.gameObject;

                        if (ignorePrefab == false) {
                            if (PrefabUtility.IsPartOfNonAssetPrefabInstance(foundGO)) {
                                foundGO = PrefabUtility.GetOutermostPrefabInstanceRoot(foundGO);
                            }
                        }

                        selectedGameObjects.Add(foundGO);
                    }
                }
                //bMesh.Edges.Length
                //for (int surfIdx = 0; surfIdx < bSurfs.Length; surfIdx++) {
                //    bSurfs[surfIdx].
                //}
                //.Surfaces
            }
        }
        
        //static void Nothing() {
        //    var found = false;
        //    foreach (var model in InternalCSGModelManager.Models) {
        //        found = InternalCSGModelManager.External.GetItemsInFrustum(model, planes, objectsInFrustum) || found;
        //    }
        //
        //    var visibleLayers = Tools.visibleLayers;
        //
        //    var items = objectsInFrustum.ToArray();
        //    for (var i = items.Length - 1; i >= 0; i--) {
        //        var child = items[i];
        //        var node = child.GetComponent<CSGNode>();
        //        if (!node || ((1 << node.gameObject.layer) & visibleLayers) == 0)
        //            continue;
        //
        //        if (!objectsInFrustum.Contains(child))
        //            continue;
        //
        //        while (true) {
        //            var parent = GetGroupOperationForNode(node);
        //            if (!parent ||
        //                !AreAllBrushesSelected(parent.transform, objectsInFrustum))
        //                break;
        //
        //            objectsInFrustum.Add(parent.gameObject);
        //            node = parent;
        //        }
        //    }
        //    return found;
        //}
    }

    

    [EditorTool("Cage Selection Tool")]
    public class CageSelectionTool : EditorTool
    {
        public static bool _CageSelectionActive;
        public override bool gridSnapEnabled => base.gridSnapEnabled;


        static Vector3 lastCenter;
        static Vector3 lastSize;
        static Quaternion rotation = Quaternion.identity;
        static BoxBoundsHandle boxBoundsHandle = new();

        public override bool IsAvailable() {
            return RealtimeCSG.CSGSettings.EnableRealtimeCSG && RealtimeCSG.CSGSettings.EditMode == ToolEditMode.Place;
            return base.IsAvailable();
        }
        public override void OnActivated() {
            base.OnActivated();
            lastCenter = default;
            lastSize = new(10, 10, 10);
            rotation = Quaternion.identity;

            SceneView sv = SceneView.lastActiveSceneView;
            if (sv == null) { Debug.Log("AH");  return; }
            Camera c = sv.camera;
            //boxBoundsHandle.center = ;
            Transform cTransform = c.transform;
            Vector3 cPosition = cTransform.position;
            Vector3 cForward = cTransform.forward;

            lastCenter = cPosition + cForward * 40;
            lastSize = new(10, 10, 10);

            if (Physics.SphereCast(new(cPosition, cForward), 5, out RaycastHit hit, 100)) {
                lastCenter = hit.point;
            }

            boxBoundsHandle.center = lastCenter;
            boxBoundsHandle.size = lastSize;
        }
        public override void OnWillBeDeactivated() {
            base.OnWillBeDeactivated();
        }

        public static bool IsProbablyActive() {
            return Tools.current == Tool.Custom && ToolManager.activeToolType == typeof(CageSelectionTool);
        }


        public static void ONTOOLGUI(SceneView sv) {
            Camera c = sv.camera;

            bool btnIsShifted = (Event.current.modifiers & EventModifiers.Shift) != EventModifiers.None;

            if (btnIsShifted) {
                //this.rotation = Handles.RotationHandle(this.rotation, boxBoundsHandle.center);
                boxBoundsHandle.center = Handles.PositionHandle(boxBoundsHandle.center, rotation);
            }

            boxBoundsHandle.DrawHandle();

            // to support rotation
            // Matrix4x4 matrix = Matrix4x4.TRS(boxBoundsHandle.center, this.rotation, Vector3.one);
            // Handles.matrix = matrix;
            // {
            //     Handles.DrawWireCube(default, new(2, 2, 2));
            //     boxBoundsHandle.center = default;
            //     boxBoundsHandle.DrawHandle();
            //     boxBoundsHandle.center = matrix.MultiplyPoint(boxBoundsHandle.center);
            // }
            // Handles.matrix = Matrix4x4.identity;

            {
                const int UI_SELECT_ALL = 1;
                int UIAction = 0;
                var current = Event.current;
                Handles.BeginGUI();
                {
                    float handleSizeAtCenter = HandleUtility.GetHandleSize(boxBoundsHandle.center);
                    Vector3 screenPoint = HandleUtility.WorldToGUIPointWithDepth(boxBoundsHandle.center + (c.transform.up * handleSizeAtCenter * 1.5f));
                    if (screenPoint.z > 0) {
                        Vector2 rectSize = new(256, 18);
                        Rect r = new(screenPoint - new Vector3(rectSize.x / 2f, 0f), rectSize);
                        //GUILayout.BeginArea(new(screenPoint - new Vector3(rectSize.x / 2f, 0f), rectSize));

                        //string btnText = btnIsShifted ? "Add Bounded Brushes To Selection" : "Select All Brushes In Bounds";
                        string btnText = btnIsShifted ? "Add Bounded Brushes To Selection" : "Select Brushes In Bounds";
                        if (GUI.Button(r, btnText) || current.keyCode == KeyCode.Return) {
                            UIAction = UI_SELECT_ALL;
                        }
                        r.y += r.height;
                        GUI.Label(r, CSGSettings.Temp_SelectionIgnorePrefabs ? "Ignoring prefabs" : "Selecting prefabs");
                        //GUILayout.EndArea();
                    }
                }
                Handles.EndGUI();

                if (UIAction == UI_SELECT_ALL) {
                    List<GameObject> selectedGameObjects = ListPool<GameObject>.Get();
                    selectedGameObjects.Clear();
                    if(btnIsShifted) {
                        selectedGameObjects.AddRange(Selection.gameObjects);
                    }
                    CageSelection.Select(boxBoundsHandle.center, boxBoundsHandle.size, selectedGameObjects);


                    Selection.objects = selectedGameObjects.ToArray();
                    ListPool<GameObject>.Release(selectedGameObjects);
                }
            }
        }
        public override void OnToolGUI(EditorWindow window) {
            // TODO: Prevent RCSG from selecting
            SceneView sv = window as SceneView;
            if (sv == null) { Debug.Log("hmm");  return; }
        }

        void OnDrawHandles() {
            Handles.DrawWireCube(default, new(2, 2, 2));
        }
    }
}
