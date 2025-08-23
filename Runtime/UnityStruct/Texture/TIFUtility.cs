using LZ.WarGameCommon;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace LZ.WarGameMap.Runtime {
    public class TIFUtility : Singleton<TIFUtility> {

        public TIFUtility() { }


    }

    // NOTE: adobe TIF file specification:
    // https://web.archive.org/web/20060615022002/http://partners.adobe.com/public/developer/tiff/index.html
    public class TIFFTexture {
        bool ByteOrder;
        byte[] data;

        public int ImageWidth = 0;
        public int ImageLength = 0;

        public List<int> BitsPerSample = new List<int>();
        public int PixelBytes = 0;
        public int Compression = 0;

        public int PhotometricInterpretation = 0;
        public int RowsPerStrip = 0;
        public List<int> StripOffsets = new List<int>();
        public List<int> StripByteCounts = new List<int>();

        public float XResolution = 0f;
        public float YResolution = 0f;
        public int ResolutionUnit = 0;

        public int Predictor = 0;
        public List<int> SampleFormat = new List<int>();
        public string DateTime = "";
        public string Software = "";

        public void Decode(string path) {
            data = File.ReadAllBytes(path);

            // ���Ƚ����ļ�ͷ����ñ��뷽ʽ�Ǵ�˻���С�ˣ��Լ���һ��IFD��λ��
            int pIFD = DecodeIFH();

            // Ȼ������һ��IFD������ֵ����һ��IFD�ĵ�ַ
            while (pIFD != 0) {
                pIFD = DecodeIFD(pIFD);
            }
        }

        private int DecodeIFH() {
            string byteOrder = GetString(0, 2);
            if (byteOrder == "II")
                ByteOrder = true;
            else if (byteOrder == "MM")
                ByteOrder = false;
            else
                throw new UnityException("The order value is not II or MM.");

            int Version = GetInt(2, 2);

            if (Version != 42)
                throw new UnityException("Not TIFF.");

            return GetInt(4, 4);
        }

        public int DecodeIFD(int Pos) {
            int n = Pos;
            int DECount = GetInt(n, 2);
            n += 2;
            for (int i = 0; i < DECount; i++) {
                DecodeDE(n);
                n += 12;
            }

            //�ѻ��ÿ��ɨ����λ�ã���С��ѹ����ʽ���������ͣ����������н���
            DecodeStrips();
            int pNext = GetInt(n, 4);
            return pNext;
        }

        private void DecodeDE(int Pos) {
            int TagIndex = GetInt(Pos, 2);
            int TypeIndex = GetInt(Pos + 2, 2);
            int Count = GetInt(Pos + 4, 4);

            //�Ȱ��ҵ����ݵ�λ��
            int pData = Pos + 8;
            int totalSize = TypeArray[TypeIndex].size * Count;
            if (totalSize > 4)
                pData = GetInt(pData, 4);

            //�ٸ���Tag��ֵ������������
            GetDEValue(TagIndex, TypeIndex, Count, pData);
        }

        private void DecodeStrips() {
            //...
        }

        #region get value in TIF file

        private int GetInt(int startPos, int Length) {
            int value = 0;
            if (ByteOrder) {
                for (int i = 0; i < Length; i++) {
                    value |= data[startPos + i] << i * 8;
                }
            } else {
                for (int i = 0; i < Length; i++) {
                    value |= data[startPos + Length - 1 - i] << i * 8;
                }
            }
            return value;
        }

        private float GetRational(int startPos) {
            int A = GetInt(startPos, 4);
            int B = GetInt(startPos + 4, 4);
            return A / B;
        }

        private float GetFloat(byte[] b, int startPos) {
            byte[] byteTemp;
            if (ByteOrder) {
                byteTemp = new byte[] { 
                    b[startPos], b[startPos + 1], 
                    b[startPos + 2], b[startPos + 3] 
                };
            } else {
                byteTemp = new byte[] { 
                    b[startPos + 3], b[startPos + 2], 
                    b[startPos + 1], b[startPos] 
                };
            }
                
            float fTemp = BitConverter.ToSingle(byteTemp, 0);
            return fTemp;
        }

        private string GetString(int startPos, int Length)//II��MM��Stringû��Ӱ��
        {
            string tmp = "";
            for (int i = 0; i < Length; i++) {
                tmp += (char)data[startPos];
            }
            return tmp;
        }

        private void GetDEValue(int TagIndex, int TypeIndex, int Count, int pdata) {
            int typesize = TypeArray[TypeIndex].size;
            switch (TagIndex) {
                case 254:           //NewSubfileType
                    break;
                case 255:           //SubfileType
                    break;
                case 256:           //ImageWidth
                    ImageWidth = GetInt(pdata, typesize);
                    break;
                case 257:           //ImageLength
                    if (TypeIndex == 3) {
                        ImageLength = GetInt(pdata, typesize);
                    }
                    break;
                case 258:           //BitsPerSample
                    for (int i = 0; i < Count; i++) {
                        int v = GetInt(pdata + i * typesize, typesize);
                        BitsPerSample.Add(v);
                        PixelBytes += v / 8;
                    }
                    break;
                case 259:           //Compression
                    Compression = GetInt(pdata, typesize); 
                    break;
                case 262:           //PhotometricInterpretation
                    PhotometricInterpretation = GetInt(pdata, typesize); 
                    break;
                case 273:           //StripOffsets
                    for (int i = 0; i < Count; i++) {
                        int v = GetInt(pdata + i * typesize, typesize);
                        StripOffsets.Add(v);
                    }
                    break;
                case 274:           //Orientation
                    break;
                case 277:           //SamplesPerPixel
                    break;
                case 278:           //RowsPerStrip
                    RowsPerStrip = GetInt(pdata, typesize);
                    break;
                case 279:           //StripByteCounts
                    for (int i = 0; i < Count; i++) {
                        int v = GetInt(pdata + i * typesize, typesize);
                        StripByteCounts.Add(v);
                    }
                    break;
                case 282:           //XResolution
                    XResolution = GetRational(pdata);
                    break;
                case 283:           //YResolution
                    YResolution = GetRational(pdata);
                    break;
                case 284:           //PlanarConfig
                    break;
                case 296:           //ResolutionUnit
                    ResolutionUnit = GetInt(pdata, typesize);
                    break;
                case 305:           //Software
                    Software = GetString(pdata, typesize);
                    break;
                case 306:           //DateTime
                    DateTime = GetString(pdata, typesize);
                    break;
                case 315:           //Artist
                    break;
                case 317:           //Differencing Predictor
                    Predictor = GetInt(pdata, typesize);
                    break;
                case 320:           //ColorDistributionTable
                    break;
                case 338:           //ExtraSamples
                    break;
                case 339:           //SampleFormat
                    for (int i = 0; i < Count; i++) {
                        int v = GetInt(pdata + i * typesize, typesize);
                        SampleFormat.Add(v);
                    }
                    break;
                default:
                    break;
            }
        }

        #endregion

        static private DType[] TypeArray = {
            
        };

        private struct DType {
            public string name;
            public int size;

            public DType(string n, int s) {
                name = n;
                size = s;
            }
        }
    }
}