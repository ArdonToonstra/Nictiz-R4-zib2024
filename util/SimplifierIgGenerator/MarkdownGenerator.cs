// MarkdownGenerator.cs
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System; // Needed for StringComparison
using System.Text;


public static class MarkdownGenerator
{
    /// <summary>
    /// Generates the appropriate Markdown content string for a given FHIR resource.
    /// </summary>
    /// <param name="resource">The FHIR resource.</param>
    /// <param name="baseFileName">The base filename (Type-Id) used for topic links etc.</param>
    /// <param name="category">The category ("Conformance", "Terminology", "Example", etc.)</param> /// <New Parameter>
    /// <returns>A string containing the generated Markdown.</returns>
    public static string GenerateMarkdown(Resource resource, string baseFileName, string category) // <Signature Updated>
    {
        var markdownContent = new StringBuilder();

        // *** Switch based on the category passed from OutputGenerator ***
        switch (category)
        {
            case "Conformance":
                // Still check specific type if needed within the category
                if (resource is StructureDefinition sd)
                {
                    GenerateStructureDefinitionMarkdown(markdownContent, sd, baseFileName);
                }
                else
                {
                    // Fallback for other Conformance types (e.g., SearchParameter)
                    GenerateDefaultMarkdown(markdownContent, resource); // Use JSON dump for now
                }
                break;

            case "Terminology":
                 // Add specific templates for ValueSet, CodeSystem etc. here later if needed
                 // For now, use the default JSON dump
                 GenerateDefaultMarkdown(markdownContent, resource);
                 break;

            case "Example":
                GenerateExampleMarkdown(markdownContent, resource, baseFileName);
                break;

            default: // True fallback for any unexpected category or resource
                Logger.Warning($"Unexpected resource category '{category}' encountered for {baseFileName}. Using default markdown.");
                GenerateDefaultMarkdown(markdownContent, resource);
                break;
        }

        return markdownContent.ToString();
    }

    // --- Specific generator for StructureDefinition (Unchanged) ---
    private static void GenerateStructureDefinitionMarkdown(StringBuilder sb, StructureDefinition sd, string baseFileName)
    {
        // ... (content remains the same) ...
        string title = string.IsNullOrWhiteSpace(sd.Title) ? (sd.Id ?? "Unknown") : sd.Title;
        string canonical = sd.Url ?? "urn:undefined";
        sb.AppendLine("---");
        sb.AppendLine($"topic: {baseFileName}");
        sb.AppendLine($"canonical: {canonical}");
        sb.AppendLine("buttons: yes");
        sb.AppendLine("expand: 2");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {title}");
        sb.AppendLine();
        sb.AppendLine("{{page:Metadata-table}}");
        sb.AppendLine("{{page:FQL-get-resource-description}}");
        sb.AppendLine("{{page:Resource-Content-View}}");
    }

    private static void GenerateExampleMarkdown(StringBuilder sb, Resource resource, string baseFileName)
    {
        string subjectValue = baseFileName;
        int firstHyphenIndex = baseFileName.IndexOf('-');
        if (firstHyphenIndex >= 0)
        {
            // Replace first hyphen with slash
            subjectValue = baseFileName.Substring(0, firstHyphenIndex) + "/" + baseFileName.Substring(firstHyphenIndex + 1);
        }
        // If no hyphen, subjectValue remains baseFileName

        sb.AppendLine("---");
        sb.AppendLine($"topic: {baseFileName}");
        sb.AppendLine($"subject: {subjectValue}"); // Use the modified value
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# {{page-title}}");
        sb.AppendLine();
        sb.AppendLine("{{page:Resource-Example}}");
    }

    // --- Default generator (JSON dump - now the true fallback) ---
    private static void GenerateDefaultMarkdown(StringBuilder sb, Resource resource)
    {
        sb.AppendLine($"# {resource.TypeName}: {resource.Id ?? "[No ID]"}");
        sb.AppendLine();
        sb.AppendLine("```json");
        var serializerSettings = new SerializerSettings { Pretty = true };
        var jsonSerializer = new FhirJsonSerializer(serializerSettings);
        sb.AppendLine(jsonSerializer.SerializeToString(resource));
        sb.AppendLine("```");
    }
}