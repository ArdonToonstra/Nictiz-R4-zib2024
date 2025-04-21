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
        string indexPagePath = Path.Combine(baseIgOutputPath, "Home", "Index.page.md"); // Path for the new index page
     

        try
        {
            Directory.CreateDirectory(conformancePath); 
            Directory.CreateDirectory(terminologyPath); 
            Directory.CreateDirectory(examplesPath);  
            Logger.Info($"Ensured output structure exists at: {resourcesBasePath}");

            // --- Copy Styles Directory (if configured and source exists) ---
            if (!string.IsNullOrEmpty(settings.SourceStylesDirectory) && Directory.Exists(settings.SourceStylesDirectory))
            {
                 CopyStylesDirectoryIfNotExists(settings.SourceStylesDirectory, destStylesPath);
            }
            // --- End Copy Styles ---

            GenerateGuideYaml(settings, guideYamlPath);
            CategorizedFiles categorizedFileNames = GenerateResourceFiles(settings, resources, conformancePath, terminologyPath, examplesPath);
            GenerateIndexPage(indexPagePath, categorizedFileNames);
            GenerateTocFiles(homeDirectoryPath);

        }
        catch (Exception ex)
        {
            Logger.Error($"Error creating or accessing output directory structure {baseIgOutputPath}: {ex.Message}");
        }
    }

// --- New Helper Method to Copy Styles ---
    private static void CopyStylesDirectoryIfNotExists(string sourceDir, string destinationDir)
    {
        if (Directory.Exists(destinationDir))
        {
            Logger.Info($"Styles directory already exists at {destinationDir}. Skipping copy.");
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
            yamlContent.AppendLine("style-name: ballotable");
            yamlContent.AppendLine("numbered-headings: false");

            File.WriteAllText(guideYamlPath, yamlContent.ToString());
            Logger.Magenta($"  Successfully wrote: guide.yaml");
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
            string markdownFileName = $"{baseFileName}.md";

            string targetDirectory;
            List<string> targetList; 

            switch (resource.TypeName)
            {
                case "StructureDefinition":
                case "SearchParameter":
                case "OperationDefinition":
                case "CapabilityStatement":
                case "ImplementationGuide":
                    targetDirectory = conformancePath;
                    targetList = categorizedFiles.ConformanceFiles;
                    break;

                case "CodeSystem":
                case "ValueSet":
                case "ConceptMap":
                case "NamingSystem":
                    targetDirectory = terminologyPath;
                    targetList = categorizedFiles.TerminologyFiles; 
                    break;

                default:
                    targetDirectory = examplesPath;
                    targetList = categorizedFiles.ExampleFiles;
                    break;
            }
            string fullPath = Path.Combine(targetDirectory, markdownFileName);
            targetList.Add(baseFileName); 

            try
            {
                 if (resource.Id == null) { Logger.Warning($"Resource type {resource.TypeName} found with no ID. Assigning temporary filename: {markdownFileName}"); }

                 string markdownContentString = MarkdownGenerator.GenerateMarkdown(resource, baseFileName);
                
                 File.WriteAllText(fullPath, markdownContentString); // Use the generated string
                 Logger.Cyan($"  Successfully wrote: {Path.Combine(Path.GetFileName(targetDirectory), markdownFileName)}");
                 filesWritten++;
            }
            catch (IOException ioEx) { Logger.Error($"    Error writing file {markdownFileName} to {Path.GetFileName(targetDirectory)}: {ioEx.Message}"); }
            catch (Exception ex) { Logger.Error($"    Unexpected error generating file for {resource.TypeName} (ID: {resource.Id ?? "none"}): {ex.Message}"); }
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
        sb.AppendLine("|         |         |         |         |         |");
        sb.AppendLine("| :------ | :------ | :------ | :------ | :------ |");

        int columns = 5;
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
            var tocEntries = new List<string>(); // Using List<string> for simple YAML lines

            var subDirectories = Directory.GetDirectories(directoryPath)
                                          .OrderBy(d => d) // Sort alphabetically
                                          .ToList();

            foreach (var subDir in subDirectories)
            {
                string folderName = Path.GetFileName(subDir);
                // YAML entry for a folder
                tocEntries.Add($"- name: {folderName}");
                tocEntries.Add($"  filename: {folderName}"); // No suffix for folders
            }

            // Get markdown files in the current directory
            var files = Directory.GetFiles(directoryPath, "*.md")
                               .OrderBy(f => f) // Sort alphabetically
                               .ToList();

            foreach (var file in files)
            {
                string fileNameWithExtension = Path.GetFileName(file);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileNameWithExtension);
                // We need the base name to construct the .page.md suffix filename
                string pageFileName = Path.ChangeExtension(fileNameWithExtension, ".page.md");

                // YAML entry for a file
                tocEntries.Add($"- name: {fileNameWithoutExtension}"); // Use name without extension
                tocEntries.Add($"  filename: {pageFileName}"); // Add .page.md suffix
            }

            // Only write toc.yaml if there are entries
            if (tocEntries.Any())
            {
                string tocFilePath = Path.Combine(directoryPath, "toc.yaml");
                try
                {
                    File.WriteAllLines(tocFilePath, tocEntries); // Write lines directly
                    Logger.Cyan($"  Successfully wrote: {Path.Combine(Path.GetFileName(directoryPath), "toc.yaml")}");
                }
                catch (IOException ioEx)
                {
                    Logger.Error($"    Error writing toc.yaml in {Path.GetFileName(directoryPath)}: {ioEx.Message}");
                }
            }
            else
            {
                Logger.Warning($"  No files or subdirectories found in {directoryPath}. Skipping toc.yaml generation.");
            }
            // --- Recurse into subdirectories ---
            foreach (var subDir in subDirectories)
            {
                GenerateTocForDirectory(subDir); // Recursive call
            }
        }
        catch (Exception ex)
        {
            // Log errors accessing directory contents etc.
            Logger.Error($"Error processing directory for TOC {directoryPath}: {ex.Message}");
        }
    } 
}