// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#ifndef PCH_H
#define PCH_H
#define WIN32_LEAN_AND_MEAN
#define _WIN32_WINNT 0x0600

// To avoid warning C4005. This definition is done via compiler command line.
#undef __CLR_VER

#ifndef UNICODE
#define UNICODE
#define _UNICODE
#endif // UNICODE

#pragma comment(lib, "kernel32.lib")
#pragma comment(lib, "Imagehlp.lib")
#pragma comment(lib, "Shlwapi.lib")
#pragma comment(lib, "Pathcch.lib")

#include <map>
#include <vector>
#include <memory>
#include <xstring>
#include <Windows.h>
#include <Pathcch.h>
#include <Shlwapi.h>
#include <ImageHlp.h>

#pragma unmanaged

#include "String.h"

#endif //PCH_H
