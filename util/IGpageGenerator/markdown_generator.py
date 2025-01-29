import os
import logging
from pathlib import Path

class MarkdownGenerator:
    """
    Handles the generation of Markdown files for parsed FHIR resources.
    """

    def __init__(self, output_dir):
        """
        Initialize the MarkdownGenerator with the output directory.

        Args:
            output_dir (str): Path to the directory where Markdown files will be generated.
        """
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        logging.info(f"MarkdownGenerator initialized with output directory: {self.output_dir}")

    def generate_markdown(self, resource_type, resource_id, resource):
        """
        Generate a Markdown file for the given resource.

        Args:
            resource_type (str): The FHIR resource type (e.g., Patient, Observation).
            resource_id (str): The unique identifier of the resource.
            resource (dict): The complete parsed FHIR resource as a dictionary.

        Returns:
            str: Path to the generated Markdown file.
        """
        if not resource_type or not resource_id:
            logging.warning("Invalid resource type or ID. Skipping Markdown generation.")
            return None

        md_file_name = f"{resource_type}-{resource_id}.page.md"
        md_file_path = self.output_dir / md_file_name

        try:
            # Create the file and write default content (currently empty)
            with open(md_file_path, 'w', encoding='utf-8') as md_file:
                if resource_type == 'StructureDefinition':
                    resource_url = resource.url
                    resource_title = resource.title

                    md_file.write(f"---\n")
                    md_file.write(f"topic: {resource_type}-{resource_id}\n")
                    md_file.write(f"canonical: {resource_url}\n")
                    md_file.write(f"buttons: yes\n")
                    md_file.write(f"expand: 2\n")
                    md_file.write(f"---\n\n")
                    md_file.write(f"# {resource_title}\n")
                    md_file.write("{{page:Metadata-table}}\n")
                    md_file.write("{{page:FQL-get-resource-description}}\n\n")
                    md_file.write("{{page:Resource-StructureDefinition-View}}\n\n")
                    md_file.write("<!---start manual part--->\n")

                md_file.write("")  # Placeholder for future content
            logging.info(f"Generated Markdown file: {md_file_path}")
            return str(md_file_path)
        except Exception as e:
            logging.error(f"Failed to generate Markdown file {md_file_path}: {e}")
            return None
