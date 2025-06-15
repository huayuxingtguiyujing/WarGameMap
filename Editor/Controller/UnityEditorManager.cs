
using UnityEditor;
using static UnityEditor.EditorApplication;

namespace LZ.WarGameMap.MapEditor
{
    [InitializeOnLoad]
    public static class UnityEditorManager {

        static bool IsEditorUpdating = false;

        static UnityEditorManager() {
            IsEditorUpdating = false;
        }

        public static void RegisterUpdate(CallbackFunction unityAction) {
            if (!IsEditorUpdating) {
                IsEditorUpdating = true;
                EditorApplication.update += unityAction;
            }
        }

        public static void UnregisterUpdate(CallbackFunction unityAction) {
            EditorApplication.update -= unityAction;
        }

    }
}
