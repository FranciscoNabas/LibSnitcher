# LibSnitcher

**LibSnitcher** is a PowerShell module designed to retrieve module dependency information.
It easily, and quickly returns the module's entire dependency chain, including if a given
module loaded successfully. You can also find which modules didn't load, or bring the whole PE headers.  
  
This module was designed to work with **Portable Executables**. It's also prepared to work with
.NET assemblies. It works by loading the module, reading its **PE** information, and retrieving
referenced assemblies, if applicable.  
  
## Installation

This module is available at the [PowerShell Gallery](https://www.powershellgallery.com/packages/WindowsUtils).

```powershell
Install-Module -Name 'LibSnitcher'
Import-Module -Name 'LibSnitcher'
```
  
If you clone the repository, run `\Tools\Build.ps1`, and the output should be at `\Release\LibSnitcher`

```powershell
Set-Location "$env:SystemDrive\LibSnitcher"
& '.\Tools\Build.ps1'

Import-Module '.\Release\LibSnitcher\LibSnitcher.psd1'
```

## Cmdlets

### Get-PeDependencyChain

This command lists the module's dependency chain. Due to the nature of module dependencies, if a module
appears more than once, its dependency chain is brought only once. Subsequent appearances show only
module information.  
You can also bring unique module information by using the `-Unique` parameter. This will only bring
the first occurrence of a module.  
Attention! Using the `-Unique` parameter might not return all dependencies for a given module.  
  
The `-Path` parameter accepts a file path, module name, or .NET fully qualified assembly name.

```powershell
Get-PeDependencyChain -Path 'C:\Windows\System32\kernel32.dll'
Get-PeDependencyChain -Path 'System.Private.CoreLib, Version=8.0.0.0, Culture=neutral'
Get-PeDependencyChain -Name 'explorer.exe' -Unique
```

### Get-PeFailedDependency

This command lists the dependencies of a given module that failed to load. The `-Path` parameter works
like the previous command, with file path, module name, or .NET fully qualified name.  
The `-ClrOnly` parameter returns only .NET assemblies that failed to load.  

```powershell
Get-PeFailedDependency -Path 'C:\repos\MyAwesomeProject\MyAwesomeLibrary.dll'
Get-PeFailedDependency -Path 'MyAwesomeLibrary, Version=0.0.0.0, Culture=neutral' -ClrOnly
```

### Get-PeHeaders

This command returns information from the portable executable headers. Although implemented
differently, it mimics the `System.Reflection.PortableExecutable.PEHeaders`, from .NET Core.  
The object does not include the `DOS header`.  
The `-Path` parameter must contain a file path to a valid portable executable.  

```powershell
Get-PeHeaders -Path 'C:\Windows\System32\ntdll.dll'
```
  
## Credit
  
This project draws inspiration from the great [Dependencies][01].  
The documentation used is available in the Microsoft docs:  
  
[PE file format][02]  
[The 'Assembly' class][03]  
[The 'PortableExecutable' namespace][04]  

<!-- Link definition -->

[01]: https://github.com/lucasg/Dependencies
[02]: https://learn.microsoft.com/windows/win32/debug/pe-format
[03]: https://learn.microsoft.com/dotnet/api/system.reflection.assembly
[04]: https://learn.microsoft.com/en-us/dotnet/api/system.reflection.portableexecutable?view=net-7.0
