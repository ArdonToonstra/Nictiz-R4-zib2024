// OutputGenerator.cs
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text; // Required for StringBuilder

public static class OutputGenerator
{
        private class CategorizedFiles
    {
        public List<string> ConformanceFiles { get; } = new List<string>();
        public List<string> TerminologyFiles { get; } = new List<string>();
        public List<string> ExampleFiles { get; } = new List<string>();
    }
    public static void GenerateOutput(AppSettings settings, List<Resource> resources)
    {
        Logger.Info("\nStarting output generation...");

        if (string.IsNullOrEmpty(settings.OutputDirectory) || string.IsNullOrEmpty(settings.IgName))
        {
            Logger.Error("Error: OutputDirectory or IgName is missing in settings. Cannot generate output.");
            return;
        }

        // --- Define base and subfolder paths ---
        string baseIgOutputPath = Path.Combine(settings.OutputDirectory, settings.IgName);
        string homeDirectoryPath = Path.Combine(baseIgOutputPath, "Home");
        string resourcesBasePath = Path.Combine(baseIgOutputPath, "Home", "Resources");
        string conformancePath = Path.Combine(resourcesBasePath, "Conformance");
        string terminologyPath = Path.Combine(resourcesBasePath, "Terminology");
        string examplesPath = Path.Combine(resourcesBasePath, "Examples");
        string guideYamlPath = Path.Combine(baseIgOutputPath, "guide.yaml");
        string destStylesPath = Path.Combine(baseIgOutputPath, "styles");
        string indexPagePath = Path.Combine(baseIgOutputPath, "Home", "Index.page.md"); 
        string rootTocPath = Path.Combine(baseIgOutputPath, "toc.yaml");
        string destTemplatePagesPath = Path.Combine(homeDirectoryPath, "TemplatePages");
     
        try
        {
            Directory.CreateDirectory(conformancePath); 
            Directory.CreateDirectory(terminologyPath); 
            Directory.CreateDirectory(examplesPath);  
            Logger.Info($"Ensured output structure exists at: {resourcesBasePath}");

            // --- Copy Styles and Template page directories (if configured and source exists) ---
            if (!string.IsNullOrEmpty(settings.SourceStylesDirectory) && Directory.Exists(settings.SourceStylesDirectory))
            {
                 CopyStylesDirectoryIfNotExists(settings.SourceStylesDirectory, destStylesPath);
            }
            if (!string.IsNullOrEmpty(settings.SourceTemplatePagesDirectory) && Directory.Exists(settings.SourceTemplatePagesDirectory))
            {
                 CopyStaticAssetDirectoryIfNotExists(settings.SourceTemplatePagesDirectory, destTemplatePagesPath, "TemplatePages"); // Pass asset type name
            }
            // --- End Copy ---

            GenerateGuideYaml(settings, guideYamlPath);
            CategorizedFiles categorizedFileNames = GenerateResourceFiles(settings, resources, conformancePath, terminologyPath, examplesPath);
            GenerateIndexPage(indexPagePath, categorizedFileNames);
            GenerateStaticRootToc(rootTocPath);
            GenerateTocFiles(homeDirectoryPath);

        }
        catch (Exception ex)
        {
            Logger.Error($"Error creating or accessing output directory structure {baseIgOutputPath}: {ex.Message}");
        }
    }

    private static void CopyStylesDirectoryIfNotExists(string sourceDir, string destinationDir)
    {
        if (Directory.Exists(destinationDir))
        {
            Logger.Warning($"Styles directory already exists at {destinationDir}. Skipping copy.");
            return;
        }

        Logger.Info($"Copying styles from {sourceDir} to {destinationDir}...");
        try
        {
            CopyDirectoryRecursive(sourceDir, destinationDir);
            Logger.Success($"Successfully copied styles directory.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to copy styles directory: {ex.Message}");
            try
            {
                 if(Directory.Exists(destinationDir)) Directory.Delete(destinationDir, true);
            } catch (Exception cleanupEx) {
                 Logger.Error($"Failed to cleanup partially copied styles directory {destinationDir}: {cleanupEx.Message}");
            }
        }
    }

    // --- Updated Helper Method name and logging for generic usage ---
    private static void CopyStaticAssetDirectoryIfNotExists(string sourceDir, string destinationDir, string assetTypeName)
    {
        if (Directory.Exists(destinationDir))
        {
            Logger.Warning($"{assetTypeName} directory already exists at {Path.GetFileName(destinationDir)}. Skipping copy."); // Use Path.GetFileName for clarity
            return;
        }

        Logger.Info($"Copying {assetTypeName} from {sourceDir} to {destinationDir}...");
        try
        {
            CopyDirectoryRecursive(sourceDir, destinationDir); // Recursive copy logic remains the same
            Logger.Success($"Successfully copied {assetTypeName} directory.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to copy {assetTypeName} directory: {ex.Message}");
            // Optional: Attempt to clean up partially copied directory
            try
            {
                 if(Directory.Exists(destinationDir)) Directory.Delete(destinationDir, true);
            } catch (Exception cleanupEx) {
                 Logger.Error($"Failed to cleanup partially copied {assetTypeName} directory {destinationDir}: {cleanupEx.Message}");
            }
        }
    }

    // --- New Recursive Copy Logic ---
    private static void CopyDirectoryRecursive(string sourceDir, string destinationDir)
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}");
        }

        // If the destination directory doesn't exist, create it.
        if (!Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
            // Logger.Info($" Created directory: {destinationDir}"); // Optional verbose logging
        }

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(tempPath, false); // false = do not overwrite if exists (though parent check prevents this)
            // Logger.Info($"  Copied file: {file.Name}"); // Optional verbose logging
        }

        // If copying subdirectories, copy them and their contents to new location.
        DirectoryInfo[] subdirs = dir.GetDirectories();
        foreach (DirectoryInfo subdir in subdirs)
        {
            string tempPath = Path.Combine(destinationDir, subdir.Name);
            CopyDirectoryRecursive(subdir.FullName, tempPath);
        }
    }

    private static void GenerateGuideYaml(AppSettings settings, string guideYamlPath)
    {
        try
        {
            var yamlContent = new StringBuilder();
            yamlContent.AppendLine($"title: {settings.IgName}");
            yamlContent.AppendLine($"description: {settings.IgDescription}");
            yamlContent.AppendLine($"version: {settings.IgVersion}");
            yamlContent.AppendLine("style-root: styles");
            yamlContent.AppendLine("style-name: custom");
            yamlContent.AppendLine("numbered-headings: false");

            File.WriteAllText(guideYamlPath, yamlContent.ToString());
            Logger.Success($"  Successfully wrote: guide.yaml");
        }
        catch (Exception ex)
        {
            Logger.Error($"    Error writing guide.yaml: {ex.Message}");
        }
    }

    // --- Method signature updated to RETURN categorized filenames ---
    private static CategorizedFiles GenerateResourceFiles(AppSettings settings, List<Resource> resources,
                                              string conformancePath, string terminologyPath, string examplesPath)
    {
        Logger.Info($"\nGenerating resource files into categorized subfolders...");
        int filesWritten = 0;
        CategorizedFiles categorizedFiles = new CategorizedFiles(); 

        foreach (var resource in resources)
        {
            string resourceId = resource.Id ?? $"generated-{Guid.NewGuid()}";
            string baseFileName = $"{resource.TypeName}-{resourceId}";
            string markdownFileName = $"{baseFileName}.page.md";

            string targetDirectory;
            List<string> targetList; 
            string fileCategory;

            switch (resource.TypeName)
            {
                case "StructureDefinition":
                case "SearchParameter":
                case "OperationDefinition":
                case "CapabilityStatement":
                case "ImplementationGuide":
                    targetDirectory = conformancePath;
                    targetList = categorizedFiles.ConformanceFiles;
                    fileCategory = "Conformance"; 
                    break;

                case "CodeSystem":
                case "ValueSet":
                case "ConceptMap":
                case "NamingSystem":
                    targetDirectory = terminologyPath;
                    targetList = categorizedFiles.TerminologyFiles;
                    fileCategory = "Terminology"; 
                    break;

                default: // Assume others are Examples for directory placement
                    targetDirectory = examplesPath;
                    targetList = categorizedFiles.ExampleFiles;
                    fileCategory = "Example"; 
                    break;
            }
            string fullPath = Path.Combine(targetDirectory, markdownFileName);
            targetList.Add(baseFileName);

            try
            {
                if (resource.Id == null) { Logger.Warning($"..."); }

                string markdownContentString = MarkdownGenerator.GenerateMarkdown(resource, baseFileName, fileCategory);

                File.WriteAllText(fullPath, markdownContentString);
                Logger.Cyan($"  Successfully wrote: {Path.Combine(Path.GetFileName(targetDirectory), markdownFileName)}");
                filesWritten++;
            }
            catch (IOException) { Logger.Error($"..."); }
            catch (Exception) { Logger.Error($"..."); }
        }
        Logger.Info($"\nOutput generation complete. Wrote {filesWritten} resource files.");
        return categorizedFiles;
    }
    private static void GenerateIndexPage(string indexPagePath, CategorizedFiles categorizedFiles)
    {
        Logger.Info($"Generating index page: {indexPagePath}");
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("---");
            sb.AppendLine("topic: index"); // Example topic, adjust if needed
            sb.AppendLine("layout: landing"); // Example layout, adjust if needed
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("# Implementation Guide Index");
            sb.AppendLine();

            // Add tables for each category
            AppendResourceTable(sb, "Conformance Resources", categorizedFiles.ConformanceFiles, "Conformance");
            AppendResourceTable(sb, "Terminology Resources", categorizedFiles.TerminologyFiles, "Terminology");
            AppendResourceTable(sb, "Example Resources", categorizedFiles.ExampleFiles, "Examples");

            File.WriteAllText(indexPagePath, sb.ToString());
            Logger.Success($"Successfully wrote: {Path.GetFileName(indexPagePath)}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to generate index page: {ex.Message}");
        }
    }

    private static void AppendResourceTable(StringBuilder sb, string title, List<string> fileList, string subfolder)
    {
        if (!fileList.Any()) return;

        sb.AppendLine($"## {title}");
        sb.AppendLine();
        sb.AppendLine("|         |         |         |         |");
        sb.AppendLine("| :------ | :------ | :------ | :------ |");

        int columns = 4;
        for (int i = 0; i < fileList.Count; i++)
        {
            if (i % columns == 0)
            {
                 sb.Append("| ");
            }

            sb.Append($"{{{{pagelink:{fileList[i]}}}}} | ");

            if ((i + 1) % columns == 0 || i == fileList.Count - 1)
            {
                if ((i + 1) % columns != 0 && i == fileList.Count - 1)
                {
                    int remainingCells = columns - ((i + 1) % columns);
                    for (int j = 0; j < remainingCells; j++)
                    {
                        sb.Append("         | ");
                    }
                }
                sb.AppendLine();
            }
        }
        sb.AppendLine();
    }


    private static void GenerateStaticRootToc(string rootTocPath)
    {
        Logger.Info("Generating static root toc.yaml...");
        try
        {
            // Simple static content
            string[] tocContent = { "- name: Home", "  filename: Home" };
            File.WriteAllLines(rootTocPath, tocContent);
            Logger.Success($"Successfully wrote: {Path.GetFileName(rootTocPath)}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to generate static root toc.yaml: {ex.Message}");
        }
    }

    private static void GenerateTocFiles(string homeDirectoryPath)
    {
        if (!Directory.Exists(homeDirectoryPath))
        {
            Logger.Warning($"Home directory not found at {homeDirectoryPath}. Skipping TOC generation.");
            return;
        }
        Logger.Info("\nGenerating toc.yaml files...");
        GenerateTocForDirectory(homeDirectoryPath); // Start recursion from Home
    }

 private static void GenerateTocForDirectory(string directoryPath)
    {
        try
        {
            var tocEntries = new List<string>();

            // Get subdirectories, **FILTERING OUT TemplatePages**
            var subDirectories = Directory.GetDirectories(directoryPath)
                                          .Where(d => !Path.GetFileName(d).Equals("TemplatePages", StringComparison.OrdinalIgnoreCase)) // Exclude TemplatePages
                                          .OrderBy(d => d)
                                          .ToList();

            foreach (var subDir in subDirectories)
            {
                string folderName = Path.GetFileName(subDir);
                tocEntries.Add($"- name: {folderName}");
                tocEntries.Add($"  filename: {folderName}");
            }

            // Get markdown files (logic remains the same)
            var files = Directory.GetFiles(directoryPath, "*.md")
                               .OrderBy(f => f)
                               .ToList();
            foreach (var file in files)
            {
                // ... (file processing logic from previous step remains the same) ...
                string originalFileName = Path.GetFileName(file);
                string nameForDisplay = Path.GetFileNameWithoutExtension(originalFileName);
                if (nameForDisplay.EndsWith(".page", StringComparison.OrdinalIgnoreCase)) {
                    nameForDisplay = Path.GetFileNameWithoutExtension(nameForDisplay);
                }
                string filenameForLink;
                if (originalFileName.EndsWith(".page.md", StringComparison.OrdinalIgnoreCase)) {
                    filenameForLink = originalFileName;
                } else {
                    string baseName = Path.GetFileNameWithoutExtension(originalFileName);
                    filenameForLink = baseName + ".page.md";
                }
                tocEntries.Add($"- name: {nameForDisplay}");
                tocEntries.Add($"  filename: {filenameForLink}");
            }

            // Write toc.yaml if entries exist (logic remains the same)
            if (tocEntries.Any())
            {
                // ... (write file logic) ...
                string tocFilePath = Path.Combine(directoryPath, "toc.yaml");
                 try { File.WriteAllLines(tocFilePath, tocEntries); Logger.Cyan($"  Successfully wrote: {Path.Combine(Path.GetFileName(directoryPath), "toc.yaml")}"); }
                 catch (IOException ioEx) { Logger.Error($"    Error writing toc.yaml in {Path.GetFileName(directoryPath)}: {ioEx.Message}"); }
            }

            // Recurse into subdirectories (now uses the filtered list)
            foreach (var subDir in subDirectories) // <- uses the filtered list
            {
                GenerateTocForDirectory(subDir);
            }
        }
        catch (Exception ex)
        {
             Logger.Error($"Error processing directory for TOC {directoryPath}: {ex.Message}");
        }
    }
}