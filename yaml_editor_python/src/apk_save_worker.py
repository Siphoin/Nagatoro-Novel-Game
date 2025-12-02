"""
Worker class for APK saving operations with progress reporting
"""
import zipfile
import os
import shutil
import tempfile
from PyQt5.QtCore import pyqtSignal
from progress_dialog import ProgressWorker


class ApkSaveWorker(ProgressWorker):
    """
    Worker class for saving changes back to APK with progress reporting
    """
    def __init__(self, current_apk_path, apk_temp_dir, root_localization_path, lang_relative_path):
        super().__init__()
        self.current_apk_path = current_apk_path
        self.apk_temp_dir = apk_temp_dir  # This is the temp APK extraction directory
        self.root_localization_path = root_localization_path  # This is where language files are edited
        self.lang_relative_path = lang_relative_path  # This is the path from APK root to the language directory

    def run_operation(self):
        """
        Perform the APK saving operation in background thread
        """
        try:
            self.operation_started.emit()

            # Create a backup of the original APK
            self.status_updated.emit("Creating backup...")
            self.progress_updated.emit(10, 100, "Creating backup...")

            backup_path = self.current_apk_path + ".backup"
            shutil.copy2(self.current_apk_path, backup_path)

            # Check for cancellation after creating backup
            if self.is_cancelled():
                self.operation_finished.emit(False, "Operation cancelled by user")
                return

            # Extract original APK to temp directory
            self.status_updated.emit("Extracting APK...")
            self.progress_updated.emit(20, 100, "Extracting APK...")

            extract_temp_dir = tempfile.mkdtemp(prefix="apk_rebuild_")

            try:
                with zipfile.ZipFile(self.current_apk_path, 'r') as original_apk:
                    # Get total number of files for progress calculation
                    total_files = len(original_apk.filelist)
                    current_file = 0

                    for file_info in original_apk.filelist:
                        if self.is_cancelled():
                            self.operation_finished.emit(False, "Operation cancelled by user")
                            return

                        original_apk.extract(file_info, extract_temp_dir)
                        current_file += 1
                        progress = 20 + int((current_file / total_files) * 30)  # 20% to 50%
                        self.progress_updated.emit(progress, 100, f"Extracting file {current_file}/{total_files}")

                # Overwrite language files with our updated ones
                # We need to update the language files in the extracted APK with the ones
                # from the root_localization_path (which contains the edited files)
                self.status_updated.emit("Updating language files...")
                self.progress_updated.emit(50, 100, "Updating language files...")

                # Walk through the language root directory to find updated files
                updated_count = 0

                # Use the pre-calculated relative path from the APK root to the language directory
                # This tells us where the language files are located within the APK
                for root_src, dirs_src, files_src in os.walk(self.root_localization_path):
                    for file_src in files_src:
                        if self.is_cancelled():
                            self.operation_finished.emit(False, "Operation cancelled by user")
                            return

                        full_path_src = os.path.join(root_src, file_src)

                        # Calculate the relative path of this file within the language directory
                        rel_path_in_lang = os.path.relpath(full_path_src, self.root_localization_path).replace('\\', '/')

                        # Combine to get the full path within the APK
                        apk_asset_path = os.path.normpath(os.path.join(self.lang_relative_path, rel_path_in_lang)).replace('\\', '/')

                        # Only update language files (check if the APK path is within language folders)
                        if (apk_asset_path.startswith('assets/StreamingAssets/Language/') or
                            apk_asset_path.startswith('StreamingAssets/Language/') or
                            apk_asset_path.startswith('root/assets/StreamingAssets/Language/') or
                            apk_asset_path.startswith('assets/Language/')):

                            full_path_dest = os.path.join(extract_temp_dir, apk_asset_path)

                            # Create directory if it doesn't exist
                            os.makedirs(os.path.dirname(full_path_dest), exist_ok=True)

                            # Copy the updated file from our edited directory to the extracted APK
                            shutil.copy2(full_path_src, full_path_dest)
                            updated_count += 1

                self.status_updated.emit(f"Updated {updated_count} language files")
                self.progress_updated.emit(70, 100, f"Updated {updated_count} language files")

                # Create a new APK with updated files
                self.status_updated.emit("Rebuilding APK...")
                self.progress_updated.emit(80, 100, "Rebuilding APK...")

                temp_apk_path = self.current_apk_path + ".temp"

                with zipfile.ZipFile(temp_apk_path, 'w', zipfile.ZIP_DEFLATED) as new_apk:
                    # Get total number of files for progress calculation
                    all_files = []
                    for root, dirs, files in os.walk(extract_temp_dir):
                        for file in files:
                            full_path = os.path.join(root, file)
                            rel_path = os.path.relpath(full_path, extract_temp_dir)
                            all_files.append((full_path, rel_path))

                    total_files = len(all_files)
                    current_file = 0

                    for full_path, rel_path in all_files:
                        if self.is_cancelled():
                            self.operation_finished.emit(False, "Operation cancelled by user")
                            return

                        new_apk.write(full_path, rel_path)
                        current_file += 1
                        progress = 80 + int((current_file / total_files) * 20)  # 80% to 100%
                        self.progress_updated.emit(progress, 100, f"Adding file {current_file}/{total_files}")

                # Before replacing the original APK, check if cancellation was requested
                if self.is_cancelled():
                    self.operation_finished.emit(False, "Operation cancelled by user")
                    return

                # Replace the original APK with the updated one
                shutil.move(temp_apk_path, self.current_apk_path)

                self.status_updated.emit("Operation completed successfully")
                self.progress_updated.emit(100, 100, "Operation completed successfully")
                self.operation_finished.emit(True, f"Changes saved to APK: {os.path.basename(self.current_apk_path)}")

            finally:
                # Clean up the extraction temp directory
                shutil.rmtree(extract_temp_dir, ignore_errors=True)

        except Exception as e:
            self.operation_finished.emit(False, f"Error saving APK: {str(e)}")