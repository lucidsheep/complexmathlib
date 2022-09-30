using UnityEngine;
using System.Collections;

public class NodeSpawner : MonoBehaviour
{
	public Node.Type type;
	
	// Use this for initialization
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
			
	}

    private void OnMouseDown()
    {
		Node newNode = Plotter.CreateNodeFromButton(type);
		if (newNode != null)
			newNode.SetDragged(true, Input.mousePosition);
    }
}

