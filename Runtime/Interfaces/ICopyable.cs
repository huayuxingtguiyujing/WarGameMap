using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public interface ICopyable<T> where T : class
    {
        public T CopyObject();

        public void Copy(T obj);

    }
}
