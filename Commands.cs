using System;
using System.Management.Automation;

namespace LibSnitcher.Commands
{
    [Cmdlet(VerbsCommon.Get, "LibInfo")]
    public class PublishLibInfoCommand : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Path { get; set; }

        [Parameter(Mandatory = true)]
        public int ThreadCount { get; set; }

        protected override void ProcessRecord()
        {
            Helper helper = new(this);
            helper.GetDependencyChainList(Path, ThreadCount);
        }
    }
}