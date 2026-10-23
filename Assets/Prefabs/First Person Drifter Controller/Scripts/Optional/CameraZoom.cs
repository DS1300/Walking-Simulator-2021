// by @torahhorse

using UnityEngine;
using System.Collections;

// allows player to zoom in the FOV when holding a button down
[RequireComponent (typeof (Camera))]
public class CameraZoom : MonoBehaviour
{
	public float zoomFOV = 30.0f;
	public float zoomSpeed = 9f;
	
	private float targetFOV;
	private float baseFOV;
	private float baseLodBias = -1f;
	
	void Start ()
	{
		SetBaseFOV(GetComponent<Camera>().fieldOfView);
	}
	
	void Update ()
	{
		if( Input.GetButton("Fire2") )
		{
			targetFOV = zoomFOV;
		}
		else
		{
			targetFOV = baseFOV;
		}
		
		UpdateZoom();
	}
	
	private void UpdateZoom()
	{
		GetComponent<Camera>().fieldOfView = Mathf.Lerp(GetComponent<Camera>().fieldOfView, targetFOV, zoomSpeed * Time.deltaTime);
		KeepLodWhileZoomed(GetComponent<Camera>().fieldOfView);
	}

	// Zooming makes trees and grass bigger on screen, which would switch them to more detailed LODs (branches
	// popping in). Scaling the LOD bias with the zoom keeps the LODs they had before zooming.
	private void KeepLodWhileZoomed(float fov)
	{
		if (fov >= baseFOV - 0.01f)
		{
			RestoreLodBias();
			return;
		}
		if (baseLodBias < 0f) baseLodBias = QualitySettings.lodBias;
		QualitySettings.lodBias = baseLodBias * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(baseFOV * 0.5f * Mathf.Deg2Rad);
	}

	private void RestoreLodBias()
	{
		if (baseLodBias < 0f) return;
		QualitySettings.lodBias = baseLodBias;
		baseLodBias = -1f;
	}

	void OnDisable()
	{
		RestoreLodBias();
	}
	
	public void SetBaseFOV(float fov)
	{
		baseFOV = fov;
	}
}
