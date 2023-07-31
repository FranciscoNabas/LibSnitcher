using System;
using System.Management.Automation;

namespace LibSnitcher.Commands
{
    [Cmdlet(VerbsCommon.Get, "LibInfo")]
    public class PublishLibInfoCommand : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            string text = string.Empty;
            Helpers helper = new();
            LibraryInfo info = helper.GetLibraryInfo(Path);
            Helpers.PrintLibraryInfo(info, ref text);
            WriteObject(text);
        }
    }
}