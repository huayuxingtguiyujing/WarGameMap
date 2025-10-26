using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    public class MountainNoiseSubWindow : BaseSubWindow
    {
        static GUIContent confirmPopTitleTxt = new GUIContent("�༭ɽ������");

        public static MountainNoiseSubWindow GetPopInstance()
        {
            return GetPopInstance<MountainNoiseSubWindow>(confirmPopTitleTxt, verticalWindowSize);
        }

        public MountainNoiseData mountainNoiseData;

        Action<MountainNoiseData> ConfirmEvent;
        Action CancelEvent;

        public override void ShowSubWindow(params object[] args)
        {
            mountainNoiseData = (MountainNoiseData)args[0];
            ConfirmEvent = (Action<MountainNoiseData>)args[1];
            CancelEvent = (Action)args[2];
            base.ShowSubWindow(args);
        }

        [Button("���沢�˳�", ButtonSizes.Medium)]
        private void SaveEdit()
        {
            ConfirmEvent?.Invoke(mountainNoiseData);
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
