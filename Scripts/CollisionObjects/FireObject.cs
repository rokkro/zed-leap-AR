using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Fires a single collision object forwards from the camera
// No longer used for anything. Instead use CollisionObjectManager.cs
public class FireObject : MonoBehaviour
{
    Camera LeftCamera;
    int force = 5;
    public KeyCode fireKey = KeyCode.Space;
    public int spawnLimit = 10;
    string colliderTag = "portalCollider";
    void Awake()
    {
        ZEDManager manager = GameObject.FindObjectOfType(typeof(ZEDManager)) as ZEDManager;
        LeftCamera = manager.GetLeftCameraTransform().gameObject.GetComponent<Camera>();
    }
    void Update()
    {
        if (Input.GetKeyDown(fireKey) && GameObject.FindGameObjectsWithTag(colliderTag).Length < spawnLimit)
        {
            // Reset vel/rotation
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

            // Move to position/rotation of the camera (the position of you)
            transform.position = LeftCamera.transform.position;
            transform.rotation = LeftCamera.transform.rotation;

            // Move it forwards
            GetComponent<Rigidbody>().AddForce(transform.forward * force);
        }
    }
}
