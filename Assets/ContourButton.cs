using UnityEngine;
using System.Collections;

public class ContourButton : MonoBehaviour
{
	public int delta = 1;

    private void OnMouseDown()
    {
        Plotter.ChangeContourFromButton(delta);
    }
}

