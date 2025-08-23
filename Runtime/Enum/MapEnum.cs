using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public static class MapEnum
    {

        // go in the hierarchy!
        public static string MapRootName = "mapRoot";

        public static string ClusterParentName = "clusters";

        public static string SignParentName = "signs";


        public static string OtherRootName = "otherRoot";

        public static GameObject GetOtherRootObj()
        {
            GameObject otherRootObj = GameObject.Find(OtherRootName);
            if (otherRootObj == null)
            {
                otherRootObj = new GameObject(OtherRootName);
            }
            return otherRootObj;
        }


        public static Color DefaultGridColor = Color.white;


    }
}
