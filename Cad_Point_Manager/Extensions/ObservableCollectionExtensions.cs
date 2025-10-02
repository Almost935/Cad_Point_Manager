using System.Collections.ObjectModel;

namespace Cad_Point_Manager.Extensions
{
    public static class ObservableCollectionExtensions
    {
        public static void AddRange<T>(this ObservableCollection<T> source, IEnumerable<T> items)
        {
            if (source is null) { throw new ArgumentNullException(nameof(source)); }
            if (items is null) { return; }

            foreach (var item in items)
            {
                source.Add(item); // raises CollectionChanged per item
            }
        }
    }
}
