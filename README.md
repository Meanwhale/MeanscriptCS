# MeanscriptCS
Meanscript serializes data in human-readable script and compact bytecode.

# Featuring

- Scripting language
- Bytecode format
- Basic data types, like integers, floats, and text. Strong typing.
- Data structures, arrays, map (dictionary)
- C# API to read and write data
- GUI

<b>Upcoming features</b>

- Generate C# classes by using data structure (schema) described in Meanscript. That's how you could read and write
data objects directly from your source, like in <a href=https://en.wikipedia.org/wiki/Protocol_Buffers>Protobuf</a>.
- Command line tool
- Functions, conditions, loops, i.e. programming features that already are in the core of Meanscript but are diabled for now due to recent refactorings. 

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
