namespace GruntWurk
{
    class FieldSpec {
        public string FieldName;
        public int LineNo;
        public int ColNo;
        public int Width;
        // TODO If we never do anything with FieldType, delete it.
        public string FieldType;

        public FieldSpec(string FieldName, string spec) {
            // TODO Switch these for the JUST_SPACE and JUST_COMMA constants in StringUtils
            char[] delimComma = { ',' };
            char[] delimSpace = { ' ' };

            this.FieldName = FieldName;

            string[] parts = spec.ToUpper().Split(delimComma);
            foreach (string part in parts) {
                string partTrimmed = part.Trim();
                string[] subparts = partTrimmed.Split(delimSpace, 2);

                switch (subparts[0]) {
                    case "LINE":
                        LineNo = int.Parse(subparts[1]);
                        break;
                    case "COL":
                        ColNo = int.Parse(subparts[1]);
                        break;
                    case "WIDTH":
                        Width = int.Parse(subparts[1]);
                        break;
                    case "TYPE":
                        FieldType = subparts[1];
                        break;
                }
            }
        }

        public int AdjustedLineNo(int LineCount) {
            return (LineNo > 0) ? LineNo : LineCount + LineNo;
        }
    }
}
