// MarkdownGenerator.cs
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Text;

public static class MarkdownGenerator
{
    /// <summary>
    /// Generates the appropriate Markdown content string for a given FHIR resource.
    /// </summary>
    /// <param name="resource">The FHIR resource.</param>
    /// <param name="baseFileName">The base filename (Type-Id) used for topic links etc.</param>
    /// <returns>A string containing the generated Markdown.</returns>
    public static string GenerateMarkdown(Resource resource, string baseFileName)
    {
        var markdownContent = new StringBuilder();

        // Use 'is' pattern matching for cleaner type checks and casting
        switch (resource)
        {
            case StructureDefinition sd:
                GenerateStructureDefinitionMarkdown(markdownContent, sd, baseFileName);
                break;

            // Future cases for other specific resource types can be added here:
            // case ValueSet vs:
            //     GenerateValueSetMarkdown(markdownContent, vs, baseFileName);
            //     break;
            // case CodeSystem cs:
            //     GenerateCodeSystemMarkdown(markdownContent, cs, baseFileName);
            //     break;

            default: // Fallback for any resource type not specifically handled
                GenerateDefaultMarkdown(markdownContent, resource);
                break;
        }

        return markdownContent.ToString();
    }

    // --- Specific generator for StructureDefinition ---
    private static void GenerateStructureDefinitionMarkdown(StringBuilder sb, StructureDefinition sd, string baseFileName)
    {
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
        sb.AppendLine("{{page:Resource-StructureDefinition-View}}");
    }

    // --- Specific generator for ValueSet (Example - Implement content later) ---
    // private static void GenerateValueSetMarkdown(StringBuilder sb, ValueSet vs, string baseFileName)
    // {
    //     sb.AppendLine("---");
    //     sb.AppendLine($"topic: {baseFileName}");
    //     // Add other front matter for ValueSet
    //     sb.AppendLine("---");
    //     sb.AppendLine($"# ValueSet: {vs.Name ?? vs.Id}");
    //     // Add specific ValueSet page includes or content
    // }


    // --- Default generator for unspecified resource types ---
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