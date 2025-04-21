// InputProcessor.cs
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class InputProcessor
{
    public static List<Resource> ParseInputDirectories(AppSettings settings)
    {
        var parsedResources = new List<Resource>();

        Logger.Info("\nStarting input processing...");

        foreach (var inputDir in settings.InputDirectories)
        {
            if (!Directory.Exists(inputDir))
            {        
                Logger.Warning($"Warning: Input directory not found, skipping: {inputDir}");
                continue;
            }

            Logger.Info($"Processing directory: {inputDir}");

            var potentialFiles = Directory.EnumerateFiles(inputDir, "*.*", SearchOption.AllDirectories)
                                          .Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                                                      f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

            foreach (var filePath in potentialFiles)
            {
                try
                {
                    string fileContent = File.ReadAllText(filePath);
                    Resource? resource = null;
                    var parserSettings = new ParserSettings
                    {
                        AcceptUnknownMembers = true,
                        AllowUnrecognizedEnums = true
                    };

                    if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        var jsonParser = new FhirJsonParser(parserSettings);
                        resource = jsonParser.Parse<Resource>(fileContent);
                    }
                    else // Must be XML
                    {
                        var xmlParser = new FhirXmlParser(parserSettings);
                        resource = xmlParser.Parse<Resource>(fileContent);
                    }

                    if (resource != null)
                    {
                        parsedResources.Add(resource);
                        // Optional: Log successful parsing
                        // Console.ForegroundColor = ConsoleColor.Green;
                        // Console.WriteLine($"    Successfully parsed as {resource.TypeName} (ID: {resource.Id ?? "none"})");
                        // Console.ResetColor();
                    }
                }
                catch (FormatException fe)
                {
                    Logger.Error($"    Error parsing FHIR content in {Path.GetFileName(filePath)}: {fe.Message}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"    Unexpected error processing file {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }
        }

        Logger.Success($"\nParsing complete. Found {parsedResources.Count} FHIR resources.");
        PrintResourceSummary(parsedResources); // Keep the summary logic here or move it too

        return parsedResources;
    }

    // Helper method to print summary (could also be in its own class)
    private static void PrintResourceSummary(List<Resource> resources)
    {
         if (resources.Any())
         {
             Logger.Success("\n--- Summary: List of Parsed Resources ---");
             int resourceNumber = 1;
             foreach (var resource in resources)
             {
                 Logger.Success($"{resourceNumber,3}. Type: {resource.TypeName,-25} ID: {resource.Id ?? "[No ID]"}");
                 resourceNumber++;
             }
             Logger.Success("-----------------------------------------");
         }
    }
}