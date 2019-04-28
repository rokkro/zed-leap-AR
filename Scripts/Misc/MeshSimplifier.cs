using System.Collections.Generic;
using UnityEngine;
using System;

// Runs the mesh simplifier on planes at a set interval in an attempt to reduce complexity.
// Intended to help performance a little if there are a lot of planes in the scene.
// Not all meshes will be simplified. The mesh simplifier script used will fail to simplify some.
// The mesh simplification script MeshColliderTools.cs was not by me.
public class MeshSimplifier : MonoBehaviour
{
    float requiredTime = 3f;
    float currentTime = 0f;

    //list of gameObjects IDs that cant be simplified.
    List<int> dontSimplify = new List<int>();

    // The PlaneDetectionManager GameObject should have a child object containing all detected planes
    // Go through every plane (that we havent already tried to simplify), grab its mesh, and try to simplify it.
    void simplify()
    {
        int numberOfPlanes = transform.GetChild(0).childCount;

        for (int planeIndex = 0; planeIndex < numberOfPlanes; planeIndex++)
        {
            GameObject plane = transform.GetChild(0).GetChild(planeIndex).gameObject;

            if (dontSimplify.Contains(plane.GetInstanceID()))
                continue;

            MeshCollider mc = plane.GetComponent<MeshCollider>();
            try
            {
                // Try to simplify the mesh
                MeshColliderTools.Simplify(mc.sharedMesh);
            }
            catch (Exception) { }

            // Dont need to simplify planes we've already simplified
            dontSimplify.Add(plane.GetInstanceID());
        }
    }

    void LateUpdate()
    {
        // Don't want it to simplify every frame
        currentTime += Time.deltaTime;
        if (currentTime > requiredTime)
        {
            simplify();
            currentTime = 0f;
        }
    }
}
