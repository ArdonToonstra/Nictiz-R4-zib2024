import argparse
import logging
from config_manager import ConfigManager
from fhir_parser import FHIRResourceParser
from markdown_generator import MarkdownGenerator
import os

if __name__ == "__main__":
    # Argument parser for command-line arguments
    parser = argparse.ArgumentParser(description="Parse FHIR resources and generate markdown files.")
    parser.add_argument(
        "--config",
        type=str,
        required=False,
        default="util/IGpageGenerator/config.yaml",
        help="Path to the configuration file (default: config.yaml)."
    )
    args = parser.parse_args()

    # Initialize and load configuration
    config_manager = ConfigManager(config_path=args.config)

    try:
        config_manager.load_config()
        config_manager.setup_logging()
        input_dirs = config_manager.get_input_dirs()
        output_dir = config_manager.get_output_dir()
    except Exception as e:
        print(f"Configuration error: {e}")
        exit(1)

    # Parse FHIR resources
    parsed_resources = FHIRResourceParser.parse_fhir_resources(input_dirs)

    # Initialize MarkdownGenerator
    markdown_generator = MarkdownGenerator(output_dir)

    # Generate markdown files for parsed resources
    for resource in parsed_resources:
        markdown_generator.generate_markdown(
            resource_type=resource["resourceType"],
            resource_id=resource["id"],
            resource=resource["resource"]
        )

    print(f"Generated markdown files for {len(parsed_resources)} resources.")


    # Generate a markdown list for zib-* StructureDefinitions
    zib_resources = sorted(
        [res for res in parsed_resources if res["resourceType"] == "StructureDefinition" and res["id"].startswith("zib-")],
        key=lambda x: x["id"]
    )

    # Define the markdown file path
    zib_list_filepath = os.path.join(output_dir, "zib_structure_definitions.md")

    # Write the list to the markdown file
    with open(zib_list_filepath, "w", encoding="utf-8") as md_file:
        md_file.write("# Zib StructureDefinitions\n\n")
        for res in zib_resources:
            md_file.write(f"* {{{{pagelink:{res['resourceType']}-{res['id']}}}}}\n")

    print(f"Markdown file for zib-* StructureDefinitions created at: {zib_list_filepath}")