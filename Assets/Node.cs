using UnityEngine;
using System.Collections.Generic;
using ComplexMathLib;

public class Node : MonoBehaviour
{
	public enum Type { Control, Adder, Multiplier, Pole, Zero, Anchor, Constant }
	public ComplexNumber value;
	public SpriteRenderer sprite;
	public SpriteRenderer outline;
	public int touchID = -1;

	ComplexNumber cachedValue;
	public Type type = Type.Control;
	List<Node> sources = new List<Node>();
	bool beingDragged = false;

	public bool canBeControlled { get {
			return
				(Plotter.IsActionAllowed(CMPuzzle.PlayerActions.MovePole) && type == Type.Pole)
				|| (Plotter.IsActionAllowed(CMPuzzle.PlayerActions.MoveZero) && type == Type.Zero)
				|| (Plotter.IsActionAllowed(CMPuzzle.PlayerActions.MoveAnchor) && type == Type.Anchor)
				|| (Plotter.IsActionAllowed(CMPuzzle.PlayerActions.MoveScatter) && type == Type.Constant); } }
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
				OnNodeMoved(Input.mousePosition);
			}
			if (Input.GetMouseButtonUp(0))
				OnMouseRelease();
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
			GetComponent<Rigidbody2D>().MovePosition(position);
			//transform.position = position;
			cachedValue = value;
		}
	}

	public void OnNodeMoved(Vector2 screenPosition)
    {
		var newPos = Camera.main.ScreenToWorldPoint(screenPosition);
		value = new ComplexNumber(newPos.x, newPos.y);
		if (type == Type.Pole) Plotter.poleMoved = true;
		else if (type == Type.Zero) Plotter.zeroMoved = true;
	}
    private void OnMouseDown()
    {
		if (Plotter.USE_TOUCH_INPUT)
			return;
		if (!Plotter.canManipulateNodes) return;

		SetDragged(true);
    }

    private void OnMouseRelease()
    {
		if (Plotter.USE_TOUCH_INPUT)
			return;
		if(beingDragged)
			SetDragged(false);
    }

	public void SetVisible(bool visible)
    {
		sprite.enabled = outline.enabled = visible;
    }

	public void SetDragged(bool newDrag)
    {
		if(canBeControlled)
			beingDragged = newDrag;
		if (!newDrag && Mathf.Max(Mathf.Abs(transform.position.x), Mathf.Abs(transform.position.y)) > .8f)
		{
			//remove nodes dragged out of canvas
			if ((type == Type.Pole && Plotter.IsActionAllowed(CMPuzzle.PlayerActions.DeletePole))
				|| (type == Type.Zero && Plotter.IsActionAllowed(CMPuzzle.PlayerActions.DeleteZero)))
				Plotter.RemoveNodeFromDrag(this);
		}
	}

	public void SetDragged(bool newDrag, Vector2 initPosition)
	{
		SetDragged(newDrag);
		if (newDrag)
			OnNodeMoved(initPosition);

	}

	public Vector3 position
    {
        get
        {
			return new Vector3(value.r, value.i, transform.position.z);
        }
    }
}

