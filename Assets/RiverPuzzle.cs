using UnityEngine;
using System.Collections;

[CreateAssetMenu()]
public class RiverPuzzle : CMPuzzle
{
	public int numRivers;
    public float riverWidth = 0.1f;
    public float riverSpeed = 1.0f;

    public override void Setup(Plotter game, Material material)
    {
        base.Setup(game, material);
        material.SetInt("Rivers", numRivers);
        material.SetFloat("RiverWidth", riverWidth);
        material.SetFloat("ContourSpeed", riverSpeed);
        game.contourLevel = 5;
    }
}

