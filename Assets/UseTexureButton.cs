using UnityEngine;
using System.Collections;
using TMPro;

public class UseTexureButton : MonoBehaviour
{
    public TextMeshPro label;

	bool val = false;
    private void OnMouseDown()
    {
        val = !val;
        label.text = val ? "o" : "x";
        Plotter.ChangeTextureFromButton(val);
    }
}

