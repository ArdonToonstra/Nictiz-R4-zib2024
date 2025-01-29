from xml.etree import ElementTree as ET
from lxml import etree
import os
import json
import logging
from pathlib import Path
from importlib import import_module


class FHIRResourceParser:
    """A class to handle parsing of FHIR resources in XML and JSON formats."""

    @staticmethod
    def import_from(module, name):
        """Dynamically imports a class from a module."""
        try:
            module = import_module(module)
            return getattr(module, name)
        except (ImportError, AttributeError) as e:
            logging.error(f"Failed to import {name} from {module}: {e}")
            raise

    @staticmethod
    def strip_xml_comments(xml_bytes):
        """
        Strip comments from an XML string.
        
        Args:
            xml_bytes (bytes): XML content in bytes.
        
        Returns:
            bytes: XML content without comments.
        """
        try:
            parser = etree.XMLParser(remove_comments=True)
            root = etree.fromstring(xml_bytes, parser=parser)
            cleaned_xml = etree.tostring(root, encoding="utf-8", xml_declaration=True)
            logging.info("Stripped comments from XML content.")
            return cleaned_xml
        except Exception as e:
            logging.error(f"Failed to strip comments from XML: {e}")
            raise
    
    @staticmethod
    def parse_json(file_path):
        """Parse a JSON FHIR resource."""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                resource = json.load(f)
            logging.info(f"Successfully parsed JSON file: {file_path}")
            return resource
        except Exception as e:
            logging.error(f"Could not parse JSON file {file_path}: {e}")
            return None
    
    @staticmethod
    def parse_xml(file_path):
        """Parse an XML FHIR resource dynamically by inspecting the root element."""
        try:
            xml_bytes = Path(file_path).read_bytes()  # Read the XML file as bytes
            root = ET.fromstring(xml_bytes)  # Parse XML to get the root element
            resource_name = root.tag.replace('{http://hl7.org/fhir}', '')  # Extract resource name

            cleaned_xml = FHIRResourceParser.strip_xml_comments(xml_bytes) # Need to strip comments otherwise it cannot be parsed into the FHIR model.

            # Dynamically import and validate the resource class
            try:
                resourceType = FHIRResourceParser.import_from(f"fhir.resources.R4B.{resource_name.lower()}", resource_name)
                resource = resourceType.model_validate_xml(cleaned_xml)
                logging.info(f"Successfully parsed {resource_name}: {file_path}")
                return resource
            except Exception as e:
                logging.warning(f"Unknown or unsupported FHIR resource type '{resource_name}' in file: {file_path} - {e}")
                return None
        except Exception as e:
            logging.error(f"Could not parse XML file {file_path}: {e}")
            return None

    @staticmethod
    def parse_fhir_resources(input_dirs):
        """
        Parse all .xml and .json FHIR resources in specified folders (including subfolders).

        Args:
            input_dirs (list): List of directories to parse.

        Returns:
            list: A list of dictionaries containing resourceType, id, and file_path and the resource itself.
        """
        resources = []

        for path in input_dirs:
            for root, _, files in os.walk(path):
                for file_name in files:
                    file_path = os.path.join(root, file_name)
                    resource = None

                    # Parse JSON and XML files
                    if file_name.endswith('.json'):
                        resource = FHIRResourceParser.parse_json(file_path)
                    elif file_name.endswith('.xml'):
                        resource = FHIRResourceParser.parse_xml(file_path)

                    # Collect resource metadata if successfully parsed
                    if resource:
                        resource_type = resource.get_resource_type()
                        resource_id = resource.id
                        resources.append({
                            "resourceType": resource_type,
                            "id": resource_id,
                            "file_path": file_path,
                            "resource": resource	
                        })

        return resources