using System.IO;
using CommandLine;
using System;

namespace GruntWurk {
    class Options : CommonOptions {
        [Option('f', "file", Required = true, HelpText = "Input file(s) to be scanned. May include wildcards. If no path is given, then the current directory is assumed.")]
        public string InputFilepath { get; set; }

        [Option('s', "spec", Required = false, HelpText = "Name of corresponding specification file. (Default is the input filename with an INI extension.)")]
        public string SpecFilename { get; set; }

        [Option('o', "outfile", Required = false, HelpText = "Name of output file to be saved. (Default is the input filename with an OUT extension.)")]
        public string OutputFilename { get; set; }

        [Option('l', "logfile", Required = false, HelpText = "Name of a log file to be appended to.")]
        public string LogFilename { get; set; }

        public void Validate() {
            if (SpecFilename == null) {
                SpecFilename = Path.ChangeExtension(InputFilepath, "INI");
            }
            if (OutputFilename == null) {
                OutputFilename = Path.ChangeExtension(InputFilepath, "OUT");
            }
        }
        public override string ToString() {
            return string.Format("Input Pathname: {0}\nOutput Filename: {1}\nSpec Filename: {2}\nLog Filename: {3}", InputFilepath, OutputFilename, SpecFilename, LogFilename);
        }
    }
}
