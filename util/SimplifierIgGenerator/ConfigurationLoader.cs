// ConfigurationLoader.cs
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

public static class ConfigurationLoader
{
    public static AppSettings? LoadAppSettings()
    {
        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var settings = configuration.GetSection("Settings").Get<AppSettings>();

            // Basic validation (can be expanded)
            if (settings == null)
            {
                Logger.Error("Error: 'Settings' section missing or empty in appsettings.json.");
                return null;
            }
            if (settings.InputDirectories == null || !settings.InputDirectories.Any())
            {
                Logger.Error("Error: InputDirectories not configured in appsettings.json.");
                return null; 
            }
             if (string.IsNullOrEmpty(settings.OutputDirectory))
             {
                Logger.Warning("Warning: OutputDirectory not configured in appsettings.json.");
             }
             if (string.IsNullOrEmpty(settings.IgName))
             {
                Logger.Error("Error: IgName not configured in appsettings.json.");
                return null;
             }

            if (!string.IsNullOrEmpty(settings.SourceStylesDirectory))
            {
                if (!Directory.Exists(settings.SourceStylesDirectory))
                {                
                    Logger.Warning($"Configured SourceStylesDirectory not found: {settings.SourceStylesDirectory}");
                }
                else
                {
                    Logger.Info($"Source styles directory found: {settings.SourceStylesDirectory}");
                }
            }
            else
            {
                 Logger.Info("SourceStylesDirectory not configured. Styles will not be copied.");
            }

            Logger.Success("Configuration loaded successfully.");
            return settings;
        }
        catch (FileNotFoundException)
        {
            Logger.Error("Error: appsettings.json not found in the current directory.");
            return null;
        }
        catch (Exception ex) // Catch other potential issues like invalid JSON
        {
             
            Logger.Error($"Error loading configuration: {ex.Message}");
            return null;
        }
    }
}