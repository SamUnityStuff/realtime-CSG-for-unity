using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealtimeCSG
{

    [Overlay(typeof(SceneView), displayName: "Realtime CSG", id: _id, defaultDisplay: true, defaultLayout = Layout.VerticalToolbar)]

    internal class EditorModeOverlay : ToolbarOverlay
    {
        //public const string iconPath = "Packages/com.prenominal.realtimecsg/Plugins/Editor/Resources/GUI/";
        //public static string iconPath = KeyedDirectory.GetDirectory("RCSG_Icons");
        public static string iconPath = "";
        public const string _id = "RealtimeCSG";
        public EditorModeOverlay()
        : base(

            CSGActivateToggleButton._id,
            PlaceEditorModeButton._id,
            GenerateEditorModeButton._id,
            EditEditorModeButton._id,
            ClipEditorModeButton._id,
            SurfaceEditorModeButton._id
        ) {
            this.collapsedIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath + "CSG_Icon.png");
        }
    }

    [EditorToolbarElement(_id, typeof(SceneView))]
    internal class CSGActivateToggleButton : EditorToolbarToggle
    {
        public const string _id = EditorModeOverlay._id + "/CSGActivateToggle";
        public CSGActivateToggleButton() {
            tooltip = "Toggle Realtime CSG";

            onIcon = TempIcons.GetIcon("CSG_Icon");
            offIcon = TempIcons.GetIcon("CSG_Icon_off");

            this.RegisterValueChangedCallback(x => OnClicked());
            CSGSettings.OnRealtimeCSGEnabledChanged += OnRealtimeCSGEnabledChanged;

            value = CSGSettings.EnableRealtimeCSG;
        }

        private void OnClicked() {
            RealtimeCSG.CSGSettings.SetRealtimeCSGEnabled(value);
            OnRealtimeCSGEnabledChanged(value);
        }

        void OnRealtimeCSGEnabledChanged(bool isEnabled) {
            value = isEnabled;
            //parent.Query<EditorModeButton>().ForEach((button) => { button.SetEnabled(isEnabled); });
            foreach (EditorModeButton button in parent.Query<EditorModeButton>().ToList()) {
                button.SetEnabled(isEnabled);
            }
        }
    }

    internal class EditorModeButton : EditorToolbarToggle
    {
        ToolEditMode mode;
        public EditorModeButton(string iconName, ToolEditMode _mode) {
            mode = _mode;

            CSG_GUIStyleUtility.InitializeEditModeTexts();
            ToolTip tt = CSG_GUIStyleUtility.brushEditModeTooltips[(int)mode];
            tooltip = $"{tt.TitleString()}\n{tt.ContentsString()}\n{tt.KeyString()}";

            icon = TempIcons.GetIcon(iconName);

            this.RegisterValueChangedCallback(x => OnClicked());

            value = EditModeManager.EditMode == mode;

            EditModeManager.OnEditModeChanged += OnEditModeChanged;

            SetEnabled(CSGSettings.EnableRealtimeCSG);
        }

        void OnEditModeChanged(ToolEditMode _mode) => SetValueWithoutNotify(_mode == mode);

        private void OnClicked() {
            //do nothing if clicking on an already clicked toggle button, and reactivate it
            if (!value) {
                value = true;
            }

            if (value && !RealtimeCSG.CSGSettings.EnableRealtimeCSG) // avoid re-invoking if already enabled, since SetRealtimeCSGEnabled alters grid settings
            {
                RealtimeCSG.CSGSettings.SetRealtimeCSGEnabled(true);
            }
            EditModeManager.EditMode = mode;
        }
    }

    #region Buttons class for each Mode
    [EditorToolbarElement(_id, typeof(SceneView))]
    internal class PlaceEditorModeButton : EditorModeButton
    {
        public const string _id = EditorModeOverlay._id + "/Place";
        public PlaceEditorModeButton() : base("Place", ToolEditMode.Place) { }
    }


    [EditorToolbarElement(_id, typeof(SceneView))]
    internal class GenerateEditorModeButton : EditorModeButton
    {
        public const string _id = EditorModeOverlay._id + "/Generate";
        public GenerateEditorModeButton() : base("Generate", ToolEditMode.Generate) { }
    }

    [EditorToolbarElement(_id, typeof(SceneView))]
    internal class EditEditorModeButton : EditorModeButton
    {
        public const string _id = EditorModeOverlay._id + "/Edit";
        public EditEditorModeButton() : base("Edit", ToolEditMode.Edit) { }
    }

    [EditorToolbarElement(_id, typeof(SceneView))]
    internal class ClipEditorModeButton : EditorModeButton
    {
        public const string _id = EditorModeOverlay._id + "/Clip";
        public ClipEditorModeButton() : base("Clip", ToolEditMode.Clip) { }
    }

    [EditorToolbarElement(_id, typeof(SceneView))]
    internal class SurfaceEditorModeButton : EditorModeButton
    {
        public const string _id = EditorModeOverlay._id + "/Surfaces";
        public SurfaceEditorModeButton() : base("Surface", ToolEditMode.Surfaces) { }
    }

    #endregion

    // Extremely quick and dirty. When the time comes to switch how we're loading these,
    // we can just delete this class and patch wherever there's a compile error.
    internal static class TempIcons
    {
        static Dictionary<string, Texture2D> cache = new();
        internal static Texture2D GetIcon(string key) {
            Texture2D result;
            if (!cache.TryGetValue(key, out result)) {
                result = Resources.Load<Texture2D>("RealtimeCSG/Icons/" + key);
                cache[key] = result;
            }
            return result;
        }

    }

}