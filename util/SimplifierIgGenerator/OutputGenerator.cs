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

            if (!string.IsNullOrEmpty(settings.SourceStylesDirectory) && Directory.Exists(settings.SourceStylesDirectory))
            {
                 AssetCopier.CopyStylesDirectoryIfNotExists(settings.SourceStylesDirectory, destStylesPath);
            }
            if (!string.IsNullOrEmpty(settings.SourceTemplatePagesDirectory) && Directory.Exists(settings.SourceTemplatePagesDirectory))
            {
                 AssetCopier.CopyStaticAssetDirectoryIfNotExists(settings.SourceTemplatePagesDirectory, destTemplatePagesPath, "TemplatePages");
            }
            GenerateGuideYaml(settings, guideYamlPath);
            CategorizedFiles categorizedFileNames = GenerateResourceFiles(settings, resources, conformancePath, terminologyPath, examplesPath);
            GenerateIndexPage(indexPagePath, categorizedFileNames);
            TocGenerator.GenerateStaticRootToc(rootTocPath);
            TocGenerator.GenerateTocFiles(homeDirectoryPath, resources); 

        }
        catch (Exception ex)
        {
            Logger.Error($"Error creating or accessing output directory structure {baseIgOutputPath}: {ex.Message}");
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


}