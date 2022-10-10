namespace Colosoft.DataServices
{
    public class DataSourceFilterKeyValueAction
    {
        public string? FieldKey { get; set; }

        public object? FieldValue { get; set; }

        public DataSourceFilterLogic FilterLogic { get; set; }

        public DataSourceFilterAction FilterAction { get; set; }
    }
}