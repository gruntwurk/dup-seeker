using System.IO;
using static GruntWurk.QuickLog;

namespace GruntWurk {
    class Program {
        public static Options options;
        private static DupSeeker seeker;

        static void Main(string[] args) {
            using (seeker = new DupSeeker())
            {
                if (LoadCommandLineOptions(args))
                {
                    LoadSpecfication(ref seeker.spec);

                    timestamp("DupSeeker Started");
                    seeker.ScanAllSpecifiedFiles(options.InputFilepath);
                    timestamp("DupSeeker Done");
                }
            }
        }

        /// <summary>
        /// Parse the command-line options.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>True if the program should proceed; otherwise, False if the user merely asked for a help screen.</returns>
        private static bool LoadCommandLineOptions(string[] args) {
            options = new Options();
            bool proceed = CommandLine.Parser.Default.ParseArguments(args, options);
            if (proceed) {
                options.Validate();

                InfoEnabled = options.Verbose;
                DebugEnabled = options.Debug;
                LogFilename = options.LogFilename;

                info("\n===============================================================================");
                info(options.ToString());
                info("");
            }
            return proceed;
        }

        private static void LoadSpecfication(ref IniFile spec) {
            if (!File.Exists(options.SpecFilename)) {
                throw new FileNotFoundException("ERROR: Spec file does not exist.", options.SpecFilename);
            }
            spec = new IniFile();
            spec.LoadFile(options.SpecFilename);
            if (DebugEnabled) {
                spec.Dump();
            }
        }


    }
}
