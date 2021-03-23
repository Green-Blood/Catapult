using UnityEngine;

public static class ExtensionMethods {

    public static string Colored(this string msg, Color color)
    {
        return string.Format("<color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGBA(color), msg);
    }

    public static void CopyTagsAndLayers(this GameObject gameObject, GameObject copyObject)
    {
        gameObject.layer = copyObject.layer;
        gameObject.tag = copyObject.tag;
    }

    public static void CopyTransformValues(this Transform transform, Transform copyTransform)
    {
        transform.position = copyTransform.position;
        transform.rotation = copyTransform.rotation;
        transform.localScale = copyTransform.localScale;
    }

    public static void CopyLocalTransformValues(this Transform transform, Transform copyTransform)
    {
        transform.localPosition = copyTransform.localPosition;
        transform.localRotation = copyTransform.localRotation;
        transform.localScale = copyTransform.localScale;
    }

    public static void Log(this int _val)
    {
        Debug.Log(_val.ToString().Colored(LogColors.white));
    }

    public static void Log(this string _val)
    {
        Debug.Log(_val.Colored(LogColors.white));
    }

    public static void Log(this float _val)
    {
        Debug.Log(_val.ToString().Colored(LogColors.white));
    }

    public static void Print(this Vector2 vec, string tag = "", Color _color = default(Color))
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

    public static void Print(this Vector3 vec, string tag = "", Color _color = default(Color))
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

    public static void Print(this Vector4 vec, string tag = "", Color _color = default(Color))
    {
        string printStr = "";
        if (!string.IsNullOrEmpty(tag))
        {
            printStr += tag + "  ";
        }

        Color color = (_color == default(Color)) ? Color.white : _color;
        printStr += ("x [" + vec.x + "]  y [" + vec.y + "]  z [" + vec.z + "]  w [" + vec.w + "]").Colored(color);

        Debug.Log(printStr);
    }

    public static void Print(this Quaternion quat, string tag = "", Color _color = default(Color))
    {
        string printStr = "";
        if (!string.IsNullOrEmpty(tag))
        {
            printStr += tag + "  ";
        }

        Color color = (_color == default(Color)) ? Color.white : _color;
        printStr += ("x [" + quat.x + "]  y [" + quat.y + "]  z [" + quat.z + "] angle [" + quat.w + "]").Colored(color);

        Debug.Log(printStr);
    }

    public static void Print(this Mesh mesh, bool pVert, bool pUV, bool pNorm, bool pTan, string tag = "", Color _color = default(Color))
    {
        Color color = (_color == default(Color)) ? Color.white : _color;

        if (!string.IsNullOrEmpty(tag))
        {
            Debug.Log(tag.Colored(color));
        }

        if (pVert)
        {
            foreach (Vector3 vertex in mesh.vertices)
            {
                vertex.Print("vert ~ ", color);
            }
            Debug.Log(("Total Verts [" + mesh.vertices.Length +"]").Colored(color));
        }

        if (pUV)
        {
            foreach (Vector2 uv in mesh.uv)
            {
                uv.Print("uv ~ ", color);
            }
            Debug.Log(("Total UVs [" + mesh.uv.Length + "]").Colored(color));
        }

        if (pNorm)
        {
            foreach (Vector3 normal in mesh.normals)
            {
                normal.Print("normal ~ ", color);
            }
            Debug.Log(("Total Normals [" + mesh.normals.Length + "]").Colored(color));
        }

        if (pTan)
        {
            foreach (Vector4 tang in mesh.tangents)
            {
                tang.Print("tangent ~ ", color);
            }
            Debug.Log(("Total Tans [" + mesh.tangents.Length + "]").Colored(color));
        }
    }

    // Checks if the components of the vector fall within a certain tolerance of a Compare Vector
    public static bool IsWithinRange(this Vector2 vec, Vector2 comparVec, float tolerance)
    {
        bool xCheck = (vec.x > comparVec.x - tolerance && vec.x < comparVec.x + tolerance);
        bool yCheck = (vec.y > comparVec.y - tolerance && vec.y < comparVec.y + tolerance);

        return xCheck && yCheck;
    }

    // Checks if the components of the vector fall within a certain tolerance of a Compare Vector
    public static bool IsWithinRange(this Vector3 vec, Vector3 comparVec, float tolerance)
    {
        bool xCheck = (vec.x > comparVec.x - tolerance && vec.x < comparVec.x + tolerance);
        bool yCheck = (vec.y > comparVec.y - tolerance && vec.y < comparVec.y + tolerance);
        bool zCheck = (vec.z > comparVec.z - tolerance && vec.z < comparVec.z + tolerance);

        return xCheck && yCheck && zCheck;
    } 

    // Checks if the components of the vector fall within a certain tolerance of a Compare Vector
    public static bool IsWithinRange(this Vector4 vec, Vector4 comparVec, float tolerance)
    {
        bool xCheck = (vec.x > comparVec.x - tolerance && vec.x < comparVec.x + tolerance);
        bool yCheck = (vec.y > comparVec.y - tolerance && vec.y < comparVec.y + tolerance);
        bool zCheck = (vec.z > comparVec.z - tolerance && vec.z < comparVec.z + tolerance);
        bool wCheck = (vec.w > comparVec.w - tolerance && vec.w < comparVec.w + tolerance);

        return xCheck && yCheck && zCheck && wCheck;
    }

    // Checks if two quaternions are within range, by taking a specific tolerance for the vector and a different tolerance for the angle
    public static bool IsWithinRange(this Quaternion quat, Quaternion compareQuat, float vecTolerance, float angleTolerance)
    {
        bool xCheck = (quat.x > compareQuat.x - vecTolerance && quat.x < compareQuat.x + vecTolerance);
        bool yCheck = (quat.y > compareQuat.y - vecTolerance && quat.y < compareQuat.y + vecTolerance);
        bool zCheck = (quat.z > compareQuat.z - vecTolerance && quat.z < compareQuat.z + vecTolerance);
        bool wCheck = (quat.w > compareQuat.w - angleTolerance && quat.w < compareQuat.w + angleTolerance);

        Debug.Log("x [" + xCheck + "] y [" + yCheck + "] z [" + zCheck + "]  w [" + wCheck + "]");
        return xCheck && yCheck && zCheck && wCheck;
    }

    // Checks if two Colors are within range, by taking a specific tolerance for the vector and a different tolerance for the angle
    public static bool IsWithinRange(this Color col, Color compareCol, float tolerance)
    {
        bool rCheck = (col.r > compareCol.r - tolerance && col.r < compareCol.r + tolerance);
        bool gCheck = (col.g > compareCol.g - tolerance && col.g < compareCol.g + tolerance);
        bool bCheck = (col.b > compareCol.b - tolerance && col.b < compareCol.b + tolerance);
        bool aCheck = (col.a > compareCol.a - tolerance && col.a < compareCol.a + tolerance);

        return rCheck && gCheck && bCheck && aCheck;
    }
}
