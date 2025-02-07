
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    // TODO : �и��ܴ�����⣬�����л�֮��õ������ HeightDataModel ռ�ռ�ǳ��󣬼�����������5���洢
    // �����Ĵ�С���϶������ܸ��ŵ� runtime ���棬���Ծ�����Ҫдһ�� runtime �ع��� HeightDataModel
    public class HeightDataModel : ScriptableObject {

        public int heightFileNums;

        public int singleHeightFileSize;

        [SerializeField] List<HeightData> heightDataList;
        [SerializeField] Dictionary<string, HeightData> heightDataDict;

        [Tooltip("�� model ���������е������ݶ�Ӧ�ľ�γ�ȣ��Դ˵��޸�û���κ����ã�����չʾ")]
        public List<Vector2Int> GeographyList;

        private void OnEnable() {
            UpdateData();
        }

        public HeightDataModel() { }

        public void InitHeightModel(int heightFileNums, int size) {
            this.heightFileNums = heightFileNums;
            this.singleHeightFileSize = size;
            heightDataList = new List<HeightData>();
            heightDataDict = new Dictionary<string, HeightData>();
            GeographyList = new List<Vector2Int>();
        }

        public void AddHeightData(int longitude, int latitude, int size, float[,] heightDatas) {
            HeightData heightData = new HeightData(longitude, latitude, size, heightDatas);
            heightDataList.Add(heightData);
            GeographyList.Add(new Vector2Int(longitude, latitude));
        }

        public HeightData GetHeightData(int longitude, int latitude) {
            string key = GetHeightDataKey(longitude, latitude);
            return heightDataDict[key];
        }

        public bool ExistHeightData(int longitude, int latitude) {
            UpdateData();
            string key = GetHeightDataKey(longitude, latitude);
            return heightDataDict.ContainsKey(key);
        }

        internal string GetHeightDataKey(int longitude, int latitude) {
            return string.Format("n{0}_e{1}", longitude, latitude);
        }

        private void UpdateData(){
            if(heightDataDict == null) {
                heightDataDict = new Dictionary<string, HeightData>();
            }
            if(heightDataList == null) {
                heightDataList = new List<HeightData>();
            }

            foreach (var heightData in heightDataList) {
                string key = GetHeightDataKey(heightData.longitude, heightData.latitude);
                if (heightDataDict.ContainsKey(key)) {
                    heightDataDict[key] = heightData;
                } else {
                    heightDataDict.Add(key, heightData);
                }
            }
        }

    }


    // TODO : runtime ��ʱ�� �����
    public class HeightDataModel_Runtime {

        public int heightFileNums;

        public int singleHeightFileSize;

        List<HeightData> heightDataList;
        Dictionary<string, HeightData> heightDataDict;

        List<Vector2Int> GeographyList;

        private void OnEnable() { UpdateData(); }

        public HeightDataModel_Runtime() { }

        public void InitHeightModel(int heightFileNums, int size) {
            this.heightFileNums = heightFileNums;
            this.singleHeightFileSize = size;
            heightDataList = new List<HeightData>();
            heightDataDict = new Dictionary<string, HeightData>();
            GeographyList = new List<Vector2Int>();
        }

        public void AddHeightData(int longitude, int latitude, int size, float[,] heightDatas) {
            HeightData heightData = new HeightData(longitude, latitude, size, heightDatas);
            heightDataList.Add(heightData);
            GeographyList.Add(new Vector2Int(longitude, latitude));
        }

        public HeightData GetHeightData(int longitude, int latitude) {
            string key = GetHeightDataKey(longitude, latitude);
            return heightDataDict[key];
        }

        public bool ExistHeightData(int longitude, int latitude) {
            UpdateData();
            string key = GetHeightDataKey(longitude, latitude);
            return heightDataDict.ContainsKey(key);
        }

        internal string GetHeightDataKey(int longitude, int latitude) {
            return string.Format("n{0}_e{1}", longitude, latitude);
        }

        private void UpdateData() {
            if (heightDataDict == null) {
                heightDataDict = new Dictionary<string, HeightData>();
            }
            if (heightDataList == null) {
                heightDataList = new List<HeightData>();
            }

            foreach (var heightData in heightDataList) {
                string key = GetHeightDataKey(heightData.longitude, heightData.latitude);
                if (heightDataDict.ContainsKey(key)) {
                    heightDataDict[key] = heightData;
                } else {
                    heightDataDict.Add(key, heightData);
                }
            }
        }

    }
}
