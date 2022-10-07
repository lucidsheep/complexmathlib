using UnityEngine;
using System.Collections;

public class ToggleAnchorButton : ToggleButton
{
    protected override void OnToggle()
    {
        base.OnToggle();
        Plotter.ChangeAnchorModeFromButton(val);
        if (Plotter.inScatterMode)
            Plotter.ChangeScatterModeFromButton(false);
    }

    private void Update()
    {
        if (val != Plotter.inAnchorMode)
        {
            val = Plotter.inAnchorMode;
            OnValChange();
        }
    }
}

