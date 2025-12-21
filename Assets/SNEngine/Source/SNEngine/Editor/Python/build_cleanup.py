import os
import shutil

script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.abspath(os.path.join(script_dir, "../../../../../.."))

# Список объектов для полного удаления (папки или файлы)
PATHS_TO_DELETE = [
    "Assets/SNEngine/Source/SNEngine/Resources/Custom",
    "Assets/StreamingAssets",
    "Assets/SNEngine/Demo",
    "Assets/SNEngine/Source/SNEngine/Resources/Editor/TextTemplates/custom_ui_template.yaml"
]

# Список папок, которые нужно только очистить
PATHS_TO_CLEAR = [
    "Assets/SNEngine/Source/SNEngine/Resources/Characters",
    "Assets/SNEngine/Source/SNEngine/Resources/Dialogues"
]

def cleanup():
    print(f"Cleanup started in: {project_root}")
    changes_made = False
    
    for path in PATHS_TO_DELETE:
        full_path = os.path.join(project_root, path)
        if os.path.exists(full_path):
            try:
                if os.path.isdir(full_path):
                    shutil.rmtree(full_path)
                else:
                    os.remove(full_path)
                print(f"Removed: {path}")
                changes_made = True
            except Exception as e:
                print(f"Failed to remove {path}: {e}")

    for path in PATHS_TO_CLEAR:
        full_path = os.path.join(project_root, path)
        if os.path.exists(full_path) and os.listdir(full_path):
            for item in os.listdir(full_path):
                item_path = os.path.join(full_path, item)
                try:
                    if os.path.isfile(item_path):
                        os.remove(item_path)
                    elif os.path.isdir(item_path):
                        shutil.rmtree(item_path)
                except Exception as e:
                    print(f"Failed to clear {item}: {e}")
            print(f"Cleared folder: {path}")
            changes_made = True

    if not changes_made:
        print("Everything is already clean.")

if __name__ == "__main__":
    cleanup()