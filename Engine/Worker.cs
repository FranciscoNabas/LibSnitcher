using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Management.Automation;
using LibSnitcher.Core;

namespace LibSnitcher
{
    public class Helper
    {
        private readonly PSCmdlet _context;
        private readonly HashSet<Guid> _printed;

        public Helper(PSCmdlet context)
        {
            _context = context;
            _printed = new();
        }

        public List<Module> GetDependencyChainList(string lib_name, bool unique)
        {
            DependencyChain factory = DependencyChain.GetChain(unique);
            List<Module> chain = factory.ResolveDependencyChain(lib_name);
            
            factory.Dispose();

            return chain;
        }

        public void PrintModuleDependencyChain(string lib_name, bool unique)
        {
            DependencyChain factory = DependencyChain.GetChain(unique);
            List<Module> chain = factory.ResolveDependencyChain(lib_name);
            GetTextListFromModuleList(chain.First(m => m.Depth == 0));

            factory.Dispose();
        }

        private void GetTextListFromModuleList(Module root)
        {
            if (_printed.Contains(root.Id))
                return;

            PrintWork(root);
            _printed.Add(root.Id);

            foreach (Module dependency in root.Dependencies)
                GetTextListFromModuleList(dependency);
        }

        private void PrintWork(Module module)
        {
            StringBuilder buffer = new();
            buffer.Append(' ', module.Depth * 2);
            buffer.Append($"{module.AbsoluteName} (Loaded: {module.Loaded}): {module.PostfixText}");

            // buffer.Append($"{module.AbsoluteName} <{module.Depth}> (Loaded: {module.Loaded}; Parent: {module.Parent}): {module.Path}");
            // buffer.Append($"{module.AbsoluteName} (Id: {module.Id};Loaded: {module.Loaded}; Parent Id: {module.ParentId};Parent: {module.Parent}): {module.Path}");

            _context.WriteObject(buffer.ToString());
        }
    }

    internal class DependencyChain : IDisposable
    {
        private bool _unique;
        private readonly Wrapper _unwrapper;
        private static DependencyChain _instance;
        private readonly Dictionary<string, Module> _result;

        internal bool Unique { get { return _instance._unique; } }

        private DependencyChain()
        {
            _result = new();
            _unwrapper = new();
        }

        public void Dispose()
        {
            // Being static, the instance only gets collected when its app domain dies.
            // If we call the Cmdlet again with different parameters, we will be using the
            // previous instance. Setting it to null makes it available to the GC.
            _instance = null;
            GC.Collect();
        }

        internal static DependencyChain GetChain(bool unique)
        {
            _instance ??= new();
            _instance._unique = unique;
            return _instance;
        }

        internal List<Module> ResolveDependencyChain(string module_name)
        {
            Module root = GetModule(Guid.Empty, module_name, string.Empty, DependencySource.None, 0, out _);
            root.ResolveDependencies();
            return _instance._result.Values.ToList();
        }

        internal Module GetModule(Guid parent_id, string name, string parent, DependencySource source, int new_depth, out bool is_trivial)
        {
            if (_instance._result.TryGetValue(name, out Module module))
            {
                is_trivial = true;
                return module.TrivialCopy(new_depth, parent, parent_id);
            }
            is_trivial = false;

            ModuleBase base_module = _unwrapper.GetDependencyList(name, source);
            Module new_module = new(parent_id, parent, source, new_depth, base_module, ref _instance);
            _result.Add(name, new_module);

            return new_module;
        }

        internal static void PrintLocation(string location)
        {
            // Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(location);
        }
    }

    public class Module
    {
        private readonly DependencyChain _chain;
        private readonly List<DependencyEntry> _native_dependencies;

        internal Guid Id { get; private set; }
        internal Guid ParentId { get; private set; }

        public string Name { get; }
        public string AssemblyFullName { get; }
        public string Parent { get; private set; }
        public DependencySource Source { get; }
        internal int Depth { get; private set; }
        public string Path { get; }
        public bool IsClr { get; }
        public bool Loaded { get; }
        public Exception LoaderException { get; }

        public List<Module> Dependencies { get; private set; }

        internal string PostfixText
        {
            get
            {
                if (Loaded)
                    return Path;
                else
                    if (LoaderException is not null)
                    return LoaderException.Message;

                return string.Empty;
            }
        }

        internal string AbsoluteName
        {
            get
            {
                if (IsClr)
                    if (!string.IsNullOrEmpty(AssemblyFullName))
                        return AssemblyFullName;

                return Name;
            }
        }

        internal Module(Guid parent_id, string parent, DependencySource source, int depth, ModuleBase base_module, ref DependencyChain chain)
        {
            Id = Guid.NewGuid();
            ParentId = parent_id;
            Parent = parent;
            Source = source;
            Depth = depth;

            Name = base_module.Name;
            Path = base_module.Path;
            AssemblyFullName = base_module.AssemblyFullName;
            Loaded = base_module.Loaded;
            LoaderException = base_module.LoaderException;

            Dependencies = new();
            _native_dependencies = base_module.Dependencies;
            _chain = chain;
        }

        internal Module TrivialCopy(int depth, string parent, Guid parent_id)
        {
            // DependencyChain.PrintLocation("Module.TrivialCopy");
            Module new_module = (Module)this.MemberwiseClone();

            new_module.Depth = depth;
            new_module.Parent = parent;
            new_module.ParentId = parent_id;
            new_module.Dependencies = new();

            if (!_chain.Unique)
                new_module.Id = Guid.NewGuid();

            return new_module;
        }

        internal void ResolveDependencies()
        {
            // DependencyChain.PrintLocation("Module.ResolveDependencies");
            List<Module> new_dependencies = new();
            foreach (DependencyEntry entry in _native_dependencies)
            {
                Module dependency = _chain.GetModule(Id, entry.Name, Name, entry.Source, Depth + 1, out bool is_trivial);
                if (is_trivial)
                {
                    Dependencies.Add(dependency);
                    continue;
                }

                Dependencies.Add(dependency);
                new_dependencies.Add(dependency);
            }

            foreach (Module dependency in new_dependencies)
                dependency.ResolveDependencies();
        }
    }
}