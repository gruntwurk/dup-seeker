using System;
using System.Collections.Generic;
using System.IO;
using static GruntWurk.QuickLog;

namespace GruntWurk {
    class DupSeeker: IDisposable { 
        private StreamWriter outputFile;
        public IniFile spec;
        string shortFilename;
        int searchColumn = 0;
        string delimiterChar = ",";
        bool filenameContainsDate;
        bool prependFileName;
        bool prependLineNumber;
        string fileDate = "";
        string fileType;
        List<ConditionSpec> inclusions = new List<ConditionSpec>();
        List<ConditionSpec> exclusions = new List<ConditionSpec>();

        public void ScanAllSpecifiedFiles(string FilenameWithPossibleWildcards) {
            if (spec == null)
            {
                throw new FileNotFoundException("A specification (INI) file must be loaded before the Scan method is called.");
            }
            using (outputFile = new StreamWriter(Program.options.OutputFilename)) {
                string folderPart = Path.GetDirectoryName(FilenameWithPossibleWildcards);
                string filenamePart = Path.GetFileName(FilenameWithPossibleWildcards);
                if (folderPart == "")
                {
                    folderPart = "."; // Directory.GetCurrentDirectory();
                }

                string[] fileEntries = Directory.GetFiles(folderPart, filenamePart);
                foreach (string InputFilename in fileEntries) {
                    if (!File.Exists(InputFilename)) {
                        throw new FileNotFoundException("ERROR: Input file does not exist.", InputFilename);
                    }
                    ScanFile(InputFilename);
                }
            }
        }

        private void ScanFile(string InputFilename)
        {
            int lineCount = 0;
            shortFilename = Path.GetFileName(InputFilename);

            Dictionary<string, int> keyValueOccurances = new Dictionary<string, int>();
            string keyValueToCompare;
            string[] currentRow;
            Microsoft.VisualBasic.FileIO.TextFieldParser CsvReader;

            int missedInclusionCount = 0;
            int excludedCount = 0;
            int processedCount = 0;


            // Load the various specifications from the INI file into this object's fields
            if (!TransferSpecifications())
            {
                return;
            }
            TransferConditionSpecs(inclusions, "INCLUDE");
            TransferConditionSpecs(exclusions, "EXCLUDE");
            if (DebugEnabled)
            {
                debug("Number of inclusions = {0}", inclusions.Count);
                foreach (ConditionSpec cond in inclusions)
                {
                    debug("    {0} = {1}", cond.col, cond.condition);
                }
                debug("Number of exclusions = {0}", exclusions.Count);
                foreach (ConditionSpec cond in exclusions)
                {
                    debug("    {0} = {1}", cond.col, cond.condition);
                }
            }

            if (filenameContainsDate)
            {
                info("Processing {0} ({1})...", InputFilename, fileDate);
            }
            else {
                info("Processing {0}...", InputFilename);
            }

            // PASS 1: Gather a list of all distinct values in the search column, counting occurances of each
            CsvReader = new Microsoft.VisualBasic.FileIO.TextFieldParser(InputFilename);
            using (CsvReader)
            {
                CsvReader.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                CsvReader.Delimiters = new string[] { "," };
                lineCount = 0;
                while (!CsvReader.EndOfData)
                {
                    try
                    {
                        currentRow = CsvReader.ReadFields();
                        lineCount++;
                        if (inclusions.Count > 0 && !conditionMatchesAny(inclusions, currentRow))
                        {
                            missedInclusionCount++;
                            continue;
                        }
                        if (exclusions.Count > 0 && conditionMatchesAny(exclusions, currentRow))
                        {
                            excludedCount++;
                            continue;
                        }

                        processedCount++;
                        keyValueToCompare = currentRow[searchColumn - 1];
                        if (keyValueOccurances.ContainsKey(keyValueToCompare))
                        {
                            keyValueOccurances[keyValueToCompare]++;
                        }
                        else {
                            keyValueOccurances[keyValueToCompare] = 1;
                        }
                    }
                    catch (Microsoft.VisualBasic.FileIO.MalformedLineException ex)
                    {
                        // TODO Use delimiterChar
                        outputFile.WriteLine("{0},{1},{2},{3}", InputFilename, ex.LineNumber, "X", ex.Message);
                    }
                }
            }


            info("    {0} total lines in source file.", lineCount);
            info("    {0} lines were specifically excluded.", excludedCount);
            info("    {0} lines were excluded because they missed the inclusion criteria.", missedInclusionCount);
            info("    {0} lines were processed.", processedCount);


            int numberOfKeysWithDups = GatherAndReportStatistics(keyValueOccurances);
            if (numberOfKeysWithDups == 0)
            {
                // Nothing left to do here
                return;
            }



            // PASS 2: Output the lines with dups
            CsvReader = new Microsoft.VisualBasic.FileIO.TextFieldParser(InputFilename);
            int outlineCount = 0;
            using (CsvReader)
            {
                String outline;
                CsvReader.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                CsvReader.Delimiters = new string[] { delimiterChar };
                lineCount = 0;
                excludedCount = 0;
                missedInclusionCount = 0;
                processedCount = 0;
                while (!CsvReader.EndOfData)
                {
                    try
                    {
                        currentRow = CsvReader.ReadFields();
                        lineCount++;
                        if (inclusions.Count > 0 && !conditionMatchesAny(inclusions, currentRow))
                        {
                            missedInclusionCount++;
                            continue;
                        }
                        if (exclusions.Count > 0 && conditionMatchesAny(exclusions, currentRow))
                        {
                            excludedCount++;
                            continue;
                        }
                        processedCount++;
                        if (keyValueOccurances[currentRow[searchColumn - 1]] > 1)
                        {
                            outline = "";
                            if (prependFileName)
                            {
                                outline += shortFilename + delimiterChar;
                            }
                            if (filenameContainsDate)
                            {
                                outline += fileDate + delimiterChar;
                            }
                            if (prependLineNumber)
                            {
                                outline += lineCount.ToString() + delimiterChar;
                            }
                            outline += string.Join(delimiterChar, currentRow);
                            outputFile.WriteLine(outline);
                            outlineCount++;
                        }

                    }
                    catch (Microsoft.VisualBasic.FileIO.MalformedLineException ex)
                    {
                        // TODO Use delimiterChar
                        outputFile.WriteLine("{0},{1},{2},{3}", fileDate, ex.LineNumber, "X", ex.Message);
                    }
                }
            }
            //            debug("Pass 2: ");
            //            debug("    Total Line Count: {0}", lineCount);
            //            debug("    Excluded Line Count: {0}", excludedCount);
            //            debug("    Missed Inclusion Line Count: {0}", missedInclusionCount);
            //            debug("    Processed Line Count: {0}", processedCount);

            info("    {0} records written to output.", outlineCount);

        }

        private static int GatherAndReportStatistics(Dictionary<string, int> keyValueOccurances)
        {
            int numberOfKeysWithDups = 0;
            int dupCount = 0;
            int maxDupCount = 0;
            foreach (string key in keyValueOccurances.Keys)
            {
                if (keyValueOccurances[key] > 1)
                {
                    numberOfKeysWithDups++;
                    dupCount += keyValueOccurances[key] - 1;
                    if (keyValueOccurances[key] > maxDupCount)
                    {
                        maxDupCount = keyValueOccurances[key];
                    }
                }
            }
            info("\n    {0} distinct keys found.", keyValueOccurances.Count);
            info("    {0} of them have duplicates.", numberOfKeysWithDups);
            if (numberOfKeysWithDups > 0)
            {
                info("    There are an average of {0} duplicates per duplicated key.", dupCount / numberOfKeysWithDups);
                info("    Some keys have as many as {0} duplicates.\n", maxDupCount);
            }

            return numberOfKeysWithDups;
        }

        private bool TransferSpecifications()
        {
            bool keepGoing = true;
            fileType = spec.GetString("File", "Type", "").ToUpper();
            if (fileType != "CSV")
            {
                log("ERROR: DupSeeker currently only works with CSV files. Spec file must positively specify Type=CSV in the [File] section.");
                keepGoing = false;
            }
            filenameContainsDate = spec.GetBool("File", "FilenameContainsDate", false);
            prependFileName = spec.GetBool("File", "PrependFileName", false);
            prependLineNumber = spec.GetBool("File", "PrependLineNumber", false);
            delimiterChar = spec.GetString("File", "Delimiter", delimiterChar);
            searchColumn = spec.GetInt("File", "SearchColumn", 0);
            if (searchColumn < 1)
            {
                log("ERROR: Spec file must specify a SearchColumn number, in the [File] section.");
                keepGoing = false;
            }
            if (filenameContainsDate)
            {
                // TODO Use the generalized routine for finding dates in filenames
                fileDate = StringUtils.Right(shortFilename, 8);
                fileDate = StringUtils.Mid(fileDate, 5, 2) + "/" + StringUtils.Mid(fileDate, 7, 2) + "/" + StringUtils.Left(fileDate, 4);
            }

            return keepGoing;
        }

        private void TransferConditionSpecs(List<ConditionSpec> conditionList, string SectionName)
        {
            if (spec.Sections.ContainsKey(SectionName))
            {
                foreach (string columnId in spec.Sections[SectionName].Keys)
                {
                    string valueList = spec.GetString(SectionName, columnId, "");
                    string[] values = valueList.Split(StringUtils.JUST_COMMA);
                    foreach (string value in values)
                    {
                        ConditionSpec cond = new ConditionSpec(columnId, value.Trim());
                        conditionList.Add(cond);
                    }
                }
            }
        }

        private bool conditionMatchesAny(List<ConditionSpec> conditions, string[] currentRow) {
            foreach (ConditionSpec cond in conditions) {
                if (currentRow[cond.col - 1] == cond.condition) {
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
            // Currently, nothing to do here
        }
    }
}
