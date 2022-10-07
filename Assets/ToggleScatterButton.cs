using UnityEngine;
using System.Collections;

public class ToggleScatterButton : ToggleButton
{
    protected override void OnToggle()
    {
        base.OnToggle();
        Plotter.ChangeScatterModeFromButton(val);
        if (Plotter.inAnchorMode)
            Plotter.ChangeAnchorModeFromButton(false);
    }

    private void Update()
    {
        if (val != Plotter.inScatterMode)
        {
            val = Plotter.inScatterMode;
            OnValChange();
        }
    }
}

