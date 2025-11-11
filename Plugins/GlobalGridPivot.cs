using UnityEngine;

public class GlobalGridPivot : MonoBehaviour
{
    public static GlobalGridPivot Get() 
    {
        if(_instance == null)
            _instance = GameObject.FindObjectOfType<GlobalGridPivot>();
        return _instance;
    }

    public static Transform GetTransform()
    {
        if(_transform == null)
            _transform = Get().transform;
        return _transform;
    }
    private static Transform _transform;
    private static GlobalGridPivot _instance;

    public enum GlobalGridMode
    {
        Origin, Anchored
    }

    public static GlobalGridMode mode
    {
        get
        {
            var i = Get();
            if (i == null)
            {
                return GlobalGridMode.Origin;
            }

            return i.gridMode;
        }
    }
    
    
    // stupid hack leveraging unity UI
    public GlobalGridMode gridMode = GlobalGridMode.Origin;
}
