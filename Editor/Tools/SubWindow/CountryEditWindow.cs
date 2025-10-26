using LZ.WarGameMap.Runtime.Model;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    internal class CountryEditWindow : BaseSubWindow
    {
        static GUIContent confirmPopTitleTxt = new GUIContent("编辑区域数据");

        public static CountryEditWindow GetPopInstance()
        {
            return GetPopInstance<CountryEditWindow>(confirmPopTitleTxt, verticalWindowSize);
        }

        public CountryData countryData;

        Action<CountryData> ConfirmEvent;
        Action CancelEvent;

        public override void ShowSubWindow(params object[] args)
        {
            countryData = (CountryData)args[0];
            ConfirmEvent = (Action<CountryData>)args[1];
            CancelEvent = (Action)args[2];
            base.ShowSubWindow(args);
        }

        public override void HideSubWindow()
        {
            ConfirmEvent = null;
            CancelEvent = null;
            base.HideSubWindow();
        }

        [Button("保存并退出", ButtonSizes.Medium)]
        private void SaveEdit() 
        {
            ConfirmEvent?.Invoke(countryData);
            Debug.Log("save editing");
            CloseWindow();
        }

        [Button("取消编辑", ButtonSizes.Medium)]
        private void CancelEdit()
        {
            CancelEvent?.Invoke();
            Debug.Log("cancel editing");
            CloseWindow();
        }

    }
}
