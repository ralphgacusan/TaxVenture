#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;


public class OptimizeSyntyTextures
{

    [MenuItem("Tools/Optimization/Resize Synty PolygonOffice Textures")]
    public static void ResizeTextures()
    {

        string folder =
            "Assets/Synty/PolygonOffice/Textures";


        string[] textures =
            AssetDatabase.FindAssets(
                "t:Texture",
                new[] { folder }
            );


        int changed = 0;


        foreach (string guid in textures)
        {

            string path =
                AssetDatabase.GUIDToAssetPath(guid);


            TextureImporter importer =
                AssetImporter.GetAtPath(path)
                as TextureImporter;


            if (importer == null)
                continue;


            if (importer.maxTextureSize > 1024)
            {

                importer.maxTextureSize = 1024;


                importer.SaveAndReimport();


                changed++;

                Debug.Log(
                    "Optimized: "
                    + path
                );

            }

        }


        Debug.Log(
            "Finished. Changed "
            + changed
            + " textures."
        );

    }

}

#endif