using UnityEngine;
using System.Collections.Generic;
using ComplexMathLib;

public class CMPuzzle : ScriptableObject
{
	public enum PlayerActions { PlaceZero, PlacePole, MoveZero, MovePole, DeleteZero, DeletePole, MoveScatter, MoveAnchor, EditContours}
	public List<PlayerActions> allowedActions;
	public ComplexNumber[] startingZeros;
	public ComplexNumber[] startingPoles;

	public virtual void Setup(Plotter game, Material material) {
		foreach(var z in startingZeros)
        {
			var n = game.CreateNode(Node.Type.Zero);
			n.value = z;
        }
		foreach(var p in startingPoles)
        {
			var n = game.CreateNode(Node.Type.Pole);
			n.value = p;
        }
	}
	public virtual bool CheckForWin() { return false; }
}

