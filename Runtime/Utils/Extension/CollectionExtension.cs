using System.Collections.Generic;

namespace LZ.WarGameMap.Runtime
{
    public static class CollectionExtension
    {

        public static void FillInList<T>(this List<T> list, int count)
        {
            T val = default(T);
            for(int i = 0; i < count; i++)
            {
                list.Add(val);
            }
        }


    }
}
