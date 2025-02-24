using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AIChatWindow : EditorWindow
{
    private class ChatData
    {
        public int chatId;
        public bool isMy;
        public string content;
    }

    private class ChatGUIData
    {
        public GUIStyle style;
        public Texture2D background;
    }

    private string chatInput;
    private GUIStyle inputStyle;
    private GUIStyle buttonStyle;
    private GUIStyle AIChatTitleStyle;  //ai名字
    private GUIStyle defaultChatStyle;

    private bool isFistGUI;
    private Vector2 chatContentScrollPos;
    private float windowWidth;
    private float windowHeight;

    private float chatContentMaxWidth = 500;    //聊天内容的最大宽度

    private Dictionary<int, ChatData> chatDic = new Dictionary<int, ChatData>();
    private Dictionary<int, ChatGUIData> chatGUIDic = new Dictionary<int, ChatGUIData>();
    private int tempId;

    [MenuItem("AI/聊天界面")]
    private static void Create()
    {
        AIChatWindow window = CreateWindow<AIChatWindow>();
        window.LoadAll();
        if(window.windowWidth == 0)
        {
            window.windowWidth = 800;
        }
        if(window.windowHeight == 0)
        {
            window.windowHeight = 500;
        }
        window.position = new Rect(Screen.width / 2, Screen.height / 2, window.windowWidth, window.windowHeight);
    }

    private void OnEnable()
    {
        isFistGUI = false;
    }

    //设置style
    private void StyleSetting()
    {
        inputStyle = new GUIStyle(GUI.skin.textArea);
        inputStyle.padding.top = 5;
        inputStyle.fontSize = 15;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.padding = new RectOffset(10, 10, 5, 5);

        AIChatTitleStyle = new GUIStyle(GUI.skin.label);
        AIChatTitleStyle.fontSize = 13;
        AIChatTitleStyle.fontStyle = FontStyle.Bold;

        defaultChatStyle = new GUIStyle(GUI.skin.label);
        defaultChatStyle.fontSize = 15;
        defaultChatStyle.wordWrap = true;
        defaultChatStyle.alignment = TextAnchor.MiddleRight; //右对齐
    }


    private void OnGUI()
    {
        if(!isFistGUI)
        {
            StyleSetting();
            isFistGUI = true;
        }

        GUILayout.BeginHorizontal();
        DrawSessionPanel();
        DrawChatContentPanel();
        GUILayout.EndHorizontal();
        SaveAll();
    }

    //绘制左侧会话选择区域
    private void DrawSessionPanel()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(300), GUILayout.ExpandHeight(true));
        GUILayout.Label("功能暂未实现。。");
        GUILayout.EndVertical();
        GUILayout.Space(10);
    }

    //绘制对话区域
    private void DrawChatContentPanel()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("对话框");
        DrawChatOutputPanel();
        DrawChatInputPanel();
        GUILayout.EndVertical();
    }

    //绘制对话的输出区域
    private void DrawChatOutputPanel()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(500));
        GUILayout.Label("..");
        chatContentScrollPos = GUILayout.BeginScrollView(chatContentScrollPos);
        foreach(KeyValuePair<int, ChatData> data in chatDic)
        {
            DrawChatItem(data.Value.chatId, data.Value.isMy, data.Value.content);
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    //绘制对话输入区域
    private void DrawChatInputPanel()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(300));
        chatInput = GUILayout.TextArea(chatInput, inputStyle, GUILayout.Height(200));

        //对话框下方的按钮
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if(GUILayout.Button("确定", buttonStyle, GUILayout.Width(80)))
        {
            if(!string.IsNullOrEmpty(chatInput) && !string.IsNullOrEmpty(chatInput.Trim()))
            {
                int id = CreateChatID();
                ChatData data = AddChatData(id, true, chatInput);
                OnChatContentChange(data);
                int resId = 0;
                OllamaRequester.Instance.SendReq(chatInput,() =>
                {
                    resId = CreateChatID();
                    ChatData aiData = AddChatData(resId, false, "");  //此时还没开始回复内容
                    OnChatContentChange(aiData);
                }, data =>
                {
                    string str = data.response;
                    ChatData aiData = chatDic[resId];
                    aiData.content += str;
                    OnChatContentChange(aiData);
                }, RefreshChatOutput).ContinueWith(t => { });
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    //绘制一个聊天item
    private void DrawChatItem(int id, bool isMy, string content)
    {
        GUILayout.BeginVertical();
        if (isMy)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            //字体颜色
            Vector2 size = defaultChatStyle.CalcSize(new GUIContent(content));
            float width = Mathf.Min(size.x, chatContentMaxWidth);
            GUILayout.TextArea(content, chatGUIDic[id].style, GUILayout.Width(width));
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("AI", AIChatTitleStyle);
            GUILayout.TextArea(content, chatGUIDic[id].style, GUILayout.Width(500));
        }
        
        GUILayout.EndVertical();
    }

    private void SaveAll()
    {
        windowWidth = position.width;
        windowHeight = position.height;
        EditorPrefs.SetFloat("AIChatWindow_Width", windowWidth);
        EditorPrefs.SetFloat("AIChatWindow_Height", windowHeight);
    }

    private void LoadAll()
    {
        windowWidth = EditorPrefs.GetFloat("AIChatWindow_Width");
        windowHeight = EditorPrefs.GetFloat("AIChatWindow_Height");
    }

    //聊天内容变化时，可用来重置style
    private void OnChatContentChange(ChatData data)
    {
        ChatGUIData gui = chatGUIDic[data.chatId];
        gui.style = new GUIStyle(defaultChatStyle);
        Color color;
        if (data.isMy)
        {
            color = Color.white;
            color.a = 0.7f;
            gui.style.normal.textColor = Color.blue;
        }
        else
        {
            color = Color.white;
            color.a = 0.7f;
            gui.style.normal.textColor = new Color(24 / 255.0f, 142 / 255.0f, 0);
            gui.style.alignment = TextAnchor.MiddleLeft;
        }

        Vector2 size = gui.style.CalcSize(new GUIContent(data.content));
        float height;
        if (size.x > chatContentMaxWidth)
        {
            height = gui.style.CalcHeight(new GUIContent(data.content), chatContentMaxWidth);
        }
        else
        {
            height = size.y;
        }
        Texture2D lastTexture = gui.style.normal.background;
        gui.style.normal.background = lastTexture == null ? NewTexture((int)size.x, (int)height, color) : ResetTexture(lastTexture, (int)size.x, (int)height, color);
        Repaint();
    }

    private Texture2D NewTexture(int width, int height, Color color)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] colors = new Color[width * height];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = color;
        }
        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    private Texture2D ResetTexture(Texture2D texture, int width, int height, Color color)
    {
        Debug.Log("重置贴图");
        bool isOk = texture.Reinitialize(width, height);
        if(!isOk)
        {
            Debug.LogError("设置贴图尺寸失败！");
            return null;
        }

        Color[] colors = new Color[width * height];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = color;
        }
        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    private int CreateChatID()
    {
        tempId++;
        return tempId;
    }

    private ChatData AddChatData(int chatId, bool isMy, string content)
    {
        ChatData data = new ChatData();
        data.chatId = chatId;
        data.isMy = isMy;
        data.content = content;
        chatDic.Add(chatId, data);

        ChatGUIData gui = new ChatGUIData();
        chatGUIDic.Add(chatId, gui);
        return data;
    }

    private void RefreshChatOutput()
    {
        foreach(ChatData item in chatDic.Values)
        {
            OnChatContentChange(item);
        }
    }
}
