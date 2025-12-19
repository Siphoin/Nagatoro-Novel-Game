using UnityEditor;
using UnityEngine;
using XNodeEditor;
using SNEngine.Audio.Music;
using SNEngine.Editor;
using System.Linq;

namespace SNEngine.Audio
{
    [CustomNodeEditor(typeof(SetPlaylistMusicNode))]
    public class SetPlaylistMusicNodeEditor : NodeEditor
    {
        public override void OnBodyGUI()
        {
            serializedObject.Update();

            string[] excludes = { "m_Script", "graph", "position", "ports", "_input" };

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (excludes.Contains(iterator.name)) continue;
                NodeEditorGUILayout.PropertyField(iterator, true);
            }

            DrawAudioList();

            foreach (XNode.NodePort dynamicPort in target.DynamicPorts)
            {
                if (NodeEditorGUILayout.IsDynamicPortListPort(dynamicPort)) continue;
                NodeEditorGUILayout.PortField(dynamicPort);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAudioList()
        {
            SerializedProperty inputProp = serializedObject.FindProperty("_input");
            XNode.NodePort port = target.GetPort("_input");

            GUILayout.BeginVertical(EditorStyles.helpBox);

            Rect headerRect = EditorGUILayout.GetControlRect();
            NodeEditorGUILayout.PortField(headerRect.position, port);

            EditorGUI.LabelField(new Rect(headerRect.x + 20, headerRect.y, headerRect.width - 20, headerRect.height),
                "Playlist (" + inputProp.arraySize + ")", EditorStyles.boldLabel);

            if (inputProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Playlist is empty", MessageType.None);
            }
            else
            {
                for (int i = 0; i < inputProp.arraySize; i++)
                {
                    SerializedProperty element = inputProp.GetArrayElementAtIndex(i);
                    EditorGUILayout.BeginHorizontal();

                    Rect fieldRect = EditorGUILayout.GetControlRect(true);

                    AudioClip currentClip = element.objectReferenceValue as AudioClip;
                    string displayName = currentClip != null ? currentClip.name : "None (AudioClip)";

                    if (GUI.Button(fieldRect, displayName, EditorStyles.objectField))
                    {
                        int index = i;
                        AudioClipSelectorWindow.Open((selectedClip) =>
                        {
                            element.serializedObject.Update();
                            element.serializedObject.FindProperty("_input").GetArrayElementAtIndex(index).objectReferenceValue = selectedClip;
                            element.serializedObject.ApplyModifiedProperties();
                        });
                    }

                    if (GUILayout.Button("x", GUILayout.Width(20)))
                    {
                        inputProp.DeleteArrayElementAtIndex(i);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (GUILayout.Button("+ Add Clip", EditorStyles.miniButton))
            {
                inputProp.arraySize++;
            }

            GUILayout.EndVertical();
        }
    }
}