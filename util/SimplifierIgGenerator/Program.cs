// Program.cs
using Hl7.Fhir.Model;
using Microsoft.Extensions.Configuration; 
using Microsoft.Extensions.DependencyInjection; 
using Microsoft.Extensions.Logging; 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; 

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting FHIR IG Generator..."); 

        AppSettings? settings = null; 

        try
        {
            // 1. Load Configuration
            settings = ConfigurationLoader.LoadAppSettings();
            if (settings == null)
            {
                Console.WriteLine("\nConfiguration loading failed. Exiting.");
                return; 
            }

            if (!string.IsNullOrEmpty(settings.OutputDirectory) && !string.IsNullOrEmpty(settings.IgName))
            {
                 string baseIgOutputPath = Path.Combine(settings.OutputDirectory, settings.IgName);
                 string logFilePath = Path.Combine(baseIgOutputPath, "generator.log");
                 Logger.Init(logFilePath); 
            }
            else {
                 Logger.Warning("OutputDirectory or IgName not set, file logging will be skipped.");
            }

            // 2. Process Input Directories
            List<Resource> parsedResources = InputProcessor.ParseInputDirectories(settings);

            // 3. Generate Output
            if (parsedResources.Any() && !string.IsNullOrEmpty(settings.OutputDirectory) && !string.IsNullOrEmpty(settings.IgName))
            {
                OutputGenerator.GenerateOutput(settings, parsedResources);
            }
            else if (!parsedResources.Any())
            {
                Logger.Info("\nNo FHIR resources found or parsed successfully. Skipping output generation.");
            }
        

            Logger.Success("\nProcessing finished successfully."); 

        }
        catch (Exception ex)
        {
            Logger.Error($"\nAn unexpected error occurred: {ex.Message}");
            Logger.Error($"Stack Trace: {ex.StackTrace}");
        }
        finally
        {
           
            Logger.Close(); 
            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }
    }
}