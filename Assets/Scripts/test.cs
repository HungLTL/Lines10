using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("Local position: " + transform.localPosition);
        Debug.Log("Global position: " + transform.TransformPoint(Vector3.zero));
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            transform.parent = null;
            Debug.Log("Local position: " + transform.localPosition);
            Debug.Log("Global position: " + transform.TransformPoint(Vector3.zero));
        }
    }
}
