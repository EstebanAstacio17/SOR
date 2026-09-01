using System;
using System.Data;

namespace SOR.Helpers
{
    public static class DataReaderExtensions
    {
        public static bool TableHasColumn(this IDataRecord dr, string columnName)
        {
            if (dr == null || string.IsNullOrEmpty(columnName)) return false;
            for (int i = 0; i < dr.FieldCount; i++)
            {
                if (dr.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
