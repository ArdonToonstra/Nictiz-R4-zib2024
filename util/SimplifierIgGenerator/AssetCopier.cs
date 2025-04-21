// AssetCopier.cs
using System;
using System.IO;

public static class AssetCopier
{
    public static void CopyStylesDirectoryIfNotExists(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(sourceDir))
        {
            Logger.Warning($"Source Styles directory not found: {sourceDir}. Skipping copy.");
            return;
        }
        CopyStaticAssetDirectoryIfNotExists(sourceDir, destinationDir, "Styles");
    }

    public static void CopyTemplatePagesDirectoryIfNotExists(string sourceDir, string destinationDir)
    {
         if (!Directory.Exists(sourceDir))
        {
            Logger.Warning($"Source Template Pages directory not found: {sourceDir}. Skipping copy.");
            return;
        }
        CopyStaticAssetDirectoryIfNotExists(sourceDir, destinationDir, "TemplatePages");
    }

    public static void CopyStaticAssetDirectoryIfNotExists(string sourceDir, string destinationDir, string assetTypeName)
    {
        if (Directory.Exists(destinationDir))
        {
            Logger.Warning($"{assetTypeName} directory already exists at '{Path.GetFileName(destinationDir)}'. Skipping copy.");
            return;
        }

        Logger.Info($"Copying {assetTypeName} from '{sourceDir}' to '{destinationDir}'...");
        try
        {
            CopyDirectoryRecursive(sourceDir, destinationDir);
            Logger.Success($"Successfully copied {assetTypeName} directory.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to copy {assetTypeName} directory: {ex.Message}");
            try
            {
                if (Directory.Exists(destinationDir)) Directory.Delete(destinationDir, true);
            }
            catch (Exception cleanupEx)
            {
                Logger.Error($"Failed to cleanup partially copied {assetTypeName} directory '{destinationDir}': {cleanupEx.Message}");
            }
        }
    }

    public static void CopyDirectoryRecursive(string sourceDir, string destinationDir)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}");
        }

        DirectoryInfo[] subdirs = dir.GetDirectories();
        Directory.CreateDirectory(destinationDir);

        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(tempPath, false);
        }

        foreach (DirectoryInfo subdir in subdirs)
        {
            string tempPath = Path.Combine(destinationDir, subdir.Name);
            CopyDirectoryRecursive(subdir.FullName, tempPath);
        }
    }
}