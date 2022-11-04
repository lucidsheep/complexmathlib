using UnityEngine;
using System.Collections.Generic;
using ComplexMathLib;
using UnityEngine.UI;

public class Plotter : MonoBehaviour
{
	public static bool USE_TOUCH_INPUT = false;

	static Plotter instance;

	public enum PixelStyle { Fill, Circles, Squares }
	public CMPuzzle currentPuzzle;
	public SpriteRenderer graph;
	Material shader { get { return graph.material; } }
	public Node nodeTemplate;
	public Line lineTemplate;
	public Color[] colors;
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
	public ComplexNumber Anchor;
	public ComplexNumber Scatter;
	public bool alwaysUpdate = false;
	public PixelStyle pixelStyle = PixelStyle.Fill;

	ComplexNumber startAnchorDragValue;
	public bool anchorDragMode = false;
	bool anchorDragInProgress = false;

	ComplexNumber startScatterDragValue;
	public bool scatterDragMode = false;
	bool scatterDragInProgress = false;

	public static bool inScatterMode { get { return instance.scatterDragMode; } }
	public static bool inAnchorMode { get { return instance.anchorDragMode; } }
	//zeroes and poles are stored as buffers to send to the shader
	//each is an array of floats, every two floats is a pair that represents the (r,i) of a pole or buffer
	//[.5, -.5, 0, 2] = two complex numbers, (.5 - .5i) and (2i)
	ComputeBuffer poleBuffer, zeroBuffer;

	public static bool poleMoved = false;
	public static bool zeroMoved = false;
	public static int maxNodes = 8;

	RenderTexture rt;
	Texture2D rtCanvas;

    private void Awake()
    {
		instance = this;
    }
    void Start()
	{
		rt = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 24);
		rtCanvas = new Texture2D(Camera.main.pixelWidth, Camera.main.pixelHeight, TextureFormat.RGB24, false);
		Debug.Log("res " +rt.width + " x " + rt.height);
		zeroes = new List<Node>();
		poles = new List<Node>();

		poleBuffer = new ComputeBuffer(32, sizeof(float)); //poleBuffer[0] = [1.0, -1.0, 2.0, 3.0]
		zeroBuffer = new ComputeBuffer(32, sizeof(float));

		zeroBuffer.SetData(new float[] { });
		poleBuffer.SetData(new float[] { });

		poleMoved = zeroMoved = true; //force initial update

		SetupMaterial();

		if (currentPuzzle != null)
			currentPuzzle.Setup(this, shader);
		else if (!USE_TOUCH_INPUT)
			CreateNode(Node.Type.Zero); //create initial zero in center
	}

	void SetupMaterial()
    {
		//shader values that are set once and not changed every frame (some puzzle types may modify them)
		shader.SetInt("Rivers", 0);
		shader.SetFloat("ContourSpeed", 0f);
    }

	private void Update()
	{
		//temporarily move the graph to the top layer so game objects (nodes, obstacles etc) don't interrupt color analysis
		graph.sortingLayerName = "Render";
		Camera.main.targetTexture = rt;
		//re-render the camera onto the renderTexture
		Camera.main.Render();
		Camera.main.targetTexture = null;
		graph.sortingLayerName = "Default";

		//draw the renderTexture onto a separate canvas that can be used to examine color data
		RenderTexture.active = rt;
		rtCanvas.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
		RenderTexture.active = null;

		bool zero = false;
		bool pole = false;
		//todo - probably does not work in touch mode
		if(anchorDragMode)
        {
			var newPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			if (Input.GetMouseButtonDown(0)) //begin drag, solve for cur position
            {
				startAnchorDragValue = myFunction(new ComplexNumber(newPos.x, newPos.y));
				anchorDragInProgress = true;
			} else if(anchorDragInProgress)
            {
				var curAnchorDragValue = startAnchorDragValue / (myFunction(new ComplexNumber(newPos.x, newPos.y)));
				Anchor = Anchor * curAnchorDragValue;
				//keep ratio of real to imaginary, but constrain to a reasonable number
				Anchor = Anchor.NormalizeScalar(1000f);
				if (Mathf.Abs(Anchor.r) < 0.000001f && Mathf.Abs(Anchor.i) < 0.000001f)
					Anchor.r = 0.000001f; //avoid zeroing out anchor entirely
				if (Input.GetMouseButtonUp(0)) //end drag
                {
					anchorDragInProgress = false;
                }
			}
        }
		if(scatterDragMode)
        {
			var newPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			if (Input.GetMouseButtonDown(0)) //begin drag, solve for cur position
			{
				startScatterDragValue = myFunction(new ComplexNumber(newPos.x, newPos.y));
				scatterDragInProgress = true;
			}
			else if (scatterDragInProgress)
			{
				var curScatterDragValue = startScatterDragValue - (myFunction(new ComplexNumber(newPos.x, newPos.y)));
				Scatter = Scatter + curScatterDragValue;
				//Scatter = Scatter.NormalizeLinear();
				if (Input.GetMouseButtonUp(0)) //end drag
				{
					scatterDragInProgress = false;
				}
			}
		}
		//in touch mode, each node is assigned to a finger ID, which it stays attached to
		//node is destroyed when finger is removed
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
		//only update buffers if a pole or zero was moved / added / removed
		if (poleMoved || pole)
			UpdateBuffer(false);
		if (zeroMoved || zero)
			UpdateBuffer(true);

		//update all shader values
		poleMoved = zeroMoved = false;
		shader.SetInteger("CanvasSize", resolution);
		shader.SetFloatArray("Poles", GetBufferArray(false));
		shader.SetFloatArray("Zeroes", GetBufferArray(true));
		shader.SetInt("NumZeroes", zeroes.Count);
		shader.SetInt("NumPoles", poles.Count);
		shader.SetInt("PixelZoom", Mathf.FloorToInt(Mathf.Pow(2f, pixelZoomLevel - 1)));
		shader.SetInt("PixelStyle", (int)pixelStyle);
		shader.SetInt("UseTexture", useTexture ? 1 : 0);
		shader.SetFloat("Contours", contourLevel == 0 ? 0f : 2f - ((contourLevel - 1) * .1f));
		shader.SetVector("Anchor", new Vector4(Anchor.r, Anchor.i, 0f));
		shader.SetVector("Scatter", new Vector4(Scatter.r, Scatter.i, 0f));

	}

	public Node CreateNode(Node.Type type, int touchID = 0)
    {
		if (!canManipulateNodes) return null;

		bool pole = type == Node.Type.Pole;
		bool zero = type == Node.Type.Zero;

		Color colToUse = zero ? colors[0] : pole ? colors[1] : colors[2];

		if (pole && poles.Count >= maxNodes) return null;
		if (zero && zeroes.Count >= maxNodes) return null;

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
		return node;
	}
	void RemoveNode(Node node)
    {
		if (!canManipulateNodes) return;

		if (node.type == Node.Type.Pole)
			poles.Remove(node);
		else if (node.type == Node.Type.Zero)
			zeroes.Remove(node);

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

	public ComplexNumber myFunction(ComplexNumber z)
    {

		if (zeroes.Count == 0)
			return z * Anchor + Scatter;

		ComplexNumber numerator = z - zeroes[0].value;

		for (var i = 1; i < zeroes.Count; i++)
		{
			numerator = numerator * (z - zeroes[i].value);
		}
		if (poles.Count == 0)
			return numerator * Anchor + Scatter;

		ComplexNumber denom = z - poles[0].value;
		for (var j = 1; j < poles.Count; j++)
		{
			denom = denom * (z - poles[j].value);
		}
		return ((numerator / denom) * Anchor) + Scatter;

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

	public static void ChangeAnchorModeFromButton(bool val)
    {
		instance.anchorDragMode = val;
    }

	public static void ChangeScatterModeFromButton(bool val)
    {
		instance.scatterDragMode = val;
    }

	public static bool canManipulateNodes { get {

			return !(instance.scatterDragMode || instance.anchorDragMode);
		}
    }

	public static Color GetPixelColor(int x, int y)
    {
		return instance.rtCanvas.GetPixel(x, y);
    }

	public static bool IsActionAllowed(CMPuzzle.PlayerActions action)
    {
		if (instance.currentPuzzle == null) return true;

		return instance.currentPuzzle.allowedActions.FindIndex(x => x == action) >= 0;
    }
}

