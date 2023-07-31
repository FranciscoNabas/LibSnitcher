using System;
using System.Reflection;
using System.Collections.Generic;

#nullable enable

namespace LibSnitcher
{
    public class LibraryInfo
    {
        public string Name { get; }
        public bool IsLoaded { get; set; }
        public bool IsClr { get; set; }
        public string? Parent { get; set; }
        public string? Path { get; set; }
        public Assembly? ClrAssembly { get; set; }
        public Exception? LoadException { get; set; }
        public List<LibraryInfo> ImportList { get; }
        public List<LibraryInfo> DelayLoadList { get; }
        public List<LibraryInfo> ClrReferencedAssemblies { get; }

        public LibraryInfo(string name)
        {
            Name = name;
            ImportList = new();
            DelayLoadList = new();
            ClrReferencedAssemblies = new();
            IsLoaded = false;
            IsClr = false;
        }
    }
}