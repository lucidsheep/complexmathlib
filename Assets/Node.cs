using UnityEngine;
using System.Collections.Generic;
using ComplexMathLib;

public class Node : MonoBehaviour
{
	public enum Type { Control, Adder, Multiplier, Pole, Zero }
	public ComplexNumber value;
	public SpriteRenderer sprite;
	public SpriteRenderer outline;

	ComplexNumber cachedValue;
	Type type = Type.Control;
	List<Node> sources = new List<Node>();
	bool beingDragged = false;

	public bool canBeControlled { get { return type == Type.Zero || type == Type.Control || type == Type.Pole; } }
	// Use this for initialization
	void Awake()
	{
		outline.enabled = true;
	}

	public void SetType(Type t, Node n1, Node n2) { type = t; sources.Add(n1); sources.Add(n2); outline.enabled = t == Type.Control; transform.position = new Vector3(value.r, value.i, 1f); }

	public void SetType(Type t) { type = t; }
	// Update is called once per frame
	void Update()
	{
		if (canBeControlled)
		{
			if(beingDragged)
            {
				Vector3 newTarget = Input.mousePosition;
				var newPos = Camera.main.ScreenToWorldPoint(newTarget);
				value = new ComplexNumber(newPos.x, newPos.y);
				if (type == Type.Pole) Plotter.poleMoved = true;
				else if (type == Type.Zero) Plotter.zeroMoved = true;
			}

		} else if(type == Type.Adder)
        {
			value = new ComplexNumber();
			foreach (var cn in sources)
				value += cn.value;
        } else if(type == Type.Multiplier)
        {
			value = sources[0].value;
			for (int i = 1; i < sources.Count; i++)
				value *= sources[i].value;
		}

		if (cachedValue != value)
		{
			transform.position = position;
			cachedValue = value;
		}
	}

    private void OnMouseDown()
    {
		if (canBeControlled)
			beingDragged = true;
    }

    private void OnMouseUp()
    {
		beingDragged = false;
    }
    public Vector3 position
    {
        get
        {
			return new Vector3(value.r, value.i, transform.position.z);
        }
    }
}

