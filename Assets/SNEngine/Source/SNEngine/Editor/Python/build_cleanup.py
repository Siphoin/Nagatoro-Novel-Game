import os
import shutil

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
    for path in PATHS_TO_DELETE:
        full_path = os.path.join(project_root, path)
        if os.path.exists(full_path):
            try:
                if os.path.isdir(full_path):
                    shutil.rmtree(full_path)
                else:
                    os.remove(full_path)
            except Exception:
                pass

    for path in PATHS_TO_CLEAR:
        full_path = os.path.join(project_root, path)
        if os.path.exists(full_path):
            for item in os.listdir(full_path):
                p = os.path.join(full_path, item)
                try:
                    if os.path.isdir(p): shutil.rmtree(p)
                    else: os.remove(p)
                except Exception:
                    pass

    webgl_root = os.path.join(project_root, "Assets/WebGLTemplates")
    if os.path.exists(webgl_root):
        for item in os.listdir(webgl_root):
            if item == "SNEngine":
                continue
            
            item_path = os.path.join(webgl_root, item)
            try:
                if os.path.isdir(item_path):
                    shutil.rmtree(item_path)
                else:
                    os.remove(item_path)
            except Exception:
                pass

if __name__ == "__main__":
    cleanup()