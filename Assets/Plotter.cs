using UnityEngine;
using System.Collections.Generic;
using ComplexMathLib;
using UnityEngine.UI;

public class Plotter : MonoBehaviour
{
	public enum PixelStyle { Fill, Circles, Squares }
	public ComputeShader plotShader;
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
	public PixelStyle pixelStyle = PixelStyle.Fill;

	int lastZoom;
	int lastContour;
	PixelStyle lastStyle;

	ComputeBuffer poleBuffer, zeroBuffer;
	Texture2D oldTex;
	public static bool poleMoved = false;
	public static bool zeroMoved = false;
	public static int maxNodes = 8;
	// Use this for initialization
	void Start()
	{

		ComplexNumber x = new ComplexNumber(); //r = 0; i = 0;
		ComplexNumber y = new ComplexNumber(3f, -1f); //r = 3 i = -1
		ComplexNumber z = 3f; //r = 3f i = 0
		z.i = 5f;
		z.Pow(2);
		Debug.Log(z.ToString());
		Debug.Log(z);
		Debug.Log("complex numbers\nA= " + A + "\nB= " + B + "\nC= " + C);
		Debug.Log("A+B= " + (A + B) + "\nB+C= " + (B + C));
		Debug.Log("A-B= " + (A - B) + "\nB-C= " + (B - C));
		Debug.Log("A*B= " + (A * B) + "\nB*C= " + (B * C));
		Debug.Log("A/B= " + (A / B) + "\nB/C= " + (B / C));
		Debug.Log("C^3= " + C.Pow(3));

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
	}

	private void Update()
	{
		bool zero = Input.GetKeyDown(KeyCode.Z);
		bool pole = Input.GetKeyDown(KeyCode.P);
		bool zoom = lastZoom != pixelZoomLevel;
		bool contour = lastContour != contourLevel;
		bool style = lastStyle != pixelStyle; 
		Color colToUse = zero ? colors[0] : colors[1];
		if (pole || zero)
		{
			if (pole && poles.Count >= maxNodes) return;
			if (zero && zeroes.Count >= maxNodes) return;

			var node = Instantiate(nodeTemplate);
			node.sprite.color = colToUse;
			node.SetType(pole ? Node.Type.Pole : Node.Type.Zero);
			if (pole)
				poles.Add(node);
			else
				zeroes.Add(node);
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
		if (poleMoved || zeroMoved || pole || zero || zoom || contour || style)
		{
			poleMoved = zeroMoved = false;
			plotShader.SetInt("InputSize", resolution);
			plotShader.SetBuffer(0, "Poles", poleBuffer);
			plotShader.SetBuffer(0, "Zeroes", zeroBuffer);
			plotShader.SetInt("NumZeroes", zeroes.Count);
			plotShader.SetInt("NumPoles", poles.Count);
			plotShader.SetInt("PixelZoom", Mathf.FloorToInt(Mathf.Pow(2f, pixelZoomLevel - 1)));
			plotShader.SetInt("PixelStyle", (int)pixelStyle);
			plotShader.SetFloat("Contours", contourLevel == 0 ? 0f : 2f - ((contourLevel - 1) * .1f));
			RenderTexture tex = new RenderTexture(resolution, resolution, 24);
			Texture2D tex2d = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
			tex.enableRandomWrite = true;
			tex.Create();
			plotShader.SetTexture(0, "Output", tex);
			plotShader.Dispatch(0, resolution, resolution, 1);

			RenderTexture.active = tex;
			tex2d.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
			tex2d.Apply();
			RenderTexture.active = null;
			graphPixels.texture = tex2d;

			tex.Release();
			if (oldTex != null) Destroy(oldTex);
			oldTex = tex2d;
		}



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
			var listToUse = zero ? zeroes : poles;
		float[] newBuffer = new float[listToUse.Count * 2];
		for (int i = 0; i < listToUse.Count; i++)
		{
			newBuffer[i*2] = listToUse[i].transform.position.x;
			newBuffer[i * 2 + 1] = listToUse[i].transform.position.y;
		}
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

	/*
	 * code for adder / multipliers
bool adder = Input.GetKeyDown(KeyCode.A);
bool multiplier = Input.GetKeyDown(KeyCode.M);
Color colToUse = adder ? colors[0] : colors[1];
if (adder || multiplier)
{
	var node = Instantiate(nodeTemplate);
	node.sprite.color = colToUse;
	nodes.Add(node);

	var line = Instantiate(lineTemplate);
	line.lr.startColor = line.lr.endColor = colToUse;
	line.n1 = node;
	lines.Add(line);

	var node2 = Instantiate(nodeTemplate);
	node2.sprite.color = colToUse;
	nodes.Add(node2);

	var line2 = Instantiate(lineTemplate);
	line2.lr.startColor = line2.lr.endColor = colToUse;
	line2.n1 = node2;
	lines.Add(line2);

	var combineNode = Instantiate(nodeTemplate);
	combineNode.sprite.color = colToUse;
	if(adder)
	{
		combineNode.SetType(Node.Type.Adder, node, node2);
		var line3 = Instantiate(lineTemplate);
		line3.lr.startColor = line3.lr.endColor = colToUse;
		line3.n1 = node; line3.n2 = combineNode;
		var line4 = Instantiate(lineTemplate);
		line4.lr.startColor = line4.lr.endColor = colToUse;
		line4.n1 = node2; line4.n2 = combineNode;
		lines.Add(line3); lines.Add(line4);
	} else if(multiplier)
	{
		combineNode.SetType(Node.Type.Multiplier, node, node2);
		var line5 = Instantiate(lineTemplate);
		line5.lr.startColor = line5.lr.endColor = colToUse;
		line5.n1 = combineNode;
		lines.Add(line5);
	}
	nodes.Add(combineNode);
}
*/
	ComplexNumber Derivative(ComplexNumber z)
    {
        float aX = 0f, aY = 0f, bX = 0f, bY = 0f, cX = 0f, cY = 0f;
		ComplexNumber a = new ComplexNumber(aX, aY);
		ComplexNumber b = new ComplexNumber(bX, bY);
		ComplexNumber c = new ComplexNumber(cX, cY);

		return (((a * (c - b)) + (b * c)) + (z * (z - (2f * c)))) / ComplexNumber.Pow(2, z - c);
    }
}

