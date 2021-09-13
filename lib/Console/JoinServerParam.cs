using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.lib.Console
{

    [Verb("join", HelpText = "join server")]
    public class JoinServerParam
    {
        [Option('i', "ipadress", Required = true, HelpText = "ipadress of server")]
        public string ServerIpAdress { get; set; }

        [Option('p', "serverport", Required = false, HelpText = "serverport")]
        public int ServerPort { get; set; }
    }
}
