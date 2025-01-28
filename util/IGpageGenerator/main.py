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
    parser.add_argument(
        "--output_dir",
        type=str,
        required=False,
        default="output",
        help="Directory to store generated markdown files (default: output)."
    )
    args = parser.parse_args()

    # Initialize and load configuration
    config_manager = ConfigManager(config_path=args.config)

    try:
        config_manager.load_config()
        config_manager.setup_logging()
        input_dirs = config_manager.get_input_dirs()
    except Exception as e:
        print(f"Configuration error: {e}")
        exit(1)

    # Parse FHIR resources
    parsed_resources = FHIRResourceParser.parse_fhir_resources(input_dirs)

    # Initialize MarkdownGenerator
    markdown_generator = MarkdownGenerator(output_dir=args.output_dir)

    # Generate markdown files for parsed resources
    for resource in parsed_resources:
        markdown_generator.generate_markdown(
            resource_type=resource["resourceType"],
            resource_id=resource["id"]
        )

    print(f"Generated markdown files for {len(parsed_resources)} resources.")