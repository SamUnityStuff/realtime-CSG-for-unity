using Drawing;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
namespace RealtimeCSGExtensions.Editor
{
    public static class GUIStyles
    {
        private static GUIStyle labelAligned = new GUIStyle(GUI.skin.label);

        public static GUIStyle AlignedLabel(TextAnchor alignment) {
            labelAligned.alignment = alignment;
            return labelAligned;
        }
    }

    public static partial class EditorGUILayoutUtility {
        // TODO: only render stuff onscreen
        public struct ListState {
            public Vector2 scrollPos;

            public int _counter;
        }

        // this is bad but we can make it better later
        public static void BeginListWithHeader(ref ListState listState, string title) {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(title, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            BeginList(ref listState);
        }
        public static void BeginList(ref ListState listState) {
            listState._counter = 0;
            EditorGUILayoutUtility.HorizontalLine(1f);
            listState.scrollPos = GUILayout.BeginScrollView(listState.scrollPos);
        }
        
        public static void BeginListElement() {
            GUILayout.BeginHorizontal();
        }

        public static void EndListElement() {
            GUILayout.EndHorizontal();
            EditorGUILayoutUtility.HorizontalLine(1f);
            //GUILayout.EndArea();
        }

        public static void EndList() {
            GUILayout.EndScrollView();
        }
    }

    //class ScopedTransformTreeView : TreeView<int>
    //{
    //    public ScopedTransformTreeView(TreeViewState<int> state) : base(state) {
    //    }
    //    
    //    public void Clear() {
    //        _transformList.Clear();
    //    }
    //    public void Add(Transform t) {
    //        _transformList.Add(t);
    //    }
    //
    //    List<Transform> _transformList = new();
    //    HashSet<int> _usedTransforms = new();
    //    protected override TreeViewItem<int> BuildRoot() {
    //        for(int i = 0; i < _transformList.Count; i++) {
    //            Transform t = _transformList[i];
    //            int tID = t.GetInstanceID();
    //        }
    //    }
    //}
}