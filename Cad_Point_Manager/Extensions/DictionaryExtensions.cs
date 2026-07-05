namespace Cad_Point_Manager.Extensions
{
    public static class DictionaryExtensions
    {
        public static bool TryGetKey<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            TValue value, out TKey key)
        {
            foreach (var pair in dictionary)
            {
                if (EqualityComparer<TValue>.Default.Equals(pair.Value, value))
                {
                    key = pair.Key;
                    return true;
                }
            }

            key = default;
            return false;
        }
    }
}
