using UnityEngine;
using System.Collections;

public class Line : MonoBehaviour
{
	public Node n1, n2;
	public LineRenderer lr;
	// Use this for initialization
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		lr.SetPosition(0, n1 == null ? Vector3.zero : n1.transform.position);
		lr.SetPosition(1, n2 == null ? Vector3.zero : n2.transform.position);
	}
}

