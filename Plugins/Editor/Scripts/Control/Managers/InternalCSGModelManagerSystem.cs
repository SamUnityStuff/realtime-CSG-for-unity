using InternalRealtimeCSG;
using RealtimeCSG.Components;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using static UnityEngine.ObjectDispatcher;

namespace RealtimeCSG
{
    internal partial class InternalCSGModelManagerSystem {
        private static ObjectDispatcher dispatcher;
        private static NativeMethods External => InternalCSGModelManager.External;

        public static void LazyInitialize() {
            if (dispatcher == null) {
                dispatcher = new();
                dispatcher.EnableTransformTracking<CSGBrush>(TransformTrackingType.GlobalTRS);
                
                // activate all mesh instance colliders
                ColliderBakePopAll();

                EditorApplication.playModeStateChanged -= PlaymodeStateChanged;
                EditorApplication.playModeStateChanged += PlaymodeStateChanged;
            }
        }

        public static void Cleanup() {
            if(dispatcher != null) { dispatcher.Dispose(); dispatcher = null; }
            ColliderBakePopAll();
            EditorApplication.playModeStateChanged -= PlaymodeStateChanged;
        }
        private static void PlaymodeStateChanged(PlayModeStateChange change) {
            if(change == PlayModeStateChange.ExitingEditMode) {
                TickColliderBaking(true); // any currently-ticking collider bake timers, force active
            }
        }
        public static void Tick() {
            TickCheckTransformChanges();
            TickColliderBaking();
        }
        public static void TickCheckTransformChanges() {
            LazyInitialize();
            Component[] changedBrushes = dispatcher.GetTransformChangesAndClear<CSGBrush>(TransformTrackingType.GlobalTRS, false);
            
            for (int brushIndex = 0; brushIndex < changedBrushes.Length; brushIndex++) {
                Component component = changedBrushes[brushIndex];
                CSGBrush brush = component as CSGBrush;

                var brushNodeID = brush.brushNodeID;

                // make sure it's registered, otherwise ignore it
                if (brushNodeID == CSGNode.InvalidNodeID){
                    continue;
                }

                // TODO: simplify this
                var brushTransform = brush.hierarchyItem.Transform;
                var currentLocalToWorldMatrix = brushTransform.localToWorldMatrix;
                var prevTransformMatrix = brush.compareTransformation.localToWorldMatrix;
                {
                    var modelTransform = brush.ChildData.Model.transform;
                    brush.compareTransformation.localToWorldMatrix = currentLocalToWorldMatrix;
                    brush.compareTransformation.brushToModelSpaceMatrix = modelTransform.worldToLocalMatrix *
                        brush.compareTransformation.localToWorldMatrix;

                    var localToModelMatrix = brush.compareTransformation.brushToModelSpaceMatrix;
                    External.SetBrushToModelSpace(brushNodeID, localToModelMatrix);

                    if (brush.ControlMesh != null)
                        brush.ControlMesh.Generation = brush.controlMeshGeneration + 1;
                }

                if (brush.OperationType != brush.prevOperation) {
                    brush.prevOperation = brush.OperationType;

                    External.SetBrushOperationType(brushNodeID,
                                                   brush.OperationType);
                }

                if (brush.ControlMesh == null) {
                    brush.ControlMesh = ControlMeshUtility.EnsureValidControlMesh(brush);
                    if (brush.ControlMesh == null)
                        continue;

                    brush.controlMeshGeneration = brush.ControlMesh.Generation;
                    ControlMeshUtility.RebuildShape(brush);
                } else
                if (brush.controlMeshGeneration != brush.ControlMesh.Generation) {
                    brush.controlMeshGeneration = brush.ControlMesh.Generation;
                    ControlMeshUtility.RebuildShape(brush);
                }
            }
            //var nodeChanges = dispatcher.GetTransformChangesAndClear<CSGNode>(ObjectDispatcher.TransformTrackingType.GlobalTRS, Unity.Collections.Allocator.Temp);
            //nodeChanges.Dispose();
        }
    

        // Collider cooking
        static System.Collections.Generic.List<ColliderBakeTimer> colliderBakeTimers = new();
        static double EditorTime() { return EditorApplication.timeSinceStartup; }
        static void TickColliderBaking(bool forceAll = false) {
            double editorTime = EditorTime();
            for (int i = colliderBakeTimers.Count - 1; i >= 0; i--) {
                var bakeTimer = colliderBakeTimers[i];
                if (forceAll || editorTime > bakeTimer.timeToBake) {
                    bakeTimer.meshCollider.enabled = true; // TODO: bake in background job?
                    colliderBakeTimers.RemoveAtSwapBack(i);
                }
            }
        }

        public static void FlagMeshColliderForCooking(MeshCollider meshCollider) {
            int bombIdx = -1;
            for (int i = 0; i < colliderBakeTimers.Count; i++) {
                if (colliderBakeTimers[i].meshCollider == meshCollider) {
                    bombIdx = i;
                    break;
                }
            }
            if(bombIdx == -1) {
                bombIdx = colliderBakeTimers.Count;
                colliderBakeTimers.Add(default);
            }

            if (meshCollider.enabled) { meshCollider.enabled = false; }
            colliderBakeTimers[bombIdx] = new(meshCollider, EditorTime() + 1f);
        }
        static void ColliderBakePopAll() {
            var generatedMeshInstances = GameObject.FindObjectsByType<GeneratedMeshInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < generatedMeshInstances.Length; i++) {
                var collider = generatedMeshInstances[i].CachedMeshCollider;
                if (collider) {
                    collider.enabled = true;
                }
            }
            colliderBakeTimers.Clear();
        }
        struct ColliderBakeTimer {
            public MeshCollider meshCollider;
            public double timeToBake;
            public ColliderBakeTimer(MeshCollider _meshCollider, double _time) {
                meshCollider = _meshCollider;
                timeToBake = _time;
            }
        }
    }
}