#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ModdingUtilitys : MonoBehaviour
{
    [MenuItem("VTOLVR Modding/Build Asset Bundles")]
    public static void BuildAllAssetBundles()
    {
        string assetBundleDirectory = "Assets/_ModLoader/Exported Asset Bundle";
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }
        try
        {
            BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.StrictMode, BuildTarget.StandaloneWindows);
            Debug.Log("<b>SUCCESSFULLY BUILDED ASSETBUNDLES ✔️</b>");
        }
        catch (Exception e)
        {
            Debug.LogError("AN ERROR OCCURED WHILE BUILDING THE ASSETBUNDLES!\n" + e.ToString());
        }
    }
    [MenuItem("VTOLVR Modding/Fix Materials")]
    public static void FixMaterials()
    {
        Material[] mats = Resources.FindObjectsOfTypeAll(typeof(Material)) as Material[];
        int nullCount = 0;
        Shader standardShader = Shader.Find("Standard");
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i].shader.name == string.Empty)
            {
                mats[i].shader = standardShader;
                nullCount++;
            }
        }
        Debug.Log($"Fixed {nullCount} shaders");
    }
}
#endif