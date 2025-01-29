import logging
import yaml
import os

class ConfigManager:
    """
    Handles configuration loading, validation, and logging setup.
    """
    def __init__(self, config_path, default_output_dir="guides", default_log_file="logging.log", default_log_level="INFO"):
        self.config_path = config_path
        self.default_output_dir = default_output_dir
        self.default_log_file = default_log_file
        self.default_log_level = default_log_level
        self.config = None

    def load_config(self):
        """
        Load and validate the configuration from a YAML file.

        Raises:
            FileNotFoundError: If the config file is missing.
            ValueError: If required fields are missing or invalid.
        """
        if not os.path.exists(self.config_path):
            raise FileNotFoundError(f"Configuration file not found: {self.config_path}")
        
        try:
            with open(self.config_path, 'r') as f:
                self.config = yaml.safe_load(f)
            
            if not self.config or "input_dirs" not in self.config:
                raise ValueError("The configuration file must specify 'input_dirs'.")
            
        except yaml.YAMLError as e:
            raise ValueError(f"Failed to parse YAML configuration file: {e}")

    def get_input_dirs(self):
        """
        Retrieve input directories from the configuration.

        Returns:
            list: List of input directories.
        """
        return self.config.get("input_dirs", [])

    def get_output_dir(self):
        """
        Retrieve output directory from the configuration.

        Returns:
            string: output directoru.
        """
        return self.config.get("output_dir", self.default_output_dir)

    def setup_logging(self):
        """
        Configure logging using settings from the configuration file or defaults.
        """
        log_file = self.config.get("log_file", self.default_log_file)
        log_level = self.config.get("log_level", self.default_log_level).upper()

        logging.basicConfig(
            filename=log_file,
            filemode='w',
            level=getattr(logging, log_level, logging.INFO),
            format='%(asctime)s - %(levelname)s - %(message)s'
        )
        logging.info("Logging configured successfully.")
