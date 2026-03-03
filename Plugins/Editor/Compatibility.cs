using System.Reflection;

namespace RealtimeCSG
{
    public static class Compatibility
    {
        #if UNITY_6000_0_OR_NEWER
        public const string WinBtnClose = "ToolbarSearchCancelButton";
        public const string s_RectSelectionID = "k_RectSelectionID";
        public const BindingFlags s_RectSelectionIDFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        public const string m_SelectStartPoint = "m_StartPoint"; // or m_StartMousePoint? double check this
#else
        public const string WinBtnClose = "WinBtnClose";
        public const string s_RectSelectionID = "s_RectSelectionID";
        public const BindingFlags s_RectSelectionIDFlags = BindingFlags.NonPublic | BindingFlags.Static;
        public const string m_SelectStartPoint = "m_SelectStartPoint";
#endif
    }
}