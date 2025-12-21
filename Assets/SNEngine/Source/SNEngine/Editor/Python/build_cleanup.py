import os
import shutil
import sys

script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.abspath(os.path.join(script_dir, "../../../../../.."))

PATHS_TO_DELETE = [
    "Assets/SNEngine/Source/SNEngine/Resources/Custom",
    "Assets/StreamingAssets",
    "Assets/SNEngine/Demo",
    "Assets/SNEngine/Source/SNEngine/Resources/Editor/TextTemplates/custom_ui_template.yaml"
]

PATHS_TO_CLEAR = [
    "Assets/SNEngine/Source/SNEngine/Resources/Characters",
    "Assets/SNEngine/Source/SNEngine/Resources/Dialogues"
]

def cleanup():
    print(f"Checking assets in: {project_root}")
    changes_made = False
    
    for path in PATHS_TO_DELETE:
        full_path = os.path.join(project_root, path)
        if os.path.exists(full_path):
            shutil.rmtree(full_path)
            print(f"Removed: {path}")
            changes_made = True

    for path in PATHS_TO_CLEAR:
        full_path = os.path.join(project_root, path)
        if os.path.exists(full_path) and os.listdir(full_path):
            for item in os.listdir(full_path):
                item_path = os.path.join(full_path, item)
                if os.path.isfile(item_path):
                    os.remove(item_path)
                elif os.path.isdir(item_path):
                    shutil.rmtree(item_path)
            print(f"Cleared: {path}")
            changes_made = True

    if not changes_made:
        print("Everything is already clean. No actions needed.")

if __name__ == "__main__":
    cleanup()