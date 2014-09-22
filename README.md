Google.PhoneNumbers
===================

Another port of libphonenumber to C#

Why? 
---
There does exists other ports (ie. https://github.com/erezak/libphonenumber-csharp) 
but they're out of date and currently their implementation requires reading 
PhoneNumberMetadata.xml vs. reading an efficient binary format.

Solution Structure
------------------

### Google.PhoneNumbers
This is a port of libphonenumber. The 'Data' folder includes binary files 
which will be embedded into the assembly. The format of the binary files
is something that is readible by BinaryReader.

### Google.PhoneNumbers.GenerateData
This is a port of the libphonenumber/tools to convert the xml data to binary
files and to generate some C# files with static data.

If you just want to generate the data class files you can use the fun batch
script 'StageData.bat'. This will output the data files to the
Google.PhoneNumbers project.

### Google.PhoneNumbers.Test
Port of libphonenumber tests to C# - right now all tests are passing

### Coding Style (it's very unlike C#)
Patches are welcome. Plus it'll be easier to patch over future
revisions if the code base is similar

