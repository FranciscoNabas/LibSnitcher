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

        public void GetDependencyChainList(string lib_name, bool unique)
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
            buffer.Append($"{module.Name} (Loaded: {module.Loaded}): {module.PostfixText}");

            // buffer.Append($"{module.Name} <{module.Depth}> (Loaded: {module.Loaded}; Parent: {module.Parent}): {module.Path}");
            // buffer.Append($"{module.Name} (Id: {module.Id};Loaded: {module.Loaded}; Parent Id: {module.ParentId};Parent: {module.Parent}): {module.Path}");

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

        internal Module GetModule(Guid parent_id, string name, string parent, DependencySource source, int depth, out bool is_trivial)
        {
            if (_instance._result.TryGetValue(name, out Module module))
            {
                is_trivial = true;
                return module.TrivialCopy(depth, parent, parent_id);
            }
            is_trivial = false;

            LibInfo info = _unwrapper.GetDependencyList(name, source);
            Module new_module = new(Guid.NewGuid(), parent_id, info.Name, parent, source, depth, info.Path, info.Loaded, info.LoaderError, info.Dependencies, ref _instance);
            _result.Add(name, new_module);

            return new_module;
        }
    }

    internal class Module
    {
        private readonly DependencyChain _chain;
        private readonly DependencyEntry[] _native_dependencies;

        internal Guid Id { get; private set; }
        internal Guid ParentId { get; private set; }

        internal string Name { get; private set; }
        internal string Parent { get; private set; }
        internal DependencySource Source { get; }
        internal int Depth { get; private set; }
        internal string Path { get; set; }
        internal bool Loaded { get; set; }
        internal Exception LoaderException { get; set; }

        internal List<Module> Dependencies { get; private set; }

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

        internal Module(Guid id, Guid parent_id, string name, string parent, DependencySource source, 
            int depth, string path, bool loaded, Exception loader_exception, DependencyEntry[] native_dependencies, ref DependencyChain chain)
        {
            _chain = chain;
            if (native_dependencies is not null)
                _native_dependencies = native_dependencies;
            else
                _native_dependencies = new DependencyEntry[0];

            Id = id;
            ParentId = parent_id;
            Name = name;
            Parent = parent;
            Source = source;
            Depth = depth;
            Path = path;
            Loaded = loaded;
            LoaderException = loader_exception;
            Dependencies = new();
        }

        internal Module TrivialCopy(int depth, string parent, Guid parent_id)
        {
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