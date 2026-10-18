// original by Mr. Animator
// adapted to C# by @torahhorse
// http://wiki.unity3d.com/index.php/Headbobber

using UnityEngine;
using System.Collections;

public class HeadBob : MonoBehaviour
{	
	private float bobbingSpeed = 0.15f; 
	public float bobbingAmount = 0.025f; 
	public float  midpoint = 0.6f; 
	
	private float timer = 0.0f;
	private float eyeHeight;
	private float heightVelocity; 
 
	void LateUpdate ()
	{ 
	    float waveslice = 0.0f; 
	    float horizontal = Input.GetAxis("Horizontal"); 
	    float vertical = Input.GetAxis("Vertical"); 
	    
	    if (Mathf.Abs(horizontal) == 0f && Mathf.Abs(vertical) == 0f)
	    { 
	       timer = 0.0f; 
	    } 
	    else
	    { 
	       waveslice = Mathf.Sin(timer); 
	       timer = timer + bobbingSpeed * Time.deltaTime * 60f; 
	       if (timer > Mathf.PI * 2f)
	       { 
	          timer = timer - (Mathf.PI * 2f); 
	       } 
	    } 
	    if (waveslice != 0f)
	    { 
	       float translateChange = waveslice * bobbingAmount; 
	       float totalAxes = Mathf.Abs(horizontal) + Mathf.Abs(vertical); 
	       totalAxes = Mathf.Clamp (totalAxes, 0.0f, 1.0f); 
	       translateChange = totalAxes * translateChange;
	       
	       Vector3 localPos = transform.localPosition;
	       localPos.y = midpoint + translateChange * Time.timeScale; 
	       transform.localPosition = localPos;
	    } 
	    else
	    { 
	    	Vector3 localPos = transform.localPosition;
	    	localPos.y = midpoint; 
	    	transform.localPosition = localPos;
	    } 

        float targetHeight = transform.position.y;
        if (Mathf.Abs(eyeHeight - targetHeight) > 1f) { eyeHeight = targetHeight; heightVelocity = 0f; }
        eyeHeight = Mathf.SmoothDamp(eyeHeight, targetHeight, ref heightVelocity, .06f, Mathf.Infinity, Time.deltaTime);
        Vector3 correction = transform.parent.InverseTransformVector(Vector3.up * Mathf.Clamp(eyeHeight - targetHeight, -.2f, .2f));
        Vector3 eyePosition = transform.localPosition;
        eyePosition.y += correction.y;
        transform.localPosition = eyePosition;
	}
}
