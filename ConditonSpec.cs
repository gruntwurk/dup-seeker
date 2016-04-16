namespace GruntWurk {
    class ConditionSpec {
        public int col;
        public string condition;

        public ConditionSpec(string columnId, string value) {
            this.col = int.Parse(columnId);
            this.condition = value.Trim();
        }
    }
}
