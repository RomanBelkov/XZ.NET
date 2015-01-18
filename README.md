# XZ.NET

XZ.NET is a .NET wrapper for liblzma.dll

## License ##
Licensed under GNU General Public License 3.0 or later. Some rights reserved. See LICENSE, AUTHORS.

This project uses a public domain compression library liblzma from XZ Utils and build tools for Windows (MinGW-w64), which were used by creators of XZ Utils. See LICENSE-Notices for information.

## Description ##

The intentions of this library is to provide basic operations with `.xz` file format to .NET developers. 

*Please note that library is in the early stages of development and many features are missing. Also, bugs may be present. The library was tested with `i686-sse2` version of liblzma 5.2.0 (this version is included in project under name liblzma.dll).*

## ChangeLog ##

18/01/2015 - 1.0
Initial commit. Library can be used for: 


- getting uncompressed stream from `.xz` archive 
- getting the size of uncompressed file

You can find some basic examples in 'Examples' folder of project 

## Contact ##

Roman Belkov - romanbelkov@gmail.com

Kirill Melentyev - melentyev.k@gmail.com 