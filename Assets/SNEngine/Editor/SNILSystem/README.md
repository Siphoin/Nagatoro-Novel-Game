# SNIL System Documentation

## Overview

The SNIL (Script Novel Intermediate Language) system is a powerful text-based scripting solution for creating visual novel dialogues in Unity. It allows developers to define complex dialogue flows using simple text-based scripts that are automatically converted into node-based dialogue graphs.

## Key Features

- **Text-based dialogue scripting**: Write dialogues using simple, readable text files
- **Node-based execution**: Automatically converts text scripts to Unity node graphs
- **Flexible parameter system**: Supports complex parameter passing to nodes
- **Resource management**: Built-in system for handling characters, backgrounds, and assets
- **Multi-script support**: Create multiple dialogues in a single file
- **Function system**: Reusable code blocks for common operations
- **Validation**: Comprehensive error checking with detailed messages

## File Structure

The SNIL system stores dialogue assets in:
- `Assets/SNEngine/Source/SNEngine/Resources/Dialogues/` - Compiled dialogue graphs

## Basic Script Structure

A basic SNIL script consists of:

```
name: DialogueName
Start
[dialogue content]
End
```

Example:
```
name: Greeting
Start
Nagatoro says Hello there!
Player says Hi Nagatoro!
Nagatoro says How are you doing today?
End
```

## Creating New Nodes

To add new node types to the SNIL system, you need to create three components:

### Step 1: Create Unity Node Class

First, create your Unity node class that inherits from one of the base node classes:

```csharp
using SiphoinUnityHelpers.XNodeExtensions;
using UnityEngine;

namespace YourNamespace
{
    public class CustomNode : BaseNodeInteraction
    {
        [SerializeField] private string _message;
        [SerializeField] private int _value;

        public override void Execute()
        {
            // Implement your node logic here
            Debug.Log($"Custom node executed: {_message} with value {_value}");
        }
    }
}
```

Your node class should inherit from:
- `BaseNodeInteraction` - for nodes that can connect to other nodes
- `BaseNode` - for basic nodes
- `AsyncNode` - for nodes that take time to execute
- Or other specialized base classes

### Step 2: Create SNIL Template File

Create a template file with the `.snil` extension:

```
// Assets/SNEngine/Source/SNEngine/Editor/SNIL/CustomNode.cs.snil
{_message} with value {_value}
worker:CustomNodeWorker
```

The template should:
- Contain parameter placeholders in curly braces `{parameter_name}` or square brackets `[parameter_name]`
- Optionally specify a worker class with the `worker:` directive
- Use descriptive parameter names that match your node's field names

### Step 3: Create SNIL Worker Class (Optional but Recommended)

Create a worker class to handle parameter assignment:

```csharp
// Assets/SNEngine/Editor/SNILSystem/Workers/CustomNodeWorker.cs
using System.Collections.Generic;
using SiphoinUnityHelpers.XNodeExtensions;
using SNEngine.Editor.SNILSystem.Workers;

namespace SNEngine.Editor.SNILSystem.Workers
{
    public class CustomNodeWorker : SNILWorker
    {
        public override void ApplyParameters(BaseNode node, Dictionary<string, string> parameters)
        {
            // Custom parameter assignment logic
            // This method will be called to set parameters on your node
            foreach (var kvp in parameters)
            {
                var field = node.GetType().GetField(kvp.Key);
                if (field != null)
                {
                    // Convert and assign the value
                    var value = ConvertValue(kvp.Value, field.FieldType);
                    field.SetValue(node, value);
                }
            }
        }

        private object ConvertValue(string value, System.Type targetType)
        {
            // Convert string value to target type
            if (targetType == typeof(string)) return value;
            if (targetType == typeof(int)) return int.Parse(value);
            // Add more type conversions as needed
            return value;
        }
    }
}
```

If you don't create a custom worker, the system will use `GenericNodeWorker` which applies parameters via reflection.

### Step 4: Use Your New Node

Now you can use your new node in SNIL scripts:

```
name: MyDialogue
Start
Nagatoro says Hello!
CustomNode says "This is custom" with value 42
End
```

## Core Node Types

### Dialogue Nodes
```
[Character name] says [dialogue text]
```

### Start and End Nodes
```
Start
[dialogue content]
End
```

### Jump Nodes
```
Jump To [dialogue name]
```

## Character System Nodes

The system includes special support for character management:

### Show Character Node
Shows a character with a specific emotion:

**Template** (`ShowCharacterNode.cs.snil`):
```
Show {_character} with emotion {_emotion}
worker:ShowCharacterNodeWorker
```

**Usage in SNIL script**:
```
name: CharacterExample
Start
Show Nagatoro with emotion Happy
Nagatoro says Hello!
End
```

### Hide Character Node
Hides a character:

**Template** (`HideCharacterNode.cs.snil`):
```
Hide {_character}
worker:HideCharacterNodeWorker
```

**Usage in SNIL script**:
```
name: CharacterHideExample
Start
Show Nagatoro with emotion Happy
Nagatoro says Hello!
Hide Nagatoro
End
```

## Background System Nodes

The system includes support for background management:

### Show Background Node
Sets a background image:

**Template** (`SetBackgroundNode.cs.snil`):
```
Show Background {_background}
worker:SetBackgroundNodeWorker
```

The `_background` parameter can be:
- Just the filename (e.g., `beachBackground`) - searches for the asset by name
- Full path (e.g., `SNEngine/Demo/Sprites/beachBackground.png`) - uses exact path

If multiple assets have the same name, specify the full path or you'll get an error.

**Usage in SNIL script**:
```
name: BackgroundExample
Start
Show Background beachBackground
Nagatoro says Nice view!
End
```

### Clear Background Node
Clears the current background:

**Template** (`ClearBackgroundNode.cs.snil`):
```
Clear Background
worker:ClearBackgroundNodeWorker
```

**Usage in SNIL script**:
```
name: ClearBackgroundExample
Start
Show Background beachBackground
Nagatoro says Beautiful place!
Clear Background
End
```

## Multi-Script Support

You can create multiple dialogues in one file using separators:

```
name: FirstDialogue
Start
Nagatoro says Hello!
Jump To SecondDialogue

---

name: SecondDialogue
Start
Player says Hi back!
End
```

## Comments

You can add comments using `//` or `#`:

```
# This is a comment
name: CommentedScript
Start
// This is also a comment
Nagatoro says Hello!
End
```

## Function System

The system supports functions similar to JavaScript, allowing you to group related operations:

### Defining Functions

**Syntax**:
```
function functionName
  // function body with SNIL commands
end
```

**Example**:
```
function greetNagatoro
Nagatoro says Hi there!
Player says Hello Nagatoro!
end
```

### Calling Functions

Use the `call` keyword to execute a function:

**Syntax**:
```
call functionName
```

**Usage in SNIL script**:
```
name: FunctionExample
Start
Show Background beachBackground
call greetNagatoro
Jump To nextDialogue
end

function greetNagatoro
Nagatoro says Hi there!
Player says Hello Nagatoro!
end
```

### Function Validation

The system validates function syntax:
- Functions must have a name
- Functions must be properly closed with `end`
- Nested functions are not allowed
- Each `end` must match a `function`

## Resource Finding System

The system includes a universal resource finder that can locate any type of asset by name or path:

- **By name**: Just specify the filename without extension (e.g., `beachBackground`)
- **By path**: Specify the full path from Assets folder (e.g., `SNEngine/Demo/Sprites/beachBackground.png`)
- **Type checking**: The system verifies that the found asset is of the correct type
- **Duplicate handling**: If multiple assets have the same name, an error is shown with available paths

## Validation

The system validates scripts before import and provides detailed error messages:
- Line number where error occurred
- Type of error
- Content of the problematic line
- Clear error message

Example error message:
```
Line 3: UnknownNode - Unknown node format: 'InvalidCommand says Hello!' (Content: 'InvalidCommand says Hello!')
```

## Import Methods

### Import Single Script
- Menu: `SNEngine/Import SNIL Script`
- Allows importing a single `.snil` file

### Import Folder
- Menu: `SNEngine/Import SNIL Folder`
- Imports all `.snil` files from a selected folder

### Import Window
- Menu: `SNEngine/SNIL Importer`
- Provides a GUI interface for importing scripts with options

## Best Practices

1. **Parameter Naming**: Use descriptive parameter names that match your node's field names
2. **Worker Classes**: Create custom worker classes for complex parameter handling
3. **Template Consistency**: Keep template format consistent with your node's functionality
4. **Validation**: Test your templates with various parameter values to ensure proper validation
5. **Documentation**: Document your node's parameters and expected values
6. **Error Handling**: Implement proper error handling in your custom nodes
7. **Performance**: Consider performance implications when creating complex nodes
8. **Testing**: Test your scripts with various inputs to ensure robustness

## Example: Complete Custom Node

Here's a complete example of a WaitNode:

**Unity Node Class** (`WaitNode.cs`):
```csharp
using SiphoinUnityHelpers.XNodeExtensions;
using UnityEngine;

public class WaitNode : AsyncNode
{
    [SerializeField] private float _waitTime = 1.0f;

    public override void Execute()
    {
        // Wait for specified time
        StartCoroutine(WaitCoroutine());
    }

    private IEnumerator WaitCoroutine()
    {
        yield return new WaitForSeconds(_waitTime);
        Continue();
    }
}
```

**Template File** (`WaitNode.cs.snil`):
```
Wait {_waitTime} seconds
worker:WaitNodeWorker
```

**Worker Class** (`WaitNodeWorker.cs`):
```csharp
using System.Collections.Generic;
using SiphoinUnityHelpers.XNodeExtensions;
using SNEngine.Editor.SNILSystem.Workers;

public class WaitNodeWorker : SNILWorker
{
    public override void ApplyParameters(BaseNode node, Dictionary<string, string> parameters)
    {
        if (parameters.ContainsKey("waitTime"))
        {
            var field = node.GetType().GetField("_waitTime");
            if (field != null)
            {
                if (float.TryParse(parameters["waitTime"], out float value))
                {
                    field.SetValue(node, value);
                }
            }
        }
    }
}
```

**Usage in SNIL script**:
```
name: TimingExample
Start
Nagatoro says Wait for it...
Wait 3.5 seconds
Nagatoro says And there it is!
End
```

## Troubleshooting

### Common Issues

- **Node not appearing in graph**: Check that:
  - The node class inherits from a proper base class
  - The template file exists in the correct location
  - The template file name matches the class name
  - The class is in the correct namespace

- **Parameters not set correctly**: Verify that:
  - Parameter names in the template match field names in the class
  - Your worker class (if used) handles the parameter assignment correctly
  - Ensure the field is marked with `[SerializeField]` if using Unity's serialization

- **Resource not found**: Make sure:
  - The asset exists in the project
  - The name/path is correct
  - The asset type matches what's expected

- **Import errors**: Check:
  - Syntax is correct in your script
  - All referenced nodes have templates
  - No duplicate dialogue names in multi-script files

### Debugging Tips

- Enable Unity's console to see detailed error messages
- Use the validation system to check scripts before import
- Check the generated dialogue assets in the Resources folder
- Use the import window for better error feedback

## Advanced Features

### Template Matching

The system uses regex-based template matching to identify node types in scripts. Templates can include:
- Literal text that must match exactly
- Parameter placeholders in `{}` or `[]`
- Special directives like `worker:WorkerClassName`

### Post-Processing System

The system includes a post-processing step that:
- Resolves cross-dialogue references (Jump To nodes)
- Sets up connections between nodes
- Validates the final graph structure

### Custom Validation

You can create custom validation rules by extending the `SNILValidator` class and implementing your own validation logic.

## File Formats

### SNIL Files (.snil)
- Text-based files containing dialogue scripts
- UTF-8 encoded
- Support comments with `//` or `#`
- Use `---` as multi-script separators

### Generated Assets (.asset)
- Unity ScriptableObjects containing dialogue graphs
- Automatically saved to `Assets/SNEngine/Source/SNEngine/Resources/Dialogues/`
- Can be referenced from other scripts and scenes