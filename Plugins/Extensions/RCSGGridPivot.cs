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
            Origin, Anchored
        }
        public static GlobalGridMode mode = GlobalGridMode.Origin;

        public static RCSGGridPivot currentlySelectedGridPivot;

        public static RCSGGridPivot GetActiveGridPivot()
        {
            if (mode == GlobalGridMode.Origin) { return null; }

            return currentlySelectedGridPivot;
        }

        public static void SetActiveGridPivot(RCSGGridPivot gridPivot)
        {
            mode = (gridPivot != null) ? GlobalGridMode.Anchored : GlobalGridMode.Origin;
            currentlySelectedGridPivot = gridPivot;
        }
        
        public static Transform GetActiveTransform()
        {
            if (mode == GlobalGridMode.Origin) { return null; }
            return currentlySelectedGridPivot?.transform;
        }
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
        void ContextMakeActivePivot()
        {
            GlobalGridPivot.currentlySelectedGridPivot = this;
            Debug.Log("made active!");
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
