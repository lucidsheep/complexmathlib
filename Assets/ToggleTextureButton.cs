using UnityEngine;
using System.Collections;

public class ToggleTextureButton : ToggleButton
{
    protected override void OnToggle()
    {
        base.OnToggle();
        Plotter.ChangeTextureFromButton(val);
    }
}

