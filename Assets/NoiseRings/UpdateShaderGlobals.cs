using System.Collections.Generic;
using UnityEngine;

namespace NoiseRings
{

	[ExecuteInEditMode]
	public class UpdateShaderGlobals : MonoBehaviour
	{
		static Matrix4x4[] clipToWorldMatrices;

		static class Uniforms
		{
			public static readonly int ClipToWorld = Shader.PropertyToID("NoiseRingGlobals_ClipToWorld");
		}

		void OnEnable ()
		{
			Camera.onPreRender += CameraUpdate;
		}

		void OnDisable ()
		{
			Camera.onPreRender -= CameraUpdate;
		}

		static void CameraUpdate (Camera cam)
		{
			// ClipToWorld update
			if (clipToWorldMatrices == null)
				clipToWorldMatrices = new Matrix4x4[2];

			if (cam.stereoEnabled)
			{
				Matrix4x4 leftProj = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
				Matrix4x4 leftView = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
				Matrix4x4 rightProj = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
				Matrix4x4 rightView = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
				clipToWorldMatrices[0] = CalculateClipToWorld(leftProj, leftView);
				clipToWorldMatrices[1] = CalculateClipToWorld(rightProj, rightView);
			}
			else
			{
				clipToWorldMatrices[0] = CalculateClipToWorld(cam.projectionMatrix, cam.worldToCameraMatrix);
				clipToWorldMatrices[1] = Matrix4x4.identity;
			}

			Shader.SetGlobalMatrixArray(Uniforms.ClipToWorld, clipToWorldMatrices);
		}

		static Matrix4x4 CalculateClipToWorld (Matrix4x4 proj, Matrix4x4 view)
		{
			Matrix4x4 p = GL.GetGPUProjectionMatrix(proj, true);
			p[2, 3] = p[3, 2] = 0.0f;
			p[3, 3] = 1.0f;
			return Matrix4x4.Inverse(p * view) * Matrix4x4.TRS(new Vector3(0, 0, -p[2,2]), Quaternion.identity, Vector3.one);
		}
	}

} //namespace NoiseRings