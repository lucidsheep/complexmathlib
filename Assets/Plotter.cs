using UnityEngine;
using System.Collections.Generic;
using ComplexMathLib;
using UnityEngine.UI;

public class Plotter : MonoBehaviour
{
	public static bool USE_TOUCH_INPUT = false;

	static Plotter instance;

	public enum PixelStyle { Fill, Circles, Squares }
	public SpriteRenderer graph;
	Material shader { get { return graph.material; } }
	public Node nodeTemplate;
	public Line lineTemplate;
	public Color[] colors;
	public ComplexNumber A, B, C;
	public List<Node> zeroes;
	public List<Node> poles;

	public List<Line> lines;
	public RawImage graphPixels;
	public int resolution = 512;
	[Range(1, 8)]
	public int pixelZoomLevel = 1;
	[Range(0, 10)]
	public int contourLevel = 0;
	public bool useTexture = false;
	public PixelStyle pixelStyle = PixelStyle.Fill;

	bool[] touchesDown;
	int lastZoom;
	int lastContour;
	PixelStyle lastStyle;
	Node anchorNode;
	Node constantNode;

	ComputeBuffer poleBuffer, zeroBuffer;
	Texture2D oldTex;
	public static bool poleMoved = false;
	public static bool zeroMoved = false;
	public static bool anchorMoved = false;
	public static bool constantMoved = false;
	public static int maxNodes = 8;

    private void Awake()
    {
		instance = this;
    }
    void Start()
	{
		ComplexNumber x = new ComplexNumber(); //r = 0; i = 0;
		ComplexNumber y = new ComplexNumber(3f, -1f); //r = 3 i = -1
		ComplexNumber z = 3f; //r = 3f i = 0
		z.i = 5f;
		z.Pow(2);

		zeroes = new List<Node>();
		poles = new List<Node>();

		poleBuffer = new ComputeBuffer(32, sizeof(float)); //poleBuffer[0] = [1.0, -1.0, 2.0, 3.0]
		zeroBuffer = new ComputeBuffer(32, sizeof(float));

		zeroBuffer.SetData(new float[] { });
		poleBuffer.SetData(new float[] { });

		poleMoved = zeroMoved = true; //force initial update
		lastZoom = pixelZoomLevel;
		lastContour = contourLevel;
		lastStyle = pixelStyle;
		touchesDown = new bool[10];
		for (int i = 0; i < 10; i++)
			touchesDown[i] = false;

		if (!USE_TOUCH_INPUT)
			CreateNode(Node.Type.Zero);
	}

	private void Update()
	{
		bool zero = false;
		bool pole = false;
		bool zoom = lastZoom != pixelZoomLevel;
		bool contour = lastContour != contourLevel;
		bool style = lastStyle != pixelStyle;
		if (USE_TOUCH_INPUT)
		{
			Dictionary<int, Node> existingTouches = new Dictionary<int, Node>();
			foreach (var n in zeroes)
				existingTouches.Add(n.touchID, n);
			foreach (var n in poles)
				existingTouches.Add(n.touchID, n);
			foreach (var touch in Input.touches)
			{
				var node = existingTouches.ContainsKey(touch.fingerId) ? existingTouches[touch.fingerId] : null;
				if (node == null)
				{ // new touch
					bool isZero = existingTouches.Count == 0 ? true : touch.position.x < (Screen.height / 2); //first touch is always a zero
					node = CreateNode(isZero ? Node.Type.Zero : Node.Type.Pole, touch.fingerId);
					zero = isZero ? true : zero;
					pole = isZero ? pole : true;
					node.OnNodeMoved(touch.position);
					//existingTouches.Add(touch.fingerId, node);
				}
				
				if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
				{
					pole = node.type == Node.Type.Pole ? true : pole;
					zero = node.type == Node.Type.Zero ? true : zero;
					existingTouches.Remove(node.touchID);
					RemoveNode(node);
					
				}
				else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
				{
					//Debug.Log("Moved");
					node.OnNodeMoved(touch.position);
					existingTouches.Remove(node.touchID);
				}
			}
			//anything left in existingTouches is a stale node and should be removed
			foreach(var kv in existingTouches)
            {
				pole = kv.Value.type == Node.Type.Pole ? true : pole;
				zero = kv.Value.type == Node.Type.Zero ? true : zero;
				RemoveNode(kv.Value);
            }
		}
		else
		{
			zero = Input.GetKeyDown(KeyCode.Z);
			pole = Input.GetKeyDown(KeyCode.P);
			if (pole || zero)
			{
				CreateNode(zero ? Node.Type.Zero : Node.Type.Pole);
			}
		}
		if (poleMoved || pole)
			UpdateBuffer(false);
		if (zeroMoved || zero)
			UpdateBuffer(true);
		if (zoom)
			lastZoom = pixelZoomLevel;
		if (contour)
			lastContour = contourLevel;
		if (style)
			lastStyle = pixelStyle;
		if (poleMoved || zeroMoved || anchorMoved || constantMoved ||  pole || zero || zoom || contour || style)
		{
			poleMoved = zeroMoved = anchorMoved = constantMoved = false;
			shader.SetInteger("InputSize", resolution);
			shader.SetFloatArray("Poles", GetBufferArray(false));
			shader.SetFloatArray("Zeroes", GetBufferArray(true));
			shader.SetInt("NumZeroes", zeroes.Count);
			shader.SetInt("NumPoles", poles.Count);
			shader.SetInt("PixelZoom", Mathf.FloorToInt(Mathf.Pow(2f, pixelZoomLevel - 1)));
			shader.SetInt("PixelStyle", (int)pixelStyle);
			shader.SetInt("UseTexture", useTexture ? 1 : 0);
			shader.SetFloat("Contours", contourLevel == 0 ? 0f : 2f - ((contourLevel - 1) * .1f));
			if (anchorNode != null)
				shader.SetVector("Anchor", new Vector4(anchorNode.transform.position.x, anchorNode.transform.position.y, 0f));
			else
				shader.SetVector("Anchor", new Vector4(0f, 0f, -1f));
			if(constantNode != null)
				shader.SetVector("Constant", new Vector4(constantNode.transform.position.x, constantNode.transform.position.y, 0f));
			else
				shader.SetVector("Constant", new Vector4(0f, 0f, -1f));
		}	



	}

	Node CreateNode(Node.Type type, int touchID = 0)
    {
		bool pole = type == Node.Type.Pole;
		bool zero = type == Node.Type.Zero;
		bool anchor = type == Node.Type.Anchor;
		bool constant = type == Node.Type.Constant;

		Color colToUse = zero ? colors[0] : pole ? colors[1] : anchor ? colors[2] : colors[3];

		if (pole && poles.Count >= maxNodes) return null;
		if (zero && zeroes.Count >= maxNodes) return null;
		if (anchor && anchorNode != null) return null;
		if (constant && constantNode != null) return null;

		var node = Instantiate(nodeTemplate);
		node.sprite.color = colToUse;
		node.SetType(type);
		if(USE_TOUCH_INPUT)
        {
			node.touchID = touchID;
			node.SetVisible(false);
        }
		if (pole)
			poles.Add(node);
		else if (zero)
			zeroes.Add(node);
		else if (anchor)
			anchorNode = node;
		else if (constant)
			constantNode = node;
		return node;
	}
	void RemoveNode(Node node)
    {
		if (node.type == Node.Type.Pole)
			poles.Remove(node);
		else if (node.type == Node.Type.Zero)
			zeroes.Remove(node);
		else if (node.type == Node.Type.Anchor)
			anchorNode = null;
		else if (node.type == Node.Type.Constant)
			constantNode = null;

		Destroy(node.gameObject);
	}
    private void OnDestroy()
    {
		zeroBuffer.Release();
		poleBuffer.Release();
    }
    void UpdateBuffers()
    {
		UpdateBuffer(true);
		UpdateBuffer(false);
    }
	void UpdateBuffer(bool zero)
    {
		var newBuffer = GetBufferArray(zero);
		if (zero)
		{
			//zeroBuffer.Release();
			zeroBuffer.SetData(newBuffer);
		}
		else
		{
			//poleBuffer.Release();
			poleBuffer.SetData(newBuffer);
		}
    }
	float[] GetBufferArray(bool zero)
    {
		var listToUse = zero ? zeroes : poles;
		float[] newBuffer = new float[maxNodes * 2];
		for (int i = 0; i < listToUse.Count; i++)
		{
			newBuffer[i * 2] = listToUse[i].transform.position.x;
			newBuffer[i * 2 + 1] = listToUse[i].transform.position.y;
		}
		return newBuffer;
	}
	ComplexNumber Derivative(ComplexNumber z)
    {
        float aX = 0f, aY = 0f, bX = 0f, bY = 0f, cX = 0f, cY = 0f;
		ComplexNumber a = new ComplexNumber(aX, aY);
		ComplexNumber b = new ComplexNumber(bX, bY);
		ComplexNumber c = new ComplexNumber(cX, cY);

		return (((a * (c - b)) + (b * c)) + (z * (z - (2f * c)))) / ComplexNumber.Pow(2, z - c);
    }

	public static Node CreateNodeFromButton(Node.Type type)
    {
		return instance.CreateNode(type);
    }

	public static void RemoveNodeFromDrag(Node node)
    {
		instance.RemoveNode(node);
    }

	public static void ChangeContourFromButton(int delta)
    {
		instance.contourLevel = Mathf.Clamp(instance.contourLevel + delta, 0, 10);
    }
	public static void ChangeTextureFromButton(bool val)
	{
		instance.useTexture = val;
		poleMoved = true;
	}
}

