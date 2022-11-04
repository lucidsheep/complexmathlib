using UnityEngine;
using System.Collections;

public class Boat : MonoBehaviour
{
	public Color idealColor;
	public float colorDiffAllowed = .1f;

	// Use this for initialization
	void Start()
	{
		
	}

	// Update is called once per frame
	void Update()
	{
		float bestDiff = 99f;
		Vector2 bestVec = Vector2.zero;
		for(int x = -1; x <= 1; x++)
        {
			for(int y = -1; y <= 1; y++)
            {
				Vector2 thisPos = new Vector2(transform.position.x + (x * .01f), transform.position.y + (y * .01f));
				Color thisColor = Plotter.GetPixelColor(coordinateToPixel(thisPos.x), coordinateToPixel(thisPos.y));
				float thisDif = GetColorDiff(idealColor, thisColor);
				if(thisDif < bestDiff)
                {
					bestDiff = thisDif;
					bestVec = thisPos;
                }
            }
        }
		if(bestDiff <= colorDiffAllowed)
        {
			transform.position = bestVec;
        }
	}

	static int coordinateToPixel(float coord)
    {
		return Mathf.FloorToInt(((coord + 1f) / 2f) * 512);
    }
	static float GetColorDiff(Color color1, Color color2)
    {
		return Mathf.Abs(color1.r - color2.r) + Mathf.Abs(color1.g - color2.g) + Mathf.Abs(color1.b - color2.b);
    }
}

