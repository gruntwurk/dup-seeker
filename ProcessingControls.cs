using System.Collections.Generic;
using System.IO;
using static GruntWurk.QuickLog;

namespace GruntWurk {
    /// <summary>
    /// This immutable data object contains strongly typed and validated copies of the specifications that are exressed in the given INI file.
    /// </summary>
    class ProcessingControls {
        private IniFile spec;

        string _fileType;
        int _searchColumn = 0;
        string _delimiterChar = ",";
        bool _filenameContainsDate;
        bool _prependFileName;
        bool _prependLineNumber;
        List<ConditionSpec> _inclusions = new List<ConditionSpec>();
        List<ConditionSpec> _exclusions = new List<ConditionSpec>();

        // This data object is immutable (No set-accessors on any of the properties)
        public string fileType { get { return _fileType; } }
        public int searchColumn { get { return _searchColumn; } }
        public string delimiterChar { get { return _delimiterChar; } }
        public bool filenameContainsDate { get { return _filenameContainsDate; } }
        public bool prependFileName { get { return _prependFileName; } }
        public bool prependLineNumber { get { return _prependLineNumber; } }
        public List<ConditionSpec> inclusions { get { return _inclusions; } }
        public List<ConditionSpec> exclusions { get { return _exclusions; } }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="spec"></param>
        public ProcessingControls(IniFile spec) {
            LoadControlSpecifications(spec);
            LoadConditionSpecifications(inclusions, spec, "INCLUDE");
            LoadConditionSpecifications(exclusions, spec, "EXCLUDE");
        }

        private void LoadControlSpecifications(IniFile spec) {
            _fileType = spec.GetString("File", "Type", "").ToUpper();
            if (_fileType == "CSV") {
                _delimiterChar = ",";
            } else if (_fileType == "TSV") {
                _delimiterChar = "\t";
            } else {
                throw new FileLoadException(Program.APP_NAME + " currently only works with CSV and TSV files. Spec file must positively specify Type=CSV in the [File] section.");
            }
            _delimiterChar = spec.GetString("File", "Delimiter", delimiterChar).ToUpper();
            if (_delimiterChar == "TAB" || _delimiterChar == "\\T") {
                _delimiterChar = "\t";
            }

            _prependFileName = spec.GetBool("File", "PrependFileName", false);
            _prependLineNumber = spec.GetBool("File", "PrependLineNumber", false);

            _searchColumn = spec.GetInt("File", "SearchColumn", 0);
            if (_searchColumn < 1) {
                throw new FileLoadException("Spec file must specify a SearchColumn number, in the [File] section.");
            }
        }
        private void LoadConditionSpecifications(List<ConditionSpec> conditionList, IniFile spec, string SectionName) {
            if (spec.Sections.ContainsKey(SectionName)) {
                foreach (string columnId in spec.Sections[SectionName].Keys) {
                    string valueList = spec.GetString(SectionName, columnId, "");
                    string[] values = valueList.Split(StringUtils.JUST_COMMA);
                    foreach (string value in values) {
                        ConditionSpec cond = new ConditionSpec(columnId, value.Trim());
                        conditionList.Add(cond);
                    }
                }
            }
        }

    }

}
