using System;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSGExtensions.Editor
{
    [CustomEditor( typeof( RCSGGridAnchor ) )]
    public class RCSGGridPivotEditor : UnityEditor.Editor
    {
        private static int SphereHandleHash = "handleHash".GetHashCode();
        //public bool HasFrameBounds() { return true; }
        //
        //public Bounds OnGetFrameBounds()
        //{
        //    Debug.Log("getting frame bounds");
        //    RCSGGridPivot rcsgPivot = target as RCSGGridPivot;
        //    return new Bounds(rcsgPivot.transform.position, Vector3.one);
        //}

        public void OnSceneGUI()
        {
            return;
            RCSGGridAnchor rcsgAnchor = target as RCSGGridAnchor;
            Transform pivotTransform = rcsgAnchor.transform;

            if (Selection.activeTransform == pivotTransform) {
                pivotTransform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
                Color c = Handles.color;
                Handles.color = Color.lightGreen;
                const float RADIUS = 3;
                pivotTransform.rotation = Handles.Disc(rotation, position, Vector3.up, RADIUS, false, 0);
                Handles.color = c;
            }
        }
    }    
}