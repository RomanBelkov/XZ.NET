# XZ.NET

**XZ.NET is a .NET wrapper for liblzma.dll**

## License ##
Licensed under MIT license. See LICENSE, AUTHORS.

This project uses a public domain compression library liblzma from XZ Utils and build tools for Windows (MinGW-w64), which were used by creators of XZ Utils. See LICENSE-Notices for information.

## Description ##

The intentions of this library is to provide basic operations with `.xz` file format to .NET (C#, F#, VB) developers.

*Please note that library is in the early stages of development and many features are missing. Also, bugs may be present. The library was tested with `i686-sse2`  and `x86-64` version of liblzma 5.2.1 (these versions are included in project under names liblzma.dll and liblzma64.dll).*

**You can find some basic examples in 'Examples' folder of project.**

## ChangeLog ##

09/09/2015 - 1.2.0

- Compression part of library should work fine from now on

16/06/2015 - 1.1.0

- Switched the license to MIT

11/05/2015

- Added x86-64 liblzma support and conditional compilation

11/03/2015 
What's new:

- compressing data
- retargeted to 2.0 framework and now using generics instead of LINQ
- now using 5.2.1 version of liblzma

18/01/2015 - 1.0

Initial commit. Library can be used for: 

- getting uncompressed stream from `.xz` archive 
- getting the size of uncompressed file


## Contact ##

Roman Belkov - romanbelkov@gmail.com

Kirill Melentyev - melentyev.k@gmail.com 