using JetBrains.Annotations;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RealtimeCSGExtensions
{
    public static class GlobalGridAnchor
    {
        public static System.Collections.Generic.HashSet<RCSGGridAnchor> editorGridAnchors = new();

        public static void EditorRegisterPivot(RCSGGridAnchor anchor)
        {
            editorGridAnchors.Add(anchor);
        }

        public static void EditorResetPivotsAfterFrame()
        {
            editorGridAnchors.Clear();
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/Realtime-CSG/Grid Anchor", false)]
        public static void CreateAnchor([CanBeNull] MenuCommand menuCommand)
        {
            GameObject go = new GameObject("New Grid Anchor");
            if (menuCommand != null)
            {
                GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            }

            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;

            SceneView sv = SceneView.lastActiveSceneView;
            Transform ct = sv.camera.transform;

            const float DIST = 20f;
            Vector3 pos = sv.camera.transform.position + sv.camera.transform.forward * DIST;
            if (Physics.Raycast(ct.position, ct.forward, out RaycastHit hit, DIST))
            {
                pos = hit.point;
            }

            // TODO: snap position?
            go.transform.position = pos;

            go.AddComponent<RCSGGridAnchor>();
        }
#endif

        public enum GlobalGridMode
        {
            Origin,
            Anchored,
            NumberPunch
        }

        public static string[] GlobalGridModeStrings = { "Origin", "Anchored", "Number Punch" };
        public static GlobalGridMode mode = GlobalGridMode.Origin;


        static Vector3 dataPunchGridPosition;
        static Quaternion dataPunchGridRotation;
        public static void HackOverrideNumberPunchPositionAndRotation(Vector3 gridPosition, Quaternion gridRotation)
        {
            dataPunchGridPosition = gridPosition;
            dataPunchGridRotation = gridRotation;
        }
        
        //// TODO: support sloped grids and fix the problem with the grid shader that stops us from supporting that?
        public static void GetCurrentGridPositionAndRotation(out Vector3 gridPosition, out Quaternion gridRotation)
        {
            switch (mode)
            {
                case GlobalGridMode.Origin:
                {
                    gridPosition = Vector3.zero;
                    gridRotation = Quaternion.identity;
                    return;
                }
                case GlobalGridMode.NumberPunch:
                {
                    gridPosition = dataPunchGridPosition;
                    gridRotation = dataPunchGridRotation;
                    return;
                }
                case GlobalGridMode.Anchored:
                {
                    gridPosition = Vector3.zero;
                    gridRotation = Quaternion.identity;
                    if (CurrentlySelectedGridAnchor != null) {
                        CurrentlySelectedGridAnchor.transform.GetPositionAndRotation(out gridPosition, out gridRotation);
                    }
                    return;
                }
            }
            
            Debug.LogError("Something is very wrong");
            gridPosition = Vector3.zero;
            gridRotation = Quaternion.identity;
            return;
        }
        

        public static RCSGGridAnchor CurrentlySelectedGridAnchor;

        public static RCSGGridAnchor GetActiveGridPivot()
        {
            if (mode == GlobalGridMode.Origin) { return null; }
        
            return CurrentlySelectedGridAnchor;
        }

        public static void SetActiveGridPivot(RCSGGridAnchor gridAnchor)
        {
            //mode = (gridPivot != null) ? GlobalGridMode.Anchored : GlobalGridMode.Origin;
            CurrentlySelectedGridAnchor = gridAnchor;
        }

        public static Transform GetActiveTransform()
        {
            if (mode == GlobalGridMode.Origin) { return null; }
            return CurrentlySelectedGridAnchor?.transform;
        }
    }
    public class RCSGGridAnchor : MonoBehaviour
    {
        private const string menuItem = "CONTEXT/Make Active Grid Pivot";

        //[MenuItem(menuItem, true)]
        //static bool ContextMakeActivePivotValidate()
        //{
        //    return true;
        //}
        //
        //[MenuItem(menuItem, false)]
        //static void ContextMakeActivePivot()
        //{
        //    GlobalGridPivot.SetActiveGridPivot(this);
        //    //GlobalGridPivot.currentlySelectedGridPivot = this;
        //    Debug.Log("TODO: Implement making active!");
        //}

        void OnDrawGizmos()
        {
            //Gizmos.matrix = transform.localToWorldMatrix;
            //Gizmos.color = new Color(0f, 1f, 1f, 1f);
            
            // Make invisibly selectable: 
            Gizmos.color = new Color(0f, 1f, 1f, 0f);
            Gizmos.DrawSphere(transform.position, 1f);
            GlobalGridAnchor.EditorRegisterPivot(this);
            //Gizmos.DrawIcon(transform.position, "Light Gizmo.tiff", true);
            //Gizmos.color = new Color(0f, 1f, 1f, .3f);
            //Vector3 fwdXZ = transform.forward;
            //fwdXZ.y = 0;
            //fwdXZ = fwdXZ.normalized;
            //Gizmos.DrawRay(transform.position, fwdXZ * 3);
        }

        void OnDrawGizmosSelected()
        {
            // hack since we only support XZ planes for now
            Vector3 euler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0, euler.y, 0);
        }
    }
}
