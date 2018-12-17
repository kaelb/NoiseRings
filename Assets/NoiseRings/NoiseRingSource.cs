using System.Collections.Generic;
using UnityEngine;

namespace NoiseRings
{

	[ExecuteInEditMode]
	public class NoiseRingSource : MonoBehaviour
	{
		//
		// Settings
		//
		[Header("Settings")]
		[SerializeField] int numberOfSegments = 128;
		[SerializeField] int numberOfRings = 4;
		[SerializeField] float radialOffset = 0.1f;
		[SerializeField] Texture2D lineTexture = null;
		[SerializeField] Color color = Color.white;
		[SerializeField] float multiplier = 1.0f;
		[SerializeField] float lineWidth = 0.1f;
		[SerializeField] float noiseScale = 0.7f;
		[SerializeField] float noiseHeight = 3.0f;
		[SerializeField] float detailNoiseScale = 4.0f;
		[SerializeField] float detailNoiseHeight = 0.3f;
		[SerializeField] float speed = 1.0f;

		//
		// Animation settings
		//
		[Space]
		[Header("Animation Settings")]
		[SerializeField] float animationDuration = 1.75f;
		[SerializeField] float endRadius = 10.0f;
		[SerializeField] AnimationCurve intensityCurve = null;
		[SerializeField] float maxHeight = 1.0f;
		[SerializeField] AnimationCurve heightCurve = null;

		//
		// Debug
		//
		[Space]
		[Header("Debug")]
		[SerializeField] bool testTrigger = false;


		[SerializeField] [HideInInspector] Mesh ringMesh;
		Material ringMaterial;
		MaterialPropertyBlock propertyBlock;

		//
		// Mesh generation variables
		//
		Vector3[] vertices;
		Vector3[] prevVertices;
		Vector3[] nextVertices;
		Vector3[] uvsAndOrientations;
		int[] triangles;

		//
		// Animation variables
		//
		float animationTime = 0.0f;
		float animationProgress = 0.0f;
		bool animating = false;

		float noiseTime = 0.0f;
		float currentLineWidth;

		static class Uniforms
		{
			public static readonly int LineTexture = Shader.PropertyToID("_LineTexture");
			public static readonly int Color = Shader.PropertyToID("_Color");
			public static readonly int Multiplier = Shader.PropertyToID("_Multiplier");
			public static readonly int LineWidth = Shader.PropertyToID("_LineWidth");
			public static readonly int Radius = Shader.PropertyToID("_Radius");
			public static readonly int Height = Shader.PropertyToID("_Height");
			public static readonly int Intensity = Shader.PropertyToID("_Intensity");
			public static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");
			public static readonly int NoiseHeight = Shader.PropertyToID("_NoiseHeight");
			public static readonly int DetailNoiseScale = Shader.PropertyToID("_DetailNoiseScale");
			public static readonly int DetailNoiseHeight = Shader.PropertyToID("_DetailNoiseHeight");
			public static readonly int Speed = Shader.PropertyToID("_Speed");
			public static readonly int NoiseTime = Shader.PropertyToID("_NoiseTime");
		}


		void OnEnable ()
		{
			Shader ringShader = Shader.Find("Hidden/NoiseRing");
			if (ringShader != null)
			{
				ringMaterial = new Material(ringShader);
				ringMaterial.hideFlags = HideFlags.HideAndDontSave;

				propertyBlock = new MaterialPropertyBlock();
			}
			else
				Debug.LogError("Cannot find Hidden/NoiseLine shader");

			currentLineWidth = lineWidth * transform.lossyScale.z;

			Camera.onPreRender += HandlePreRender;
		}

		void OnDisable ()
		{
			Camera.onPreRender -= HandlePreRender;
		}
		
		void Update ()
		{
			if (Application.isEditor && !Application.isPlaying)
			{
				EditorUpdate();
				return;
			}

			if (ringMesh == null)
				return;

			if (testTrigger)
			{
				testTrigger = false;
				Trigger();
			}

			noiseTime += Time.deltaTime;
			
			if (!animating)
				return;

			animationTime += Time.deltaTime;

			if (animationTime > animationDuration)
			{
				animating = false;
				return;
			}

			animationProgress = animationTime / animationDuration;

			// line width follows lossyScale
			currentLineWidth = lineWidth * transform.lossyScale.z;

			// calculate maxium possible mesh bounds
			float maxBoundsWidth = currentLineWidth + 2.0f * endRadius;
			float maxBoundsHeight = currentLineWidth + maxHeight + noiseHeight + detailNoiseHeight;
			ringMesh.bounds = new Bounds(
				new Vector3(0.0f, 0.0f, 0.5f * maxBoundsHeight),
				new Vector3(maxBoundsWidth, maxBoundsWidth, maxBoundsHeight)
			);

			float radius;
			float radialProgress;
			float height;
			float intensity;
			for (int i = 0; i < numberOfRings; i++)
			{
				radius = Mathf.Lerp(-radialOffset * i, endRadius, animationProgress);
				if (radius < 0.0f)
					continue;
				
				radialProgress = radius / endRadius;
				height = maxHeight * heightCurve.Evaluate(radialProgress);
				intensity = intensityCurve.Evaluate(radialProgress);

				propertyBlock.SetFloat(Uniforms.Radius, radius);
				propertyBlock.SetFloat(Uniforms.Height, height);
				propertyBlock.SetFloat(Uniforms.Intensity, intensity);

				Graphics.DrawMesh(
					ringMesh,
					transform.localToWorldMatrix,
					ringMaterial,
					LayerMask.NameToLayer("Default"),
					null,
					0,
					propertyBlock,
					false,
					false
				);
			}
		}

		public void Trigger ()
		{
			animationTime = 0.0f;
			animating = true;
		}

		public void EndAnimation ()
		{
			animating = false;
		}

		public float AnimationDuration ()
		{
			return animationDuration;
		}

		void HandlePreRender (Camera cam)
		{
			if (ringMaterial == null || ringMesh == null)
				return;

			ringMaterial.SetTexture(Uniforms.LineTexture, lineTexture);
			ringMaterial.SetColor(Uniforms.Color, color);
			ringMaterial.SetFloat(Uniforms.Multiplier, multiplier);
			ringMaterial.SetFloat(Uniforms.LineWidth, currentLineWidth);
			ringMaterial.SetFloat(Uniforms.NoiseScale, noiseScale);
			ringMaterial.SetFloat(Uniforms.NoiseHeight, noiseHeight);
			ringMaterial.SetFloat(Uniforms.DetailNoiseScale, detailNoiseScale);
			ringMaterial.SetFloat(Uniforms.DetailNoiseHeight, detailNoiseHeight);
			ringMaterial.SetFloat(Uniforms.Speed, speed);
			ringMaterial.SetFloat(Uniforms.NoiseTime, noiseTime);
		}

		void EditorUpdate ()
		{
				if (numberOfSegments < 3)
					numberOfSegments = 3;
				
				if (numberOfRings < 1)
					numberOfRings = 1;

				if (vertices == null || vertices.Length != 2 * numberOfSegments)
					GenerateMesh();
		}

		// Generates a unit circle mesh with the specified number of segments.
		// The mesh is transformed and expanded appropriately in the
		// NoiseRing.shader line renderer. Each vertex also stores the position
		// of the previous and next vertex in the ring to supply to the line
		// renderer.
		void GenerateMesh ()
		{
			vertices = new Vector3[numberOfSegments * 2];
			prevVertices = new Vector3[numberOfSegments * 2];
			nextVertices = new Vector3[numberOfSegments * 2];
			uvsAndOrientations = new Vector3[numberOfSegments * 2];
			triangles = new int[numberOfSegments * 6];

			float theta;
			Vector3 vertex;
			for (int i = 0; i < numberOfSegments * 2; i += 2)
			{
				theta = (2.0f * Mathf.PI) * (i / 2) / numberOfSegments;
				vertex = new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0.0f);
				vertices[i] = vertex;
				vertices[i + 1] = vertex;
				prevVertices[(i + 2) % (numberOfSegments * 2)] = vertex;
				prevVertices[(i + 3) % (numberOfSegments * 2)] = vertex;
				nextVertices[(i - 2 < 0) ? numberOfSegments * 2 - 2 : i - 2] = vertex;
				nextVertices[(i - 1 < 0) ? numberOfSegments * 2 - 1 : i - 1] = vertex;
				uvsAndOrientations[i] = new Vector3(0.5f, 1.0f, 1.0f);
				uvsAndOrientations[i + 1] = new Vector3(0.5f, 0.0f, -1.0f);

				int tri = i * 3;
				triangles[tri] = i;
				triangles[tri + 1] = (i + 2) % (numberOfSegments * 2);
				triangles[tri + 2] = i + 1;
				triangles[tri + 3] = i + 1;
				triangles[tri + 4] = (i + 2) % (numberOfSegments * 2);
				triangles[tri + 5] = (i + 3) % (numberOfSegments * 2);
			}

			if (ringMesh != null)
				DestroyImmediate(ringMesh);

			ringMesh = new Mesh();
			ringMesh.vertices = vertices;
			ringMesh.SetUVs(0, new List<Vector3>(prevVertices));
			ringMesh.SetUVs(1, new List<Vector3>(nextVertices));
			ringMesh.SetUVs(2, new List<Vector3>(uvsAndOrientations));
			ringMesh.triangles = triangles;
		}
	}

} //namespace NoiseRings