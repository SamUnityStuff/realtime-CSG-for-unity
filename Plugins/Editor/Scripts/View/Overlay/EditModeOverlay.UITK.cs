using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealtimeCSG
{
    [Overlay(typeof(SceneView), "CSG Toolbar", defaultDisplay = true, defaultLayout = Layout.HorizontalToolbar)]
    public class RCSGBottomBarOverlay : IMGUIOverlay
    {
        public override void OnGUI() {
            Rect r = GUILayoutUtility.GetRect(830, CSG_GUIStyleUtility.BottomToolBarHeight + 4);
            SceneViewBottomBarGUI.OnBottomBarGUI(this.containerWindow as SceneView, r, (int)r.width);
        }
    }

    [Overlay(typeof(SceneView), "CSG Tool Window", defaultDisplay = true, minHeight = 600)]
    public class RCSGToolWindowOverlay : IMGUIOverlay
    {
        public override void OnGUI() {
            this.displayName = EditModeManager.ActiveTool?.GetModeName();
            EditModeManager.ActiveTool?.OnIMGUIContents();
        }
    }
}