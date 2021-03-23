using UnityEngine;
using System;
using System.Collections.Generic;

public static class Utils
{
    public static void PrintVec2(Vector2 vec, string tag = null, Color _color = default(Color))
    {
        string printStr = "";
        if (!string.IsNullOrEmpty(tag))
        {
            printStr += tag + "  ";
        }

        Color color = (_color == default(Color)) ? Color.white : _color;
        printStr += ("x [" + vec.x + "]  y [" + vec.y + "]").Colored(color);

        Debug.Log(printStr);
    }

    public static void PrintVec3(Vector3 vec, string tag = null, Color _color = default(Color))
    {
        string printStr = "";
        if (!string.IsNullOrEmpty(tag))
        {
            printStr += tag + "  ";
        }

        Color color = (_color == default(Color)) ? Color.white : _color;
        printStr += ("x [" + vec.x + "]  y [" + vec.y + "]  z [" + vec.z + "]").Colored(color);

        Debug.Log(printStr);
    }

    public static void PrintColor(Color col, string tag = null, Color _color = default(Color))
    {
        string printStr = "";
        if (!string.IsNullOrEmpty(tag))
        {
            printStr += tag + "  ";
        }

        Color color = (_color == default(Color)) ? Color.white : _color;
        printStr += ("r [" + col.r + "]  g [" + col.g + "]  b [" + col.b + "] a [" + col.a + "]").Colored(color);

        Debug.Log(printStr);
    }

    public static void PrintQuaternion(Quaternion quat, string tag = null, Color _color = default(Color))
    {
        string printStr = "";
        if (!string.IsNullOrEmpty(tag))
        {
            printStr += tag + "  ";
        }

        Color color = (_color == default(Color)) ? Color.white : _color;
        printStr += ("x [" + quat.x + "]  y [" + quat.y + "]  z [" + quat.z + "] rot [" + quat.w + "]").Colored(color);

        Debug.Log(printStr);
    }

    public static Transform CreateMarker(Transform parent, float scale = 0.5f, Color _color = default(Color))
    {
        GameObject markerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        if (parent != null)
        {
            markerSphere.transform.SetParent(parent);
        }

        markerSphere.transform.localPosition = Vector3.zero;
        markerSphere.transform.localRotation = Quaternion.identity;
        markerSphere.transform.localScale = Vector3.one;
        markerSphere.transform.localScale *= scale;
        markerSphere.name = "marker";
        MonoBehaviour.Destroy(markerSphere.GetComponent<Collider>());

        MeshRenderer _renderer = markerSphere.GetComponent<MeshRenderer>();
        _renderer.material.color = _color;
        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows = false;

        return markerSphere.transform;
    }

    public static void PrintDictionary<T, R>(Dictionary<T, R> dict, string id = "", Color? color = null)
    {
        if (dict.Count == 0)
        {
            return;
        }

        Color logColor = color ?? Color.white;

        if (!string.IsNullOrEmpty(id))
        {
            Debug.Log(("Print Dict for [" + id + "]\n").Colored(logColor));
        }

        foreach (KeyValuePair<T, R> kpv in dict)
        {
            Debug.Log((string.Format("- Key: {0} - Value: {1} ", kpv.Key, kpv.Value)).Colored(logColor));
        }
    }

    public static void PrintArray<T>(T[] arr, string id = "", Color? color = null)
    {
        if (arr.Length == 0)
        {
            return;
        }

        Color logColor = color ?? Color.white;

        if (!string.IsNullOrEmpty(id))
        {
            Debug.Log(("Print Array for [" + id + "]\n").Colored(logColor));
        }

        foreach (T element in arr)
        {
            Debug.Log(element.ToString().Colored(logColor));
        }
    }

    public static bool IsUnitVector(Vector3 vec)
    {
        if (Math.Abs(vec.x) <= 1f || Math.Abs(vec.y) <= 1f || Math.Abs(vec.z) <= 1f)
        {
            return true;
        }

        return false;
    }

    // Checks if the current Vector will overshoot the compare vector when delta is applied
    public static bool VectorWillOvershoot(Vector3 currentVector, Vector3 compareVector, float delta)
    {
        float dist = Vector3.Distance(compareVector, currentVector);

        return delta > dist;
    }

    public static void PrintScreenRatioCoords(Vector2 screenPos, string tag = "", Color _color = default(Color))
    {
        Vector2 pos = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
        Color color = (_color == default(Color)) ? Color.white : _color;
        pos.Print(tag, color);
    }

    public static void DeepCopyArray<T>(T[] origArray, T[] copyArray)
    {
        for (int i = 0; i < origArray.Length; i++)
        {
            copyArray[i] = origArray[i];
        }
    }
}
