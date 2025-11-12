using UnityEditor;
using UnityEngine;

namespace RealtimeCSGExtensions.Editor
{
    [CustomEditor( typeof( RCSGGridPivot ) )]
    public class RCSGGridPivotEditor : UnityEditor.Editor
    {
        private static int SphereHandleHash = "handleHash".GetHashCode();
        public bool HasFrameBounds() { return true; }

        public Bounds OnGetFrameBounds()
        {
            RCSGGridPivot rcsgPivot = target as RCSGGridPivot;
            return new Bounds(rcsgPivot.transform.position, Vector3.one);
        }
    }    
}