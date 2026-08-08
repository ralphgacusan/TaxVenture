#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;


public class CleanupSyntyPolygonOfficeTextures
{

    private const string TEXTURE_FOLDER =
        "Assets/Synty/PolygonOffice/Textures";


    private const string MATERIAL_FOLDER =
        "Assets/Synty/PolygonOffice";



    [MenuItem("Tools/Optimization/Cleanup Unused PolygonOffice Textures")]
    public static void Cleanup()
    {

        HashSet<string> usedTextures =
            new HashSet<string>();


        // Find all materials
        string[] materials =
            AssetDatabase.FindAssets(
                "t:Material",
                new[] { MATERIAL_FOLDER }
            );


        foreach (string guid in materials)
        {

            string matPath =
                AssetDatabase.GUIDToAssetPath(guid);


            Material mat =
                AssetDatabase.LoadAssetAtPath<Material>(
                    matPath
                );


            if (mat == null)
                continue;


            string[] dependencies =
                AssetDatabase.GetDependencies(
                    matPath,
                    true
                );


            foreach (string dep in dependencies)
            {

                if (dep.StartsWith(TEXTURE_FOLDER))
                {
                    usedTextures.Add(dep);
                }

            }

        }



        Debug.Log(
            "Used PolygonOffice textures: "
            + usedTextures.Count
        );



        List<string> unused =
            new List<string>();


        string[] allTextures =
            AssetDatabase.FindAssets(
                "t:Texture",
                new[] { TEXTURE_FOLDER }
            );



        foreach (string guid in allTextures)
        {

            string path =
                AssetDatabase.GUIDToAssetPath(guid);


            if (!usedTextures.Contains(path))
            {
                unused.Add(path);
            }

        }



        Debug.Log(
            "Unused PolygonOffice textures found: "
            + unused.Count
        );



        foreach (string texture in unused)
        {

            Debug.Log(
                "SAFE DELETE: "
                + texture
            );

        }



        if (EditorUtility.DisplayDialog(
            "Delete Unused Textures?",
            "Found "
            + unused.Count
            +
            " unused PolygonOffice textures.\n\nDelete them?",
            "Delete",
            "Cancel"))
        {


            foreach (string texture in unused)
            {

                AssetDatabase.DeleteAsset(texture);

            }


            AssetDatabase.Refresh();


            Debug.Log(
                "Deleted unused PolygonOffice textures."
            );

        }

    }

}

#endif