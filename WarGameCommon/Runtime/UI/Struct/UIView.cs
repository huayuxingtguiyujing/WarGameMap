using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameCommon {
    /// <summary>
    /// 主要的ui界面 需要继承 UIView
    /// </summary>
    public class UIView : UIWidget {


        public virtual void OnSelect() {
            HandleVisible();
        }

        public virtual void OnDeselect() {

        }


        public override void OnResume() {
            base.OnResume();
            // TODO: 设置uiwidget到最顶部
            transform.SetSiblingIndex(0);
        }

    }
}
