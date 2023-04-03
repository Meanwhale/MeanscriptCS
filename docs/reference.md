
## Script syntax quick reference

<pre>// basic variable types:
int a                       // define a 32-bit integer 'a' with a default value (0)
int a: 5                    // define it with an initial value of 5
float f: 123.456            // define a 32-bit floating-point number
int64 i64: 1234567891234;   // define a 64-bit integer
float64 f64: 12.123456789;  // define a 64-bit floating-point number
text name: "Jack"           // define an immutable string
chars [12] ch: "Jill"       // define a mutable, fixed-sized string (max. 12 bytes)
bool b: true                // define a boolean ('true' or 'false')

// basic functions: coming soon...
<!--
// Function calls can be of two formats:
// 1. Argument list separated with spaces, like on command line:
//        function arg1 arg2 ...
// 2. Argument list in brackets, separated with commands:
//        function ( arg1 , arg2 , ... )
// If an argument is a function call (return value), it's in brackets:
//        1. function_a arg_a1 (function_b arg_b1 arg_b2 ...) arg_a3 ...
//        2. function_a (arg_a1, ( function_b ( arg_b1, arg_b2, ... ) ) , arg_a3 , ... )
a + b                   // return a+b
print a                 // prints an integer 
-->
// define a struct with two members: name and id
struct person [text name, int id]     
person p                 // define struct variable
person p2: "John", 5432  // define struct variable with initial values
p.name: "Jack"           // assign struct variable member

// array
array [person, 4] team   // define an array of size four
team[0].name: "Jane"     // modify the first 'person'
<!--
// define a function that returns a value
func int increase [int foo] { return (sum foo 1) }
-->
// map contains key-value pairs, where key is a string and
// value can be any numeric, boolean, or text type or another map.
map m {
  key1: "value";
  key2: 123
  key3: true

  childMap {
    key: "value"
  }
}
</pre>

### String format

Text strings are in UTF-8 format. Text's byte values can be defined with `\xHH`, where `HH` is a two-digit hexadecimal value, eg.
<pre>
text s: "Hello \xD1\xbeorld!"    // "Hello Ѿorld!"
</pre>

Other supported escape sequences:
<pre>
\'    single quote            byte 0x27 in ASCII encoding
\"    double quote            byte 0x22 in ASCII encoding
\?    question mark           byte 0x3f in ASCII encoding
\\    backslash               byte 0x5c in ASCII encoding
\a    audible bell            byte 0x07 in ASCII encoding
\b    backspace               byte 0x08 in ASCII encoding
\f    form feed - new page    byte 0x0c in ASCII encoding
\n    line feed - new line    byte 0x0a in ASCII encoding
\r    carriage return         byte 0x0d in ASCII encoding
\t    horizontal tab          byte 0x09 in ASCII encoding
\v    vertical tab            byte 0x0b in ASCII encoding
</pre>

## Bytecode format

Meanscript bytecode is a list of 32-bit words. One bytecode word can be an instruction or data.

32-bit instruction content:

bits  | mask         | description
------|--------------|------------
0-7   | `0xff000000` | Instruction ID.
8-15  | `0x00ff0000` | Instruction size.
16-31 | `0x0000ffff` | Additional info, e.g. data type of the instruction target.

For example, here's an instruction to define a text "Meanscript" constant:

<pre>0:   0x10040002      Text adding operation (hex)
1:   10              Number of characters
2:   1851876685      Text characters in ASCII codes
3:   1769104243
4:   29808</pre>

The instruction code above is `0x10` for adding a constant text.
The instruction size is `0x04` i.e. four 32-bit words (excluding the instruction).
The data type is `0x0004`, a constant text.
After the instruction word comes data for the instruction.
The first word (int) tells the text length,
and the following words contain text characters in ASCII codes.
