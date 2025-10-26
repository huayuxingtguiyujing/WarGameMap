using LZ.WarGameMap.Runtime.Model;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    internal class CountryEditWindow : BaseSubWindow
    {
        static GUIContent confirmPopTitleTxt = new GUIContent("�༭��������");

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

        [Button("���沢�˳�", ButtonSizes.Medium)]
        private void SaveEdit() 
        {
            ConfirmEvent?.Invoke(countryData);
            Debug.Log("save editing");
            CloseWindow();
        }

        [Button("ȡ���༭", ButtonSizes.Medium)]
        private void CancelEdit()
        {
            CancelEvent?.Invoke();
            Debug.Log("cancel editing");
            CloseWindow();
        }

    }
}
