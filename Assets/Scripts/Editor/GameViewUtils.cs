﻿
/**THIS CLASS IS FOR THE EDITOR ONLY, NOT SUPPORTED ON OTHER DEVICES**/

#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;


/**SN: Game View Util Class, that allows us to Add/Check/Remove Aspect ratios and screen sizes
/**http://answers.unity3d.com/questions/956123/add-and-select-game-view-resolution.html**/
public static class GameViewUtils
{
    static object gameViewSizesInstance;
    static MethodInfo getGroup;
    static GameViewSizeGroupType groupType = GameViewSizeGroupType.Standalone;

    static GameViewUtils()
    {
        groupType = (GameViewSizeGroupType)Enum.Parse(typeof(GameViewSizeGroupType), EditorUserBuildSettings.selectedBuildTargetGroup.ToString(), true);

        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instanceProp = singleType.GetProperty("instance");
        getGroup = sizesType.GetMethod("GetGroup");
        gameViewSizesInstance = instanceProp.GetValue(null, null);
    }

    public enum GameViewSizeType
    {
        AspectRatio, FixedResolution
    }

    [MenuItem("Test/AddSize")]
    public static void AddTestSize()
    {
        AddCustomSize(GameViewSizeType.AspectRatio, 123, 456, "Test size");
    }

    [MenuItem("Test/SizeTextQuery")]
    public static void SizeTextQueryTest()
    {
        Debug.Log(SizeExists("Test size"));
    }

    [MenuItem("Test/Query16:9Test")]
    public static void WidescreenQueryTest()
    {
        Debug.Log(SizeExists("16:9"));
    }

    [MenuItem("Test/Set16:9")]
    public static void SetWidescreenTest()
    {
        SetSize(FindSize("16:9"));
    }

    [MenuItem("Test/SetTestSize")]
    public static void SetTestSize()
    {
        int idx = FindSize(groupType, 123, 456);
        if (idx != -1)
            SetSize(idx);
    }

    public static void SetSize(int index)
    {
        var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var selectedSizeIndexProp = gvWndType.GetProperty("selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var gvWnd = EditorWindow.GetWindow(gvWndType);
        selectedSizeIndexProp.SetValue(gvWnd, index, null);
    }

    [MenuItem("Test/SizeDimensionsQuery")]
    public static void SizeDimensionsQueryTest()
    {
        Debug.Log(SizeExists(groupType, 123, 456));
    }

    public static void AddCustomSize(GameViewSizeType viewSizeType, int width, int height, string text)
    {
        // GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupTyge);
        // group.AddCustomSize(new GameViewSize(viewSizeType, width, height, text);

        var group = GetGroup(groupType);
        var addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize"); // or group.GetType().
        var gvsType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
        var ctor = gvsType.GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(string) });
        var newSize = ctor.Invoke(new object[] { (int)viewSizeType, width, height, text });
        addCustomSize.Invoke(group, new object[] { newSize });
    }

    public static bool SizeExists(string text)
    {
        return FindSize(text) != -1;
    }

    public static int FindSize(string text)
    {
        // GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupType);
        // string[] texts = group.GetDisplayTexts();
        // for loop...

        var group = GetGroup(groupType);
        var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts");
        var displayTexts = getDisplayTexts.Invoke(group, null) as string[];
        for (int i = 0; i < displayTexts.Length; i++)
        {
            string display = displayTexts[i];
            // the text we get is "Name (W:H)" if the size has a name, or just "W:H" e.g. 16:9
            // so if we're querying a custom size text we substring to only get the name
            // You could see the outputs by just logging
            // Debug.Log(display);
            int pren = display.IndexOf('(');
            if (pren != -1)
                display = display.Substring(0, pren - 1); // -1 to remove the space that's before the prens. This is very implementation-depdenent
            if (display == text)
                return i;
        }
        return -1;
    }


    //Allows switching to a size, if the size is not in the list, add and switch
    public static void SwitchToSize(Vector2 compareSize, GameViewUtils.GameViewSizeType resoType, string name)
    {
        int displayIndex = FindSize(compareSize);

        if (displayIndex < 0)
        {
			GameViewUtils.AddCustomSize(resoType, (int)compareSize.x, (int)compareSize.y, name);
            int index = FindSize(compareSize);
            GameViewUtils.SetSize(index);
		}
        else
        {
            GameViewUtils.SetSize(displayIndex);
        }
    }

    public static int FindSize(Vector2 compareSize)
    {
        // GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupType);
        // string[] texts = group.GetDisplayTexts();
        // for loop...

        var group = GetGroup(groupType);
        var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts");
        var displayTexts = getDisplayTexts.Invoke(group, null) as string[];
        string sizeAsString1 = "(" + compareSize.x + "x" + compareSize.y + ")";
        string sizeAsString2 = "(" + compareSize.x + ":" + compareSize.y + ")";

        for (int i = 0; i < displayTexts.Length; i++)
        {
            string display = displayTexts[i];
            // the text we get is "Name (W:H)" if the size has a name, or just "W:H" e.g. 16:9
            // so if we're querying a custom size text we substring to only get the name
            // You could see the outputs by just logging
            // Debug.Log(display);
            int pren = display.IndexOf('(');

          

            if (pren != -1)
            {
               // Debug.Log("text: " + display);
                display = display.Substring(pren); //grab just the size portion ie: 16:9 , 1024:768 etc...

               // Debug.Log("size: " + display + " compared with: " + sizeAsString);
                if (display.Equals(sizeAsString1) || display.Equals(sizeAsString2))
                    return i;
            }
           

        }
        return -1;
    }

    public static bool SizeExists(GameViewSizeGroupType sizeGroupType, int width, int height)
    {
        return FindSize(sizeGroupType, width, height) != -1;
    }

    public static int FindSize(GameViewSizeGroupType sizeGroupType, int width, int height)
    {
        // goal:
        // GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupType);
        // int sizesCount = group.GetBuiltinCount() + group.GetCustomCount();
        // iterate through the sizes via group.GetGameViewSize(int index)

        var group = GetGroup(sizeGroupType);
        var groupType = group.GetType();
        var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
        var getCustomCount = groupType.GetMethod("GetCustomCount");
        int sizesCount = (int)getBuiltinCount.Invoke(group, null) + (int)getCustomCount.Invoke(group, null);
        var getGameViewSize = groupType.GetMethod("GetGameViewSize");
        var gvsType = getGameViewSize.ReturnType;
        var widthProp = gvsType.GetProperty("width");
        var heightProp = gvsType.GetProperty("height");
        var indexValue = new object[1];
        for (int i = 0; i < sizesCount; i++)
        {
            indexValue[0] = i;
            var size = getGameViewSize.Invoke(group, indexValue);
            int sizeWidth = (int)widthProp.GetValue(size, null);
            int sizeHeight = (int)heightProp.GetValue(size, null);
            if (sizeWidth == width && sizeHeight == height)
                return i;
        }
        return -1;
    }

    static object GetGroup(GameViewSizeGroupType type)
    {
        return getGroup.Invoke(gameViewSizesInstance, new object[] { (int)type });
    }

    [MenuItem("Test/LogCurrentGroupType")]
    public static void LogCurrentGroupType()
    {
        Debug.Log(GetCurrentGroupType());
    }
    public static GameViewSizeGroupType GetCurrentGroupType()
    {
        var getCurrentGroupTypeProp = gameViewSizesInstance.GetType().GetProperty("currentGroupType");
        return (GameViewSizeGroupType)(int)getCurrentGroupTypeProp.GetValue(gameViewSizesInstance, null);
    }
}

#endif