using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Management.Automation;

namespace LibSnitcher.Commands
{
    /// <summary>
    /// <para type="synopsis">Returns the module's dependency chain.</para>
    /// <para type="description">This Cmdlet returns the dependency chain for a given module.</para>
    /// <para type="description">The input can be the path, name from a portable executable or a .NET fully qualified assembly name.</para>
    /// <para type="description">Due the nature of cyclic dependencies, the command only resolves the dependencies for the first module's appearance.</para>
    /// <para type="description">To return unique entries only, use the "Unique" parameter.</para>
    /// <example>
    ///     <para></para>
    ///     <code>Get-PeDependencyChain -Path 'C:\Windows\explorer.exe'</code>
    ///     <para>Returning the dependency chain from 'explorer.exe'.</para>
    ///     <para></para>
    /// </example>
    /// <example>
    ///     <para></para>
    ///     <code>Get-PeDependencyChain -Name 'mscorlib, Version=4.0.0.0, Culture=neutral' -Unique</code>
    ///     <para>Returning the dependency chain for 'mscorlib.dll', using an assembly qualified name. Unique values only.</para>
    ///     <para></para>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PeDependencyChain")]
    [Alias("getdepchain")]
    public class GetModuleDependencyChainCommand : PSCmdlet
    {
        /// <summary>
        /// <para type="description">The path, name for a portable executable, or .NET assembly fully qualified name.</para>
        /// <para type="description">The Cmdlet will resolve by attempting to load the module.</para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0)]
        [Alias("Name")]
        public string Path { get; set; }

        /// <summary>
        /// <para type="description">Use this parameter to return only unique entries.</para>
        /// <para type="description">Attention, despite returning all dependencies for the root module it might not return all dependencies for a child module.</para>
        /// </summary>
        [Parameter()]
        public SwitchParameter Unique { get; set; }

        /// <summary>
        /// <para type="description">The maximum recursion depth.</para>
        /// <para type="description">Depth 1 returns only the dependencies for the main module.</para>
        /// </summary>
        [Parameter()]
        [ValidateRange(0, int.MaxValue)]
        public int Depth { get; set; } = 0;

        protected override void ProcessRecord()
        {
            Helper helper = new(this);
            helper.PrintModuleDependencyChain(Path, Unique, Depth);
        }
    }

    /// <summary>
    /// <para type="synopsis">Returns all dependencies that failed to load.</para>
    /// <para type="description">This Cmdlet returns all dependencies from a given module that failed to load.</para>
    /// <para type="description">The input can be the path, name from a portable executable or a .NET fully qualified assembly name.</para>
    /// <para type="description">Since it failed to load, the command does not return the path.</para>
    /// <example>
    ///     <para></para>
    ///     <code>Get-PeFailedDependency -Path 'C:\Program Files\PowerShell\7-preview\System.Security.Cryptography.dll'</code>
    ///     <para>Returning all dependencies from 'System.Security.Cryptography.dll' that failed to load.</para>
    ///     <para></para>
    /// </example>
    /// <example>
    ///     <para></para>
    ///     <code>Get-PeFailedDependency -Name 'System.Threading, Version=8.0.0.0, Culture=neutral' -ClrOnly</code>
    ///     <para>Returning only .NET dependencies from 'System.Threading' that failed to load.</para>
    ///     <para></para>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PeFailedDependency")]
    [Alias("getfaildep")]
    public class GetModuleFailedDependencyCommand : PSCmdlet
    {
        /// <summary>
        /// <para type="description">The path, name for a portable executable, or .NET assembly fully qualified name.</para>
        /// <para type="description">The Cmdlet will resolve by attempting to load the module.</para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0)]
        [Alias("Name")]
        public string Path { get; set; }

        /// <summary>
        /// <para type="description">Use this parameter to return only dependencies that are .NET assemblies.</para>
        /// </summary>
        [Parameter()]
        public SwitchParameter ClrOnly { get; set; }

        protected override void BeginProcessing()
        {
            if (!File.Exists(Path))
                throw new FileNotFoundException($"File '{Path}' not found.");
        }

        protected override void ProcessRecord()
        {
            Helper helper = new(this);
            List<Module> dep_chain = helper.GetDependencyChainList(Path, true, 0);
            if (dep_chain is not null)
                if (ClrOnly)
                    WriteObject(dep_chain.Where(m => !m.Loaded && m.IsClr), true);
                else
                    WriteObject(dep_chain.Where(m => !m.Loaded), true);
        }
    }

    /// <summary>
    /// <para type="synopsis">Returns the Portable Executable headers.</para>
    /// <para type="description">This Cmdlet returns the PE headers for a given portable executable.</para>
    /// <para type="description">Although implemented differently, it mimics the 'System.Reflection.PortableExecutable' from .NET core.</para>
    /// <para type="description">You can achieve the same result by creating a new 'LibSnitcher.PortableExecutable' object.</para>
    /// <example>
    ///     <para></para>
    ///     <code>Get-PeHeaders -Path "$env:SystemRoot\System32\kernel32.dll"</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PeHeaders")]
    [OutputType(typeof(PortableExecutable))]
    public class GetPeHeadersCommand : PSCmdlet
    {
        private string _path;

        /// <summary>
        /// <para type="description">The path to a portable executable.</para>
        /// </summary>
        [Parameter(Mandatory = true)]
        public string Path
        {
            get { return _path; }
            set
            {
                if (!File.Exists(value))
                    throw new FileNotFoundException($"Could not find file '{value}'.");

                _path = value;
            }
        }

        protected override void ProcessRecord()
        {
            WriteObject(new PortableExecutable(Path));
        }
    }
}