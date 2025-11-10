# src/icons.py

# Цвет иконок (светло-серый, #CCCCCC, для темной темы)
ICON_COLOR = "#CCCCCC"
FILL_COLOR = "#FFC107" # Желтый для папки, как в Unity

# 1. Иконка папки (Folder Icon) - Напоминает структуру Unity
SVG_FOLDER_ICON = f"""
<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <path fill="{FILL_COLOR}" d="M14 6H8.5L7 4.5H2a1 1 0 0 0-1 1V11a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V7a1 1 0 0 0-1-1zM2 5h4.5l1 1h6.5v4H2V5z"/>
</svg>
"""

# 2. Иконка YAML файла (File Icon) - Имитируем иконку документа
# YAML-файл часто представляется как документ с особым цветом.
SVG_YAML_FILE_ICON = f"""
<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <path fill="{ICON_COLOR}" d="M4 1h8a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1zM4 2v12h8V2H4z"/>
  <text x="8" y="10" font-family="Arial, sans-serif" font-size="8" font-weight="bold" fill="#007bff" text-anchor="middle">
    Y
  </text>
</svg>
"""

SVG_CLOSE_ICON = """
<svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
    <path d="M18 6L6 18M6 6L18 18" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
</svg>
"""
