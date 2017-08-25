using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SizedGameObject : MonoBehaviour {
    [Tooltip("Size if the object (because we do not want to change the scale (and should keep aspect ratio 1)")]
    public Vector3 Size;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
