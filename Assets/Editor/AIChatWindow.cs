using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AIChatWindow : EditorWindow
{
    [MenuItem("AI/聊天界面")]
    private static void Create()
    {
        AIChatWindow window = CreateWindow<AIChatWindow>();
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 800, 500);
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        DrawSessionPanel();
        DrawChatContentPanel();
        GUILayout.EndHorizontal();
    }

    private void DrawSessionPanel()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(300), GUILayout.ExpandHeight(true));
        GUILayout.Label("功能咱未实现。。");
        GUILayout.EndVertical();
        GUILayout.Space(10);
    }

    private void DrawChatContentPanel()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("对话框");
        DrawChatOutputPanel();
        DrawChatInputPanel();
        GUILayout.EndVertical();
    }

    private void DrawChatOutputPanel()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(500));
        GUILayout.Label("..");
        GUILayout.EndVertical();
    }

    private void DrawChatInputPanel()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(300));
        
        GUILayout.EndVertical();
    }
}
