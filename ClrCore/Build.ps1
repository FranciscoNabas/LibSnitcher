$filesStr = ''
ls $PSScriptRoot -File | ? { $_.Name.EndsWith('.h') -or $_.Name.EndsWith('.cpp') } | % { $filesStr += "$($_.Name) " }

cmd.exe 'C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat'
$clArgs = "/W4 /EHsc AssemblyInfo.cpp Common.h Expressions.h MemoryManagement.h pch.cpp pch.h PeHelper.cpp PeHelper.h PortableExecutable.cpp PortableExecutable.h Resource.h String.h Wrapper.cpp Wrapper.h  /Yu""pch.h"" /ifcOutput ""x64\Release\"" /GS /W3 /Zc:wchar_t /Zi /O2 /Fd""x64\Release\vc143.pdb"" /Zc:inline /fp:precise /D ""NDEBUG"" /D ""_WINDLL"" /D ""_UNICODE"" /D ""UNICODE"" /errorReport:prompt /WX- /Zc:forScope /clr /MD /FC /Fa""x64\Release\"" /EHa /nologo /Fo""x64\Release\"" /Fp""x64\Release\ClrCore.pch"" /diagnostics:column /link /out:ClrCore.dll"
cl 
LINK /OUT:"C:\LocalRepositories\LibSnitcher\ClrCore\x64\Release\ClrCore.dll" /MANIFEST /NXCOMPAT /PDB:"C:\LocalRepositories\LibSnitcher\ClrCore\x64\Release\ClrCore.pdb" /DYNAMICBASE /FIXED:NO /DEBUG:FULL /DLL /MACHINE:X64 /PGD:"C:\LocalRepositories\LibSnitcher\ClrCore\x64\Release\ClrCore.pgd" /MANIFESTUAC:"level='asInvoker' uiAccess='false'" /ManifestFile:"x64\Release\ClrCore.dll.intermediate.manifest" /LTCGOUT:"x64\Release\ClrCore.iobj" /ERRORREPORT:PROMPT /ILK:"x64\Release\ClrCore.ilk" /NOLOGO /TLBID:1 
exit

AssemblyInfo.cpp Common.h Expressions.h MemoryManagement.h pch.cpp pch.h PeHelper.cpp PeHelper.h PortableExecutable.cpp PortableExecutable.h Resource.h String.h Wrapper.cpp Wrapper.h 