# MeanscriptCS

Meanscript serializes data in human-readable script and compact bytecode. It's still work-in-progress,
but getting close to be practical tool, once I get some last pieces together.

# Featuring

- Scripting language.
- Bytecode format.
- Basic data types, like integers, floats, and text. Strong typing.
- Data structures, arrays, map (dictionary)
- C# API to read and write data.
- GUI to compile, run, read, and write script and bytecode.

<b>Upcoming features</b>

- Generate C# classes by using data structure (schema) described in Meanscript. That's how you could read and write
data objects directly from your source, like in <a href=https://en.wikipedia.org/wiki/Protocol_Buffers>Protobuf</a>.
- Command line tool.
- Functions, conditions, loops, i.e. programming features that already are in the core of Meanscript but are disabled for now due to recent refactorings. 

# Examples

Create a data structure and a variable in Meanscript. It has two integer members x and y:

```
struct vec [int x, int y]         // create a struct (class) named 'vec'
vec position: 3, 4                // implement the struct in a variable named 'position' with initial values (3, 4)
```

Create an identical data structure and variable using C#:

```cs
var vec = builder.CreateStruct(   // create a struct (class) named 'vec'
  "vec",                          // give struct name
  builder.IntMember("x"),         // define int members
  builder.IntMember("y")
);
var position = builder.New(vec);  // implement the struct in a variable named 'position'
position["x"].SetInt(3);          // initial values (3, 4)
position["y"].SetInt(4);
builder.CreateGlobal(             // add the variable to global variables
    builder.StructMember("position", position)
    // ...
);
```

See [reference](https://github.com/Meanwhale/MeanscriptCS/blob/main/docs/reference.md) for mode information.

Read variable data from C#:

```cs
Print(dataCode.global["position"]["x"].Int()); // prints "3"
```

# How to run it

- Prerequisites: Visual Studio with .NET 6.0.
- Clone this project.
- Go to folder MeanscriptCS/editor.
- Open the MeanscriptEditor.sln fileand run the Editor project. It has two text areas: left for Meanscript code and right for output.
- Write some code:
```
	int a: 3
	print a
```
- Run it from menu Script > Run, or press F5. Uncheck verbose in File menu for less spam. Code above should print "3" to the output area.
- Now that the script is compiled to bytecode, the Bytecode menu is activated.
- Print data description (Bytecode > Print data): it shows the internal data structure, the heap, that consists of dynamic data objects.
- Print bytecode instruction list (Bytecode > Print instructions): result of script compilation, which is a list of binary instructions and their data (in "code/data" column).
- Export data to a Meanbits bytecode file: Bytecode > Export data... The output file is not the same as the compiled bytecode. Instead it exports instructions for only the data.
- You can run the bytecode file from File > Run bytecode file... It runs the data-defining instructions in the bytecode file. The instructions are executed on the fly, while file stream is read, and not saved to memory. That's why you can print data, but not instructions, after running a bytecode file from File menu.
- Save and open script files from File menu.
- Run unit tests from Test menu.


# Design philosophy

- Practical. All-in-one solution for scripting and serializing.
- Minimal core and syntax: small and fast.
- Easy to expand, i.e. add new data types, functions etc.
- Easy to set up, stand-alone. Minimal dependencies to platforms and other software.
- Convenient. Easy to read and write, simple interface, easy to debug, and strong typing and type checking to avoid elusive bugs. Smooth crashing by exception handling, along with informative error messages.
- Iterative, non-blocking bytecode execution.
- Memory control. <a href=https://en.wikipedia.org/wiki/Zero-copy>Zero-copy</a>, when possible.

<!--swiss army knife, “opposite of domain-specific language (DSL)”, the one tool to all data serialization needs in a project.
no need to have multiple tools and languages in addition to source code.
known limitations: not going to overcome things like source code language’s own serialization. Not a tool for executable scripts… yet.

Architecture

komponentit

How to run project
-->

---

<i>Copyright &copy; Meanwhale</i>
