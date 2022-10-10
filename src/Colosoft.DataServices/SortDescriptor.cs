using System;

namespace Colosoft.DataServices
{
    public class SortDescriptor
    {
        public SortDescriptor()
        {
            this.Property = string.Empty;
        }

        public SortDescriptor(string property, SortOrder sortOrder = SortOrder.Ascending)
        {
            this.Property = property;
            this.SortOrder = sortOrder;
        }

        public string Property { get; set; }

        public SortOrder SortOrder { get; set; } = SortOrder.Ascending;

        public override bool Equals(object? obj)
        {
            if (obj is SortDescriptor sortDescriptor)
            {
                return this.Property == sortDescriptor.Property && this.SortOrder == sortDescriptor.SortOrder;
            }

            return object.Equals(this, obj);
        }

        public override int GetHashCode() =>
            (this.Property?.GetHashCode(StringComparison.Ordinal) ?? 0) ^ this.SortOrder.GetHashCode();
    }
}