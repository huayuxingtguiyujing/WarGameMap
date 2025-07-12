using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class VirtualTexture
    {
        
    }

    public static class MortonCode {
        

        // Encodes two 16-bit integers into one 32-bit morton code
        public static int MortonEncode(int x, int y) {
            int Morton = MortonCode2(x) | (MortonCode2(y) << 1);
            return Morton;
        }
        public static int MortonCode2(int x) {
            x &= 0x0000ffff;
            x = (x ^ (x << 8)) & 0x00ff00ff;
            x = (x ^ (x << 4)) & 0x0f0f0f0f;
            x = (x ^ (x << 2)) & 0x33333333;
            x = (x ^ (x << 1)) & 0x55555555;
            return x;
        }

        public static void MortonDecode(int Morton, out int x, out int y) {
            x = ReverseMortonCode2(Morton);
            y = ReverseMortonCode2(Morton >> 1);
        }

        public static int ReverseMortonCode2(int x) {
            x &= 0x55555555;
            x = (x ^ (x >> 1)) & 0x33333333;
            x = (x ^ (x >> 2)) & 0x0f0f0f0f;
            x = (x ^ (x >> 4)) & 0x00ff00ff;
            x = (x ^ (x >> 8)) & 0x0000ffff;
            return x;
        }

    }
}
