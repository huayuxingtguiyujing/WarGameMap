using System;

namespace LZ.WarGameMap.Runtime
{
    // a attribute to note that this function will not use Unity Objects API
    // so you can use it in some situation like multithread 
    [AttributeUsage(AttributeTargets.Method)]
    public class NoUnityAPIAttribute : Attribute
    {
        public string Reason { get; }

        public NoUnityAPIAttribute(string reason = "")
        {
            Reason = reason;
        }
    }
}
