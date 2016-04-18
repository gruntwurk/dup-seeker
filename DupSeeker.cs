using System;
using System.Collections.Generic;
using System.IO;
using static GruntWurk.QuickLog;

namespace GruntWurk {
    class DupSeeker : IDisposable {
        ProcessingControls controls;
        StreamWriter outputFile;
        string shortFilename;
        string fileDate = "";

        public DupSeeker(IniFile spec) {
            controls = new ProcessingControls(spec);
        }

        /// <summary>
        /// Call this method to execute the dup-seeking logic.
        /// </summary>
        /// <param name="FilenameWithPossibleWildcards">The (qualified) file name of the input file. May contain wildcards.</param>
        public void ScanAllSpecifiedFiles(string FilenameWithPossibleWildcards) {
            using (outputFile = new StreamWriter(Program.options.OutputFilename)) {
                string folderPart = Path.GetDirectoryName(FilenameWithPossibleWildcards);
                string filenamePart = Path.GetFileName(FilenameWithPossibleWildcards);
                if (folderPart == "") {
                    folderPart = ".";
                }

                string[] fileEntries = Directory.GetFiles(folderPart, filenamePart);
                foreach (string InputFilename in fileEntries) {
                    if (!File.Exists(InputFilename)) {
                        throw new FileNotFoundException("Input file does not exist.", InputFilename);
                    }
                    ScanFile(InputFilename);
                }
            }
        }

        // TODO Although this method is long, it's pretty straight-forward. Still, we might consider breaking it up into smaller methods.
        // (If, for no other reason, than to support adding unit tests around each extracted sub-method.)
        private void ScanFile(string InputFilename) {
            int lineCount = 0;
            shortFilename = Path.GetFileName(InputFilename);
            if (controls.filenameContainsDate) {
                DateTime dt = FileUtils.DateFromFileName(shortFilename);
                if (dt > DateTime.MinValue) {
                    fileDate = dt.ToString("MM/dd/YY");
                } else {
                    fileDate = "";
                }
            }

            Dictionary<string, int> keyValueOccurances = new Dictionary<string, int>();
            string keyValueToCompare;
            string[] currentRow;
            Microsoft.VisualBasic.FileIO.TextFieldParser CsvReader;

            int missedInclusionCount = 0;
            int excludedCount = 0;
            int processedCount = 0;

            if (DebugEnabled) {
                debug("Number of inclusions = {0}", controls.inclusions.Count);
                foreach (ConditionSpec cond in controls.inclusions) {
                    debug("    {0} = {1}", cond.col, cond.condition);
                }
                debug("Number of exclusions = {0}", controls.exclusions.Count);
                foreach (ConditionSpec cond in controls.exclusions) {
                    debug("    {0} = {1}", cond.col, cond.condition);
                }
            }

            if (controls.filenameContainsDate) {
                info("Processing {0} ({1})...", InputFilename, fileDate);
            } else {
                info("Processing {0}...", InputFilename);
            }

            // PASS 1: Gather a list of all distinct values in the search column, counting occurances of each
            CsvReader = new Microsoft.VisualBasic.FileIO.TextFieldParser(InputFilename);
            using (CsvReader) {
                CsvReader.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                CsvReader.Delimiters = new string[] { "," };
                lineCount = 0;
                while (!CsvReader.EndOfData) {
                    try {
                        currentRow = CsvReader.ReadFields();
                        lineCount++;
                        if (controls.inclusions.Count > 0 && !conditionMatchesAny(controls.inclusions, currentRow)) {
                            missedInclusionCount++;
                            continue;
                        }
                        if (controls.exclusions.Count > 0 && conditionMatchesAny(controls.exclusions, currentRow)) {
                            excludedCount++;
                            continue;
                        }

                        processedCount++;
                        keyValueToCompare = currentRow[controls.searchColumn - 1];
                        if (keyValueOccurances.ContainsKey(keyValueToCompare)) {
                            keyValueOccurances[keyValueToCompare]++;
                        } else {
                            keyValueOccurances[keyValueToCompare] = 1;
                        }
                    } catch (Microsoft.VisualBasic.FileIO.MalformedLineException) {
                        // Do nothing about MalformedLineExceptions (this pass)
                    }
                }
            }


            info("    {0} total lines in source file.", lineCount);
            info("    {0} lines were specifically excluded.", excludedCount);
            info("    {0} lines were excluded because they missed the inclusion criteria.", missedInclusionCount);
            info("    {0} lines were processed.", processedCount);


            int numberOfKeysWithDups = GatherAndReportStatistics(keyValueOccurances);
            if (numberOfKeysWithDups == 0) {
                // Nothing left to do here
                return;
            }



            // PASS 2: Output the lines with dups
            CsvReader = new Microsoft.VisualBasic.FileIO.TextFieldParser(InputFilename);
            int outlineCount = 0;
            using (CsvReader) {
                String outline;
                CsvReader.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                CsvReader.Delimiters = new string[] { controls.delimiterChar };
                lineCount = 0;
                excludedCount = 0;
                missedInclusionCount = 0;
                processedCount = 0;
                while (!CsvReader.EndOfData) {
                    try {
                        currentRow = CsvReader.ReadFields();
                        lineCount++;
                        if (controls.inclusions.Count > 0 && !conditionMatchesAny(controls.inclusions, currentRow)) {
                            missedInclusionCount++;
                            continue;
                        }
                        if (controls.exclusions.Count > 0 && conditionMatchesAny(controls.exclusions, currentRow)) {
                            excludedCount++;
                            continue;
                        }
                        processedCount++;
                        if (keyValueOccurances[currentRow[controls.searchColumn - 1]] > 1) {
                            outline = "";
                            if (controls.prependFileName) {
                                outline += shortFilename + controls.delimiterChar;
                            }
                            if (controls.filenameContainsDate && fileDate != "") {
                                outline += fileDate + controls.delimiterChar;
                            }
                            if (controls.prependLineNumber) {
                                outline += lineCount.ToString() + controls.delimiterChar;
                            }
                            outline += string.Join(controls.delimiterChar, currentRow);
                            outputFile.WriteLine(outline);
                            outlineCount++;
                        }

                    } catch (Microsoft.VisualBasic.FileIO.MalformedLineException ex) {
                        // For now, we'll report badly formatted data lines together with the other findings.
                        // TODO Use delimiterChar instead of assuming commas
                        outputFile.WriteLine("{0},{1},{2},{3}", fileDate, ex.LineNumber, "X", ex.Message);
                    }
                }
            }

            info("    {0} records written to output.", outlineCount);

        }

        private static int GatherAndReportStatistics(Dictionary<string, int> keyValueOccurances) {
            int numberOfKeysWithDups = 0;
            int dupCount = 0;
            int maxDupCount = 0;
            foreach (string key in keyValueOccurances.Keys) {
                if (keyValueOccurances[key] > 1) {
                    numberOfKeysWithDups++;
                    dupCount += keyValueOccurances[key] - 1;
                    if (keyValueOccurances[key] > maxDupCount) {
                        maxDupCount = keyValueOccurances[key];
                    }
                }
            }
            info("\n    {0} distinct keys found.", keyValueOccurances.Count);
            info("    {0} of them have duplicates.", numberOfKeysWithDups);
            if (numberOfKeysWithDups > 0) {
                info("    There are an average of {0} duplicates per duplicated key.", dupCount / numberOfKeysWithDups);
                info("    Some keys have as many as {0} duplicates.\n", maxDupCount);
            }

            return numberOfKeysWithDups;
        }

        private bool conditionMatchesAny(List<ConditionSpec> conditions, string[] currentRow) {
            foreach (ConditionSpec cond in conditions) {
                if (currentRow[cond.col - 1] == cond.condition) {
                    return true;
                }
            }
            return false;
        }

        public void Dispose() {
            // Currently, nothing to do here
        }
    }

}
