using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameCommon {
    public class UIWidget : BaseBehaviour {

        [Space] [Header("BasePopUI")] [SerializeField]
        protected CanvasGroup m_CanvasGroup;

        private bool visible = false;
        public bool Visible { get => visible;}

        #region 设置可见
        public virtual void Show() {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
            visible = true;
        }

        public virtual void Hide() {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
            visible = false;
        }

        public virtual void HandleVisible() {
            if (visible) {
                Hide();
            } else {
                Show();
            }
        }

        #endregion

        #region 组件生命周期

        public virtual void OnEnter() {

        }

        public virtual void OnPause() {

        }

        public virtual void OnResume() {

        }

        public virtual void OnExit() {

        }

        #endregion

    }
}
