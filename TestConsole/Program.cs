using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibSnitcher;
using LibSnitcher.Core;

namespace TestConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var pe = new PortableExecutable(@"C:\Windows\System32\kernel32.dll");
        }
    }
}
