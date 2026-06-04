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

            // Fix for "Docking RCSG overlay windows creates invisible blocking deadzones over the entire docked zone"
            // "when docked, this overlay eats mouse input events within the bounds of every overlay in this dock zone - even if those bounds extend beyond the visual rectangle of this specific panel"
            base.rootVisualElement.pickingMode = PickingMode.Ignore;
        }
    }

    [Overlay(typeof(SceneView), "CSG Tool Window", defaultDisplay = true, minHeight = 600, defaultLayout = Layout.Panel, defaultDockZone = DockZone.Floating, defaultDockPosition = DockPosition.Bottom)]
    public class RCSGToolWindowOverlay : IMGUIOverlay
    {
        public override void OnGUI() {
            this.displayName = EditModeManager.ActiveTool?.GetModeName();
            EditModeManager.ActiveTool?.OnIMGUIContents();
            // Fix for "Docking RCSG overlay windows creates invisible blocking deadzones over the entire docked zone"
            base.rootVisualElement.pickingMode = PickingMode.Ignore;
        }
    }
}