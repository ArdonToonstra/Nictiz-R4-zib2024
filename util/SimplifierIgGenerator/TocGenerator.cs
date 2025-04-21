// TocGenerator.cs
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text; // Required for StringBuilder used implicitly by List<string> with WriteAllLines

public static class TocGenerator
{
    public static void GenerateTocFiles(string homeDirectoryPath, List<Resource> resources)
    {
        if (!Directory.Exists(homeDirectoryPath))
        {
            Logger.Warning($"Home directory not found at {homeDirectoryPath}. Skipping TOC generation.");
            return;
        }
        Logger.Info("\nGenerating toc.yaml files...");
        // Pass resources down
        GenerateTocForDirectory(homeDirectoryPath, resources);
    }


    public static void GenerateStaticRootToc(string rootTocPath)
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

    private static void GenerateTocForDirectory(string directoryPath, List<Resource> resources)
    {
    try
    {
        var tocEntries = new List<string>();
        string currentDirName = Path.GetFileName(directoryPath);
        string? parentDirName = Path.GetFileName(Path.GetDirectoryName(directoryPath)); // Get parent directory name

        // --- Create Index.page.md for subdirectories under Resources ---
        // Check if the PARENT directory is 'Resources'. Home/Resources/Conformance -> parent is Resources
        bool isResourceSubdirectory = parentDirName != null &&
                                      parentDirName.Equals("Resources", StringComparison.OrdinalIgnoreCase);

        if (isResourceSubdirectory)
        {
            string indexPageName = "Index.page.md";
            string indexFilePath = Path.Combine(directoryPath, indexPageName);
            string indexFileContent = $"## {{{{page-title}}}}\n{{{{index:current}}}}"; // Use current directory name as title?

             // Use directory name for the index page title in TOC, unless it's empty
             string indexDisplayName = string.IsNullOrWhiteSpace(currentDirName) ? "Index" : currentDirName;

             // Escape special characters for YAML name
             if (indexDisplayName.Contains(':'))
             {
                 indexDisplayName = $"\"{indexDisplayName.Replace("\"", "\\\"")}\"";
             }

             // Prepend the Index entry to the TOC list for this directory
             tocEntries.Add($"- name: {indexDisplayName}"); // Use Directory name for the Index link text
             tocEntries.Add($"  filename: {indexPageName}");

            try
            {
                // Only write if it doesn't exist or if we want to overwrite
                // For simplicity, we'll overwrite here. Add File.Exists check if needed.
                File.WriteAllText(indexFilePath, indexFileContent);
                string relativeIndexPath = Path.Combine(currentDirName, indexPageName);
                Logger.Success($"  Successfully wrote: {relativeIndexPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"    Error writing {indexPageName} in '{currentDirName}': {ex.Message}");
            }
        }

        // Get subdirectories, FILTERING OUT TemplatePages
        var subDirectories = Directory.GetDirectories(directoryPath)
                                      .Where(d => !Path.GetFileName(d).Equals("TemplatePages", StringComparison.OrdinalIgnoreCase))
                                      .OrderBy(d => d)
                                      .ToList();

        foreach (var subDir in subDirectories)
        {
            string folderName = Path.GetFileName(subDir);
            // Escape name if needed
             if (folderName.Contains(':'))
             {
                 folderName = $"\"{folderName.Replace("\"", "\\\"")}\"";
             }
            tocEntries.Add($"- name: {folderName}");
            tocEntries.Add($"  filename: {folderName}"); // Link to the subdirectory itself for navigation
        }

        // Get markdown files (excluding the Index.page.md we might have just created)
        var files = Directory.GetFiles(directoryPath, "*.page.md")
                           .Where(f => !Path.GetFileName(f).Equals("Index.page.md", StringComparison.OrdinalIgnoreCase)) // Exclude Index.page.md
                           .OrderBy(f => f)
                           .ToList();

        foreach (var file in files)
        {
            // --- Logic to find Resource Title/ID (from previous step) ---
             string originalFileName = Path.GetFileName(file);
             string filenameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
             string baseFileName = filenameWithoutExtension;
              if (baseFileName.EndsWith(".page", StringComparison.OrdinalIgnoreCase)) {
                   baseFileName = Path.GetFileNameWithoutExtension(baseFileName);
              }

             string nameForDisplay = baseFileName;
             string? resourceId = null;
             string? resourceType = null;

             int firstHyphenIndex = baseFileName.IndexOf('-');
             if (firstHyphenIndex > 0 && firstHyphenIndex < baseFileName.Length - 1)
             {
                 resourceType = baseFileName.Substring(0, firstHyphenIndex);
                 resourceId = baseFileName.Substring(firstHyphenIndex + 1);

                 var matchingResource = resources.FirstOrDefault(r =>
                     r.TypeName.Equals(resourceType, StringComparison.OrdinalIgnoreCase) &&
                     r.Id != null &&
                     r.Id.Equals(resourceId, StringComparison.OrdinalIgnoreCase));

                 if (matchingResource != null)
                 {
                     string? title = null;
                     if (matchingResource is DomainResource domainResource)
                     {
                         if (domainResource is StructureDefinition sd && !string.IsNullOrWhiteSpace(sd.Title)) title = sd.Title;
                         else if (domainResource is ValueSet vs && !string.IsNullOrWhiteSpace(vs.Title)) title = vs.Title;
                         else if (domainResource is CodeSystem cs && !string.IsNullOrWhiteSpace(cs.Title)) title = cs.Title;
                         // Add other resource types here...
                         else if (domainResource is ImplementationGuide ig && !string.IsNullOrWhiteSpace(ig.Name)) title = ig.Name; // Use Name for IG
                     }

                     if (!string.IsNullOrWhiteSpace(title)) nameForDisplay = title;
                     else if (!string.IsNullOrWhiteSpace(matchingResource.Id)) nameForDisplay = matchingResource.Id;
                 }
                 else
                 {
                     Logger.Warning($"    Could not find matching parsed resource for TOC entry: Type='{resourceType}', ID='{resourceId}' (derived from file '{originalFileName}')");
                 }
             }
             else
             {
                  // Only warn if it looks like a resource file (based on location)
                  string? grandParentDirName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(directoryPath)));
                  if (isResourceSubdirectory || (parentDirName != null && parentDirName.Equals("Home", StringComparison.OrdinalIgnoreCase) && currentDirName.Equals("Resources", StringComparison.OrdinalIgnoreCase)))
                  {
                      Logger.Warning($"    Unexpected filename format for TOC name extraction in '{currentDirName}': {originalFileName}. Using base filename.");
                  }
             }

            // Escape special characters in name if needed for YAML
            if (nameForDisplay.Contains(':'))
            {
                nameForDisplay = $"\"{nameForDisplay.Replace("\"", "\\\"")}\""; // Quote and escape quotes
            }

             // Determine the correct filename for the link
             string filenameForLink = filenameWithoutExtension.EndsWith(".page", StringComparison.OrdinalIgnoreCase)
                                       ? originalFileName
                                       : baseFileName + ".page.md";
            // --- End Resource Title/ID Logic ---


            tocEntries.Add($"- name: {nameForDisplay}");
            tocEntries.Add($"  filename: {filenameForLink}");
        }


        // Write toc.yaml if entries exist
        if (tocEntries.Any())
        {
            string tocFilePath = Path.Combine(directoryPath, "toc.yaml");
            try
            {
                File.WriteAllLines(tocFilePath, tocEntries);
                string relativeDir = Path.GetRelativePath(Directory.GetParent(directoryPath)?.Parent?.FullName ?? directoryPath, directoryPath);
                Logger.Success($"  Successfully wrote: {Path.Combine(relativeDir, "toc.yaml")}");
            }
            catch (IOException ioEx)
            {
                Logger.Error($"    Error writing toc.yaml in '{currentDirName}': {ioEx.Message}");
            }
        }

        // Recurse into subdirectories (pass resources list down)
        foreach (var subDir in subDirectories)
        {
            GenerateTocForDirectory(subDir, resources); // Pass resources list
        }
    }
    catch (Exception ex)
    {
        Logger.Error($"Error processing directory for TOC {directoryPath}: {ex.Message}");
    }
 }
}