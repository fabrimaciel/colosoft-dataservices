using System.Diagnostics.CodeAnalysis;

namespace Colosoft.DataServices.Test
{
    internal class CustomModelEqualityComparer : IEqualityComparer<CustomModel>
    {
        public bool Equals(CustomModel? x, CustomModel? y)
        {
            return x?.Id == y?.Id;
        }

        public int GetHashCode([DisallowNull] CustomModel obj)
        {
            return obj?.Id ?? -1;
        }
    }
}
