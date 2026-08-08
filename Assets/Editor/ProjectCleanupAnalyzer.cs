#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;


public class ProjectCleanupAnalyzer
{

    private class AssetInfo
    {
        public string path;
        public long size;
    }


    private static StringBuilder report = new StringBuilder();


    [MenuItem("Tools/Project Cleanup Analyzer/Run Full Scan")]
    public static void RunScan()
    {
        report.Clear();

        report.AppendLine("=================================");
        report.AppendLine(" UNITY PROJECT CLEANUP REPORT ");
        report.AppendLine("=================================\n");


        ScanAssets();

        ScanFileTypes();

        ScanTextures();

        FindUnusedAssets();

        ListPackages();


        string savePath =
            Application.dataPath +
            "/../ProjectCleanupReport.txt";


        File.WriteAllText(
            savePath,
            report.ToString()
        );


        Debug.Log(
            "Scan Complete!\nReport saved:\n"
            + savePath
        );


        EditorUtility.RevealInFinder(savePath);
    }



    // ===============================
    // ASSET SIZE SCAN
    // ===============================

    private static List<AssetInfo> assets =
        new List<AssetInfo>();


    private static void ScanAssets()
    {

        report.AppendLine(
            "\n========== TOP 30 LARGEST FILES ==========\n"
        );


        string root =
            Application.dataPath;


        assets.Clear();


        foreach (string file in Directory.GetFiles(
            root,
            "*.*",
            SearchOption.AllDirectories))
        {

            if (file.EndsWith(".meta"))
                continue;


            if (file.Contains("/Editor/"))
                continue;


            FileInfo info =
                new FileInfo(file);


            assets.Add(new AssetInfo
            {
                path =
                file.Replace(
                    root,
                    "Assets"),

                size =
                info.Length
            });

        }



        foreach (var asset in assets
            .OrderByDescending(x => x.size)
            .Take(30))
        {

            report.AppendLine(
                $"{FormatBytes(asset.size)}   {asset.path}"
            );

        }

    }



    // ===============================
    // FILE TYPES
    // ===============================

    private static void ScanFileTypes()
    {

        Dictionary<string, long> sizes =
            new();


        foreach (var asset in assets)
        {

            string ext =
                Path.GetExtension(asset.path)
                .ToLower();


            if (!sizes.ContainsKey(ext))
                sizes[ext] = 0;


            sizes[ext] += asset.size;

        }



        report.AppendLine(
            "\n========== FILE TYPE TOTALS ==========\n"
        );


        foreach (var item in sizes
            .OrderByDescending(x => x.Value))
        {

            report.AppendLine(
                $"{item.Key} : {FormatBytes(item.Value)}"
            );

        }


    }



    // ===============================
    // TEXTURE REPORT
    // ===============================

    private static void ScanTextures()
    {

        int tex4k = 0;
        int tex2k = 0;
        int tex1k = 0;


        report.AppendLine(
            "\n========== TEXTURE REPORT ==========\n"
        );


        string[] textures =
            AssetDatabase.FindAssets(
                "t:Texture");


        foreach (string guid in textures)
        {

            string path =
                AssetDatabase.GUIDToAssetPath(guid);


            TextureImporter importer =
                AssetImporter.GetAtPath(path)
                as TextureImporter;


            if (importer == null)
                continue;


            int size =
                importer.maxTextureSize;


            if (size >= 4096)
                tex4k++;

            else if (size >= 2048)
                tex2k++;

            else if (size >= 1024)
                tex1k++;

        }



        report.AppendLine(
            $"4K Textures : {tex4k}"
        );

        report.AppendLine(
            $"2K Textures : {tex2k}"
        );

        report.AppendLine(
            $"1K Textures : {tex1k}"
        );


    }



    // ===============================
    // UNUSED ASSET CHECK
    // ===============================

    private static void FindUnusedAssets()
    {

        report.AppendLine(
            "\n========== POSSIBLE UNUSED ASSETS ==========\n"
        );


        foreach (var asset in assets)
        {

            if (
                asset.path.EndsWith(".cs")
                ||
                asset.path.EndsWith(".unity")
            )
                continue;



            string[] dependencies =
                AssetDatabase.GetDependencies(
                    new string[]
                    {
                        asset.path
                    },
                    true);



            if (dependencies.Length <= 1)
            {

                report.AppendLine(
                    asset.path
                );

            }

        }


    }



    // ===============================
    // PACKAGE LIST
    // ===============================

    private static void ListPackages()
    {

        report.AppendLine(
            "\n========== UNITY PACKAGES ==========\n"
        );


        string manifest =
            "Packages/manifest.json";


        if (File.Exists(manifest))
        {

            string json =
                File.ReadAllText(manifest);


            report.AppendLine(json);

        }


    }



    // ===============================
    // FORMAT SIZE
    // ===============================

    private static string FormatBytes(long bytes)
    {

        string[] sizes =
        {
            "B",
            "KB",
            "MB",
            "GB"
        };


        double len = bytes;

        int order = 0;


        while (
            len >= 1024
            &&
            order < sizes.Length - 1)
        {
            order++;

            len /= 1024;
        }


        return
            $"{len:0.##} {sizes[order]}";

    }

}

#endif