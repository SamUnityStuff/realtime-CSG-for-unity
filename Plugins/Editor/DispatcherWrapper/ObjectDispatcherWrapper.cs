#if UNITY_EDITOR
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RealtimeCSG.UnityWrappers
{
    public class ObjectDispatcher
    {
        UnityEngine.ObjectDispatcher _internal = new();
        
        // Wrapped types
        public enum TransformTrackingType
        {
            GlobalTRS = UnityEngine.ObjectDispatcher.TransformTrackingType.GlobalTRS,
            LocalTRS = UnityEngine.ObjectDispatcher.TransformTrackingType.LocalTRS,
            Hierarchy = UnityEngine.ObjectDispatcher.TransformTrackingType.Hierarchy
        }

        // Less-verbose casts (we're using the same type names in both namespaces, so this helps us not having to be so explicit everywhere)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static UnityEngine.ObjectDispatcher.TransformTrackingType Cast(TransformTrackingType type) { return (UnityEngine.ObjectDispatcher.TransformTrackingType)type; }

        // Wrapped methods
        public void EnableTransformTracking<T>(TransformTrackingType trackingType) where T : Object {
            _internal.EnableTransformTracking<T>(Cast(trackingType));
        }

        public void Dispose() {
            _internal.Dispose();
        }

        public Component[] GetTransformChangesAndClear<T>(TransformTrackingType trackingType, bool v) where T : Object {
            return _internal.GetTransformChangesAndClear<T>(Cast(trackingType), v);
        }
    }

}
#endif