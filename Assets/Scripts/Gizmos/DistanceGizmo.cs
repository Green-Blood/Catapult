using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceGizmo : MonoBehaviour
{
    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // Takes a distance in world points and scales the gizmo to cover the distance
    public void StretchGizmo(float distance)
    {
        transform.localScale = new Vector3(distance, 1f, 1f);
    }
}
