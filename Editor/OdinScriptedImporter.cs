#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.AssetImporters;
namespace Swizzler {
    public class OdinScriptedImporter : ScriptedImporterEditor {
        protected void EnsureEditor() {
            if (tree == null)
                tree = PropertyTree.Create(serializedObject);
        }

        protected PropertyTree tree;

        public override void OnEnable() {
            base.OnEnable();

            EnsureEditor();
        }

        public override void OnDisable() {
            if (tree != null) {
                tree.Dispose();
                tree = null;
            }

            base.OnDisable();
        }

        public override void OnInspectorGUI() {
            EnsureEditor();

            tree.Draw();

            ApplyRevertGUI();

            GUIHelper.RepaintIfRequested(this);
        }
    }
}
#endif
