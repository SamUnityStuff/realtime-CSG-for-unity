using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RealtimeCSGExtensions
{
    public static class GlobalGridPivot
    {
        public enum GlobalGridMode
        {
            Origin, Manual//, Anchored
        }
        public static GlobalGridMode mode = GlobalGridMode.Origin;


        static Vector3 manualGridPosition;
        static Quaternion manualGridRotation;
        public static void HackOverrideManualPositionAndRotation(Vector3 gridPosition, Quaternion gridRotation)
        {
            manualGridPosition = gridPosition;
            manualGridRotation = gridRotation;
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
                case GlobalGridMode.Manual:
                {
                    gridPosition = manualGridPosition;
                    gridRotation = manualGridRotation;
                    return;
                }
            }
            
            Debug.LogError("Something is very wrong");
            gridPosition = Vector3.zero;
            gridRotation = Quaternion.identity;
            return;
        }
        

        // public static RCSGGridPivot currentlySelectedGridPivot;

        // public static RCSGGridPivot GetActiveGridPivot()
        // {
        //     if (mode == GlobalGridMode.Origin) { return null; }
        // 
        //     return currentlySelectedGridPivot;
        // }

        // public static void SetActiveGridPivot(RCSGGridPivot gridPivot)
        // {
        //     mode = (gridPivot != null) ? GlobalGridMode.Anchored : GlobalGridMode.Origin;
        //     currentlySelectedGridPivot = gridPivot;
        // }

        // public static Transform GetActiveTransform()
        // {
        //     if (mode == GlobalGridMode.Origin) { return null; }
        //     return currentlySelectedGridPivot?.transform;
        // }
    }
    public class RCSGGridPivot : MonoBehaviour
    {
        private const string menuItem = "CONTEXT/Make Active Grid Pivot";

        [MenuItem(menuItem, true)]
        static bool ContextMakeActivePivotValidate()
        {
            return true;
        }

        [MenuItem(menuItem, false)]
        static void ContextMakeActivePivot()
        {
            //GlobalGridPivot.currentlySelectedGridPivot = this;
            Debug.Log("TODO: Implement making active!");
        }

        void OnDrawGizmos()
        {
            //Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0f, 1f, 1f, 0f);
            Gizmos.DrawSphere(transform.position, 1f);
            //Gizmos.DrawIcon(transform.position, "Light Gizmo.tiff", true);
            Gizmos.color = new Color(0f, 1f, 1f, .3f);
            Vector3 fwdXZ = transform.forward;
            fwdXZ.y = 0;
            fwdXZ = fwdXZ.normalized;
            Gizmos.DrawRay(transform.position, fwdXZ * 3);
        }

        void OnDrawGizmosSelected()
        {
            // hack since we only support XZ planes for now
            Vector3 euler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0, euler.y, 0);
        }
    }
    
#if UNITY_EDITOR && false

    [CustomEditor( typeof( RCSGGridPivot ) )]
    public class RCSGGridPivotEditor : Editor
    {
        // Draw lines between a chosen GameObject
        // and a selection of added GameObjects

        void OnSceneGUI()
        {
            // Get the chosen GameObject
            RCSGGridPivot t = target as RCSGGridPivot;

            if( t == null )
                return;

            // Grab the center of the parent
            Vector3 center = t.transform.position;

            // Iterate over GameObject added to the array...
            for( int i = 0; i < t.GameObjects.Length; i++ )
            {
                // ... and draw a line between them
                if( t.GameObjects[i] != null )
                    Handles.DrawLine( center, t.GameObjects[i].transform.position );
            }
        }
    }    
#endif
}
