using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class SceneTemplate_RotateCube : MonoBehaviour
{
    [Tooltip("Changes the rotation speed of the cube")]
    public float rotateSpeed = 1f;

    [Tooltip("Changes orientation of the cube")]
    public Vector3 objectRotation;

    
    
    void Update()
    {
        
        transform.Rotate(objectRotation * Time.deltaTime * rotateSpeed);
    }
}
