//
// Texture2D Array Importer for Unity. Copyright (c) 2019 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityTexture2DArrayImportPipeline
//
#pragma warning disable IDE1006, IDE0017
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditorInternal;

namespace Oddworm.EditorFramework
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Texture2DArrayImporter), true)]
    class Texture2DArrayImporterInspector : ScriptedImporterEditor
    {
        class Styles
        {
            public readonly GUIStyle preButton = "RL FooterButton";
            public readonly Texture2D popupIcon = EditorGUIUtility.FindTexture("_Popup");
            public readonly Texture2D errorIcon = EditorGUIUtility.FindTexture("console.erroricon.sml");
            public readonly Texture2D warningIcon = EditorGUIUtility.FindTexture("console.warnicon.sml");
            public readonly GUIContent textureTypeLabel = new GUIContent("Texture Type");
            public readonly GUIContent textureTypeValue = new GUIContent("Texture Array");
            public readonly GUIContent textureShapeLabel = new GUIContent("Texture Shape");
            public readonly GUIContent textureShapeValue = new GUIContent("2D");
            public readonly GUIContent wrapModeLabel = new GUIContent("Wrap Mode", "Select how the Texture behaves when tiled.");
            public readonly GUIContent filterModeLabel = new GUIContent("Filter Mode", "Select how the Texture is filtered when it gets stretched by 3D transformations.");
            public readonly GUIContent anisoLevelLabel = new GUIContent("Aniso Level", "Increases Texture quality when viewing the Texture at a steep angle. Good for floor and ground Textures.");
            public readonly GUIContent anisotropicFilteringDisable = new GUIContent("Anisotropic filtering is disabled for all textures in Quality Settings.");
            public readonly GUIContent anisotropicFilteringForceEnable = new GUIContent("Anisotropic filtering is enabled for all textures in Quality Settings.");
            public readonly GUIContent texturesHeaderLabel = new GUIContent("Textures", "Drag&drop one or multiple textures here to add them to the list.");
            public readonly GUIContent removeItemButton = new GUIContent("", EditorGUIUtility.FindTexture("Toolbar Minus"), "Remove from list.");
        }

        static Styles s_Styles;
        Styles styles
        {
            get
            {
                s_Styles = s_Styles ?? new Styles();
                return s_Styles;
            }
        }

        SerializedProperty m_WrapMode = null;
        SerializedProperty m_FilterMode = null;
        SerializedProperty m_AnisoLevel = null;
        SerializedProperty m_Textures = null;
        ReorderableList m_TextureList = null;

        public override void OnEnable()
        {
            base.OnEnable();

            m_WrapMode = serializedObject.FindProperty("m_WrapMode");
            m_FilterMode = serializedObject.FindProperty("m_FilterMode");
            m_AnisoLevel = serializedObject.FindProperty("m_AnisoLevel");
            m_Textures = serializedObject.FindProperty("m_Textures");

            m_TextureList = new ReorderableList(serializedObject, m_Textures);
            m_TextureList.displayRemove = false;
            m_TextureList.drawElementCallback += OnDrawElement;
            m_TextureList.drawHeaderCallback += OnDrawHeader;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // This is just some visual nonsense to make it look&feel 
            // similar to Unity's Texture Inspector.
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.LabelField(styles.textureTypeLabel, styles.textureTypeValue, EditorStyles.popup);
                EditorGUILayout.LabelField(styles.textureShapeLabel, styles.textureShapeValue, EditorStyles.popup);
                EditorGUILayout.Separator();
            }

            EditorGUILayout.PropertyField(m_WrapMode, styles.wrapModeLabel);
            EditorGUILayout.PropertyField(m_FilterMode, styles.filterModeLabel);
            EditorGUILayout.PropertyField(m_AnisoLevel, styles.anisoLevelLabel);

            // If Aniso is used, check quality settings and displays some info.
            // I've only added this, because Unity is doing it in the Texture Inspector as well.
            if (m_AnisoLevel.intValue > 1)
            {
                if (QualitySettings.anisotropicFiltering == AnisotropicFiltering.Disable)
                    EditorGUILayout.HelpBox(styles.anisotropicFilteringDisable.text, MessageType.Info);

                if (QualitySettings.anisotropicFiltering == AnisotropicFiltering.ForceEnable)
                    EditorGUILayout.HelpBox(styles.anisotropicFilteringForceEnable.text, MessageType.Info);
            }

            // Draw the reorderable texture list only if a single asset is selected.
            // This is to avoid issues drawing the list if it contains a different amount of textures.
            if (!serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.Separator();
                m_TextureList.DoLayoutList();
            }

            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }

        void OnDrawHeader(Rect rect)
        {
            var label = rect; label.width -= 16;
            var popup = rect; popup.x += label.width; popup.width = 20;

            // Display textures list header
            EditorGUI.LabelField(label, styles.texturesHeaderLabel);

            // Show popup button to open a context menu
            using (new EditorGUI.DisabledGroupScope(m_Textures.hasMultipleDifferentValues))
            {
                if (GUI.Button(popup, styles.popupIcon, EditorStyles.label))
                    ShowHeaderPopupMenu();
            }

            // Handle drag&drop on header label
            if (CanAcceptDragAndDrop(label))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (Event.current.type == EventType.DragPerform)
                    AcceptDragAndDrop();
            }
        }

        void ShowHeaderPopupMenu()
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Select Textures"), false, delegate ()
            {
                var importer = target as Texture2DArrayImporter;
                Selection.objects = importer.textures;
            });

            menu.ShowAsContext();
        }


        void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (m_Textures.arraySize <= index)
                return;

            rect.y += 1;
            rect.height -= 2;

            var r = rect;

            var importer = target as Texture2DArrayImporter;
            var textureProperty = m_Textures.GetArrayElementAtIndex(index);

            var errorMsg = importer.GetVerifyString(index);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                r = rect;
                rect.width = 24;
                switch (importer.Verify(index))
                {
                    case Texture2DArrayImporter.VerifyResult.Valid:
                    case Texture2DArrayImporter.VerifyResult.MasterNull:
                        break;

                    default:
                        EditorGUI.LabelField(rect, new GUIContent(styles.errorIcon, errorMsg));
                        break;
                }
                rect = r;
                rect.width -= 24;
                rect.x += 24;
            }
            else
            {

                r = rect;
                rect.width = 24;
                EditorGUI.LabelField(rect, new GUIContent(string.Format("{0}", index), "Slice"), isFocused ? EditorStyles.whiteLabel : EditorStyles.label);
                rect = r;
                rect.width -= 24;
                rect.x += 24;
            }

            r = rect;
            rect.width -= 18;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, textureProperty, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                // We have to apply modification here, so that Texture2DArrayImporter.Verify has the just changed values
                serializedObject.ApplyModifiedProperties();

                // Make sure we assign assets that exist on disk only.
                // During my tests, when selecting built-in assets,
                // Unity reimports the texture array asset infinitely, which is probably an Unity bug.
                var result = importer.Verify(index);
                if (result == Texture2DArrayImporter.VerifyResult.NotAnAsset)
                {
                    textureProperty.objectReferenceValue = null;
                    var msg = importer.GetVerifyString(index);
                    Debug.LogError(msg, importer);
                }
            }

            rect = r;
            rect.x += rect.width - 15;
            rect.y += 2;
            rect.width = 20;
            if (GUI.Button(rect, styles.removeItemButton, styles.preButton))
                textureProperty.DeleteCommand();
        }

        bool CanAcceptDragAndDrop(Rect rect)
        {
            if (!rect.Contains(Event.current.mousePosition))
                return false;

            foreach (var obj in DragAndDrop.objectReferences)
            {
                var tex2d = obj as Texture2D;
                if (tex2d != null)
                    return true;
            }

            return false;
        }

        void AcceptDragAndDrop()
        {
            serializedObject.Update();

            // Add all textures from the drag&drop operation
            foreach (var obj in DragAndDrop.objectReferences)
            {
                var tex2d = obj as Texture2D;
                if (tex2d != null)
                {
                    m_Textures.InsertArrayElementAtIndex(m_Textures.arraySize);
                    var e = m_Textures.GetArrayElementAtIndex(m_Textures.arraySize - 1);
                    e.objectReferenceValue = tex2d;
                }
            }

            serializedObject.ApplyModifiedProperties();
            DragAndDrop.AcceptDrag();
        }
    }
}
