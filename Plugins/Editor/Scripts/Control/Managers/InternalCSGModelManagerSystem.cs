using RealtimeCSG.Components;
using UnityEngine;
using static UnityEngine.ObjectDispatcher;

namespace RealtimeCSG
{
    internal partial class InternalCSGModelManagerSystem {
        private static ObjectDispatcher dispatcher;
        private static NativeMethods External => InternalCSGModelManager.External;

        public static void Initialize() {
            if (dispatcher == null) {
                dispatcher = new();
                dispatcher.EnableTransformTracking<CSGBrush>(TransformTrackingType.GlobalTRS);
            }
        }

        public static void Cleanup() {
            if(dispatcher != null) { dispatcher.Dispose(); dispatcher = null; }
        }

        public static void TickCheckTransformChanges() {
            Initialize();
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
    }
}