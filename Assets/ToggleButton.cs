using UnityEngine;
using System.Collections;
using TMPro;

public class ToggleButton : MonoBehaviour
{
    public TextMeshPro label;

	protected bool val = false;
    private void OnMouseDown()
    {
        OnToggle();
    }

    virtual protected void Start()
    {
        label.text = val ? "o" : "x";
    }
    virtual protected void OnToggle()
    {
        val = !val;
        OnValChange();
    }

    virtual protected void OnValChange()
    {
        label.text = val ? "o" : "x";
    }
}

