using UnityEngine.EventSystems;

// The 3D map nodes are event targets rather than Selectables; bridge their focus to a native button.
public sealed class RouteRandomButton : UnityEngine.UI.Button
{
    public RouteMapBoard Board { private get; set; }
    public override void OnMove(AxisEventData data)
    {
        if (Board != null) { Board.FocusAvailable(); data.Use(); }
        else base.OnMove(data);
    }
}
