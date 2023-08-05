using System.Management.Automation;

namespace LibSnitcher.Commands
{
    [Cmdlet(VerbsCommon.Get, "ModuleDependencyChain")]
    public class GetModuleDependencyChainCommand : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Path { get; set; }

        [Parameter()]
        public SwitchParameter Unique { get; set; }

        protected override void ProcessRecord()
        {
            Helper helper = new(this);
            helper.GetDependencyChainList(Path, Unique);
        }
    }
}