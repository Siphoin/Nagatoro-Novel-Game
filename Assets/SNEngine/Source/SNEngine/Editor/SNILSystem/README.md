# SNIL System Documentation

## Overview

The SNIL (Script Novel Intermediate Language) system is a powerful, modular text-based scripting solution for creating visual novel dialogues in Unity. It allows developers to define complex dialogue flows using simple text-based scripts that are automatically converted into node-based dialogue graphs through a plugin-based architecture.

## Key Features

- **Modular Architecture**: Plugin-based instruction handling system with dedicated validators and processors
- **Text-based dialogue scripting**: Write dialogues using simple, readable text files
- **Node-based execution**: Automatically converts text scripts to Unity node graphs
- **Flexible parameter system**: Supports complex parameter passing to nodes
- **Resource management**: Built-in system for handling characters, backgrounds, and assets
- **Multi-script support**: Create multiple dialogues in a single file
- **Function system**: Reusable code blocks for common operations
- **Comprehensive validation**: Detailed error checking with multiple validation layers
- **Extensible design**: Easy to add new node types and instruction handlers

## Architecture

The SNIL system follows a modular, plugin-based architecture:

### Core Components

#### 1. Instruction Handler System
The system uses a dynamic instruction handler architecture:
- `IInstructionHandler` interface for all instruction handlers
- `InstructionHandlerManager` for registering and managing handlers
- Dedicated handlers for different instruction types:
  - `NameInstructionHandler` - handles `name:` directives
  - `StartInstructionHandler` - creates Start nodes
  - `EndInstructionHandler` - creates End nodes
  - `FunctionDefinitionInstructionHandler` - handles function definitions
  - `FunctionEndInstructionHandler` - handles function endings
  - `CallInstructionHandler` - handles function calls
  - `GenericNodeInstructionHandler` - handles template-based nodes

#### 2. Validation System
Multi-layered validation architecture:
- `SNILSyntaxValidator` - main validation coordinator
- `EmptyFileValidator` - checks for empty/null files
- `NameDirectiveValidator` - validates `name:` directive and structure
- `FunctionValidator` - validates function definitions and structure
- `InstructionValidator` - validates individual instructions against templates
- `InstructionValidatorManager` - manages specialized validators

#### 3. Compilation System
- `SNILInstructionBasedCompiler` - main compilation engine
- `SNILCompiler` - high-level interface
- Modular compilation with error propagation
- Validation occurs before processing

#### 4. Node Connection System
- `NodeConnectionUtility` - handles sequential node connections
- Automatic `_enter` and `_exit` port connections
- Proper function body connection through `_operations` port

### File Organization

```
SNILSystem/
├── FunctionSystem/           # Function parsing and handling
├── Importers/               # Script import logic
│   ├── SNILScriptImporter.cs
│   ├── SNILGraphCreator.cs
│   ├── SNILScriptProcessor.cs
│   └── SNILScriptValidator.cs
├── InstructionHandlers/     # Instruction handling system
│   ├── IInstructionHandler.cs
│   ├── BaseInstructionHandler.cs
│   ├── NameInstructionHandler.cs
│   ├── StartInstructionHandler.cs
│   ├── EndInstructionHandler.cs
│   ├── FunctionDefinitionInstructionHandler.cs
│   ├── FunctionEndInstructionHandler.cs
│   ├── CallInstructionHandler.cs
│   ├── GenericNodeInstructionHandler.cs
│   ├── FunctionBodyCreator.cs
│   └── InstructionHandlerManager.cs
├── NodeCreation/            # Node creation logic
│   ├── NodeCreator.cs
│   ├── NodePositioner.cs
│   ├── NodeConnector.cs
│   └── NodeFormatter.cs
├── Parsers/                 # Script parsing
├── ResourceFinder/          # Asset finding utilities
├── Utilities/              # General utilities
│   ├── SNILParameterApplier.cs
│   ├── SNILTypeResolver.cs
│   ├── SNILTemplateMatcher.cs
│   └── SNILTemplateManager.cs
├── Validators/             # Validation system
│   ├── IInstructionValidator.cs
│   ├── BaseInstructionValidator.cs
│   ├── TemplateBasedInstructionValidator.cs
│   ├── InstructionValidatorManager.cs
│   ├── EmptyFileValidator.cs
│   ├── NameDirectiveValidator.cs
│   ├── FunctionValidator.cs
│   ├── InstructionValidator.cs
│   ├── ScriptLineExtractor.cs
│   └── SNILSyntaxValidator.cs
├── Workers/                # Node worker system
├── SNILCompiler.cs         # Main compiler interface
└── SNILInstructionBasedCompiler.cs  # Core compilation engine
```

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

## Adding New Node Types

The system supports easy addition of new node types through the instruction handler architecture:

### Step 1: Create Instruction Handler

Create a new handler that implements `IInstructionHandler`:

```csharp
public class CustomInstructionHandler : BaseInstructionHandler
{
    public override bool CanHandle(string instruction)
    {
        // Check if this handler can process the instruction
        return instruction.StartsWith("CustomCommand", StringComparison.OrdinalIgnoreCase);
    }

    public override InstructionResult Handle(string instruction, InstructionContext context)
    {
        // Create and configure the node
        if (context.Graph == null)
        {
            return InstructionResult.Error("Graph not initialized.");
        }

        var dialogueGraph = (DialogueGraph)context.Graph;
        var nodeType = SNILTypeResolver.GetNodeType("CustomNode");
        
        if (nodeType != null)
        {
            var node = dialogueGraph.AddNode(nodeType) as BaseNode;
            if (node != null)
            {
                // Configure the node
                node.name = "Custom";
                node.position = new Vector2(context.Nodes.Count * 250, 0);

                // Apply parameters
                var parameters = ParseParameters(instruction);
                SNILParameterApplier.ApplyParametersToNode(node, parameters, "CustomNode");

                // Add to context
                context.Nodes.Add(node);
                NodeConnectionUtility.ConnectNodeToLast(dialogueGraph, node, context);

                return InstructionResult.Ok(node);
            }
        }

        return InstructionResult.Error("Failed to create CustomNode.");
    }
}
```

### Step 2: Register the Handler

Register your handler with the manager:

```csharp
// In InstructionHandlerManager.RegisterDefaultHandlers()
RegisterHandler(new CustomInstructionHandler());
```

### Step 3: Use Your New Node

Now you can use your new instruction in SNIL scripts:

```
name: MyDialogue
Start
CustomCommand parameter1=value1 parameter2=value2
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

## Function System

The system supports functions with proper body creation and connection:

### Defining Functions
```
function functionName
// function body with SNIL commands
end
```

### Calling Functions
```
call functionName
```

### Function Body Processing
- Functions create `GroupCallsNode` for the function call
- Function bodies are created as connected nodes within the function
- Proper Y-axis positioning above main flow
- Connection through `_operations` port to function body

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

## Validation Architecture

The system uses a multi-layered validation approach:

1. **Empty File Validation**: Checks for null/empty input
2. **Name Directive Validation**: Ensures proper script structure
3. **Function Validation**: Validates function definitions
4. **Instruction Validation**: Validates individual instructions against templates
5. **Template-Based Validation**: Checks against registered node templates

Each validation layer can fail independently, stopping the import process and providing detailed error messages.

## Error Handling

The system provides comprehensive error handling:
- Detailed error messages with line numbers
- Multiple error reporting (doesn't stop at first error)
- Proper error propagation through the compilation pipeline
- Clear distinction between validation and runtime errors

## Import Process Flow

1. **Validation Phase**: All validators check the script
2. **Function Registration**: All functions are registered before processing
3. **Instruction Processing**: Each instruction is handled by appropriate handler
4. **Node Connection**: Sequential connections are established
5. **Post-Processing**: Cross-references are resolved

## Best Practices

1. **Handler Design**: Create focused handlers for specific instruction types
2. **Validation**: Implement proper validation before processing
3. **Error Reporting**: Provide clear, actionable error messages
4. **Modularity**: Keep handlers small and focused
5. **Testing**: Validate handlers with various input scenarios
6. **Documentation**: Document new instruction formats clearly

## Extensibility

The system is designed for easy extensibility:
- New instruction handlers can be added without modifying core code
- Validators can be extended for domain-specific validation
- The context system allows for state management across handlers
- Template-based validation supports new node types seamlessly

## Troubleshooting

### Common Issues

- **Instruction not recognized**: Check if appropriate handler is registered
- **Validation errors**: Verify script structure and syntax
- **Node connection issues**: Ensure proper context management
- **Function body not created**: Verify function definition syntax

### Debugging Tips

- Enable detailed logging for instruction processing
- Check handler registration in `InstructionHandlerManager`
- Verify template files exist for template-based nodes
- Use validation separately to isolate issues