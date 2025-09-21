using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    public class MountainEditor : BrushHexmapEditor 
    {
        public override string EditorName => MapEditorEnum.MountainEditor;

        protected override void InitEditor()
        {
            base.InitEditor();
            InitMapSetting();
            Debug.Log("init moutain Editor over !");
        }

        protected override BrushHexmapSetting GetBrushSetting()
        {
            return BrushHexmapSetting.Default;
        }


    }
}
