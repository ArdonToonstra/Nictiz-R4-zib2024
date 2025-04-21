// AppSettings.cs
using System.Collections.Generic;

public class AppSettings
{
    public List<string> InputDirectories { get; set; } = new List<string>();
    public string OutputDirectory { get; set; } = string.Empty;
    public string IgName { get; set; } = string.Empty; // Name for the IG folder and title
    public string IgVersion { get; set; } = string.Empty; // Version for guide.yaml
    public string IgDescription { get; set; } = string.Empty; // Description for guide.yaml
    public string? SourceStylesDirectory { get; set; } // Path to the source 'styles' folder 
    public string? SourceTemplatePagesDirectory { get; set; } // Path to the source 'template-pages' folder
}