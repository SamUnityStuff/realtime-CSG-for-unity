namespace RealtimeCSG
{
    public static class Compatibility
    {
        #if UNITY_6000_0_OR_NEWER
        public const string WinBtnClose = "ToolbarSearchCancelButton";
        #else
        public const string WinBtnClose = "WinBtnClose";
        #endif
    }
}