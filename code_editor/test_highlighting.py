#!/usr/bin/env python3
"""
Test script to verify the syntax highlighting works for multiple template parameters
"""
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'src'))

from PyQt5.QtWidgets import QApplication, QTextEdit
from PyQt5.QtGui import QSyntaxHighlighter, QTextDocument
from src.snil_highlighter import SNILHighlighter

def test_highlighting():
    app = QApplication(sys.argv)
    
    # Create a text document and highlighter
    doc = QTextDocument()
    highlighter = SNILHighlighter(doc)
    
    # Test cases for different commands with multiple parameters
    test_lines = [
        "Show {_character} with emotion {_emotion}",
        "Show Nagatoro with emotion Naga_Lean_Bikini_Sidelook_CatOpen",
        "Compare {a} {type} {b}",
        "Wait {seconds} seconds",
        "Nagatoro says Hello world!",
        "Jump To {dialogue}",
        "Print {message}"
    ]
    
    print("Testing syntax highlighting for multiple template parameters...")
    for line in test_lines:
        doc.setPlainText(line)
        # The highlighting happens automatically when text is set
        print(f"Line: {line}")
        # For a complete test, we'd need to check the actual formatting,
        # but for now we're just ensuring no errors occur
    print("Test completed successfully - no errors occurred!")

if __name__ == "__main__":
    test_highlighting()