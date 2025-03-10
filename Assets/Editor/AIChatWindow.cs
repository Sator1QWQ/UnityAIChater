using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class AIChatWindow : EditorWindow
{
    private string chatInput;
    private GUIStyle inputStyle;
    private GUIStyle buttonStyle;
    private GUIStyle AIChatTitleStyle;  //ai名字
    private GUIStyle defaultChatStyle;
    private GUIStyle defaultSessionStyle;

    private bool isFistGUI;
    private Vector2 chatContentScrollPos;
    private float windowWidth;
    private float windowHeight;
    private Vector2 sessionScrollPos;

    private int sessionWindowWidth = 300;
    private int sessionItemHeight = 40;

    private float chatContentMaxWidth = 500;    //聊天内容的最大宽度
    private int currentSelectSessionId;
    private int lastSelectSessionId;
    private ChatSession session;
    private int sessionId;
    private APIRequester.SendMode sendMode;
    private string codeModeSystemStr = "下面我发送几段代码给你，你需要记住这些代码，得到这些代码后你只需回复收到，不需要回复其他的文字";
    private string codeModeSystemStr2 = "下面我发几段代码给你，你要记住这些代码，我说“结束发送代码”之前，你只能回复“收到”，不能回复其他任何文字，在我说“结束发送代码”后，你才可以回复其他文字，好现在回复收到";
    private List<string> codeList;

    private Dictionary<int, ChatSession> sessionDic = new Dictionary<int, ChatSession>();

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
        currentSelectSessionId = 0;
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

        defaultSessionStyle = new GUIStyle(GUI.skin.button);
        defaultSessionStyle.normal.background = NewTexture(sessionWindowWidth, sessionItemHeight, Color.gray);
        defaultSessionStyle.active.background = NewTexture(sessionWindowWidth, sessionItemHeight, new Color(207 / 255.0f, 207 / 255.0f, 207 / 255.0f));
        defaultSessionStyle.margin.bottom = 5;
        defaultSessionStyle.normal.textColor = Color.black;
    }


    private void OnGUI()
    {
        if(!isFistGUI)
        {
            StyleSetting();
            LoadSessions();
            isFistGUI = true;
        }

        GUILayout.BeginHorizontal();
        DrawSessionPanel();
        if(session != null)
        {
            DrawChatContentPanel();
        }
        else
        {
            GUILayout.Label("需要新增会话...");
        }
        GUILayout.EndHorizontal();
        SaveSize();
    }

    //绘制左侧会话选择区域
    private void DrawSessionPanel()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(sessionWindowWidth), GUILayout.ExpandHeight(true));
        sessionScrollPos = GUILayout.BeginScrollView(sessionScrollPos);
        foreach(ChatSession session in sessionDic.Values)
        {
            DrawSessionItem(session);
        }
        
        GUILayout.EndScrollView();
        GUILayout.FlexibleSpace();
        if(GUILayout.Button("新增对话", GUILayout.Height(50)))
        {
            CreateSession();
        }
        GUILayout.EndVertical();
        GUILayout.Space(10);
    }

    private void DrawSessionItem(ChatSession session)
    {
        GUIStyle style;
        if(session.SessionId == currentSelectSessionId)
        {
            style = new GUIStyle(defaultSessionStyle);
            style.normal.background = NewTexture(sessionWindowWidth, sessionItemHeight, Color.white);
            style.normal.textColor = Color.black;
        }
        else
        {
            style = defaultSessionStyle;
        }
        if (GUILayout.Button(session.SessionName, style, GUILayout.Height(sessionItemHeight)))
        {
            ChangeSession(session.SessionId);
        }
    }

    //绘制对话区域
    private void DrawChatContentPanel()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("对话框");
        GUILayout.FlexibleSpace();
        if(GUILayout.Button("保存对话"))
        {
            SaveSessions();
        }
        if(GUILayout.Button("删除所有会话数据！"))
        {
            DeleteAllSessionData();
        }
        if (GUILayout.Button("刷新界面"))
        {
            RefreshChatOutput();
        }
        GUILayout.EndHorizontal();
        
        DrawChatOutputPanel();
        DrawChatInputPanel();
        GUILayout.EndVertical();
    }

    //绘制对话的输出区域
    private void DrawChatOutputPanel()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(500));
        if(session.ChatDic.Count == 0)
        {
            GUILayout.Label("对话内容空");
        }
        chatContentScrollPos = GUILayout.BeginScrollView(chatContentScrollPos);
        foreach(KeyValuePair<int, ChatData> data in session.ChatDic)
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
        if(sendMode == APIRequester.SendMode.UploadCode)
        {
            if (GUILayout.Button("结束读取", buttonStyle, GUILayout.Width(80)))
            {
                session.Requester.ChangeSendMode(APIRequester.SendMode.Chat);
                int id = session.CreateChatId();
                int resChatId = session.CreateChatId();
                ChatData data = session.AddChatData(id, true, "代码发送");
                OnChatContentChange(data, session);
                session.Requester.SendListReq(codeModeSystemStr, codeList, () =>
                {
                    ChatData aiData = session.AddChatData(resChatId, false, "");
                    OnChatContentChange(aiData, session);
                }, data =>
                {
                    string str = data;
                    ChatData aiData = session.ChatDic[resChatId];
                    aiData.content += str;
                    OnChatContentChange(aiData, session);
                }, null);
                sendMode = APIRequester.SendMode.Chat;
            }
        }
        else
        {
            if (GUILayout.Button("读取代码", buttonStyle, GUILayout.Width(80)))
            {
                codeList = new List<string>();
                string[] filePaths = Directory.GetFiles(@"E:\UnityProject\2021.3\UnityAIChater\Assets", ".cs");
                

            }
            if(sendMode == APIRequester.SendMode.UploadFile)
            {
                if(GUILayout.Button("结束上传", buttonStyle, GUILayout.Width(80)))
                {
                    session.Requester.ChangeSendMode(APIRequester.SendMode.Chat);
                    sendMode = APIRequester.SendMode.Chat;
                }
            }
            else
            {
                if (GUILayout.Button("上传文件", buttonStyle, GUILayout.Width(80)))
                {
                    session.Requester.ChangeSendMode(APIRequester.SendMode.UploadFile);
                    sendMode = APIRequester.SendMode.UploadFile;
                }
            }
        }
        
        if(GUILayout.Button("确定", buttonStyle, GUILayout.Width(80)))
        {
            if(!string.IsNullOrEmpty(chatInput) && !string.IsNullOrEmpty(chatInput.Trim()))
            {                
                if (sendMode != APIRequester.SendMode.UploadCode)
                {
                    int id = session.CreateChatId();
                    ChatData data = session.AddChatData(id, true, chatInput);
                    OnChatContentChange(data, session);
                    int currentId = currentSelectSessionId;
                    ChatSession tempSession = sessionDic[currentId];
                    int resChatId = tempSession.CreateChatId();
                    Debug.Log("发送请求..当前发送请求的会话id==" + currentId);

                    void OnResStart()
                    {
                        ChatData aiData = session.AddChatData(resChatId, false, "");  //此时还没开始回复内容
                        OnChatContentChange(aiData, session);
                    }

                    void OnResOnce(string data)
                    {
                        string str = data;
                        ChatData aiData = session.ChatDic[resChatId];
                        aiData.content += str;
                        OnChatContentChange(aiData, session);
                    }

                    tempSession.Requester.SendReq(chatInput, OnResStart, OnResOnce, null);
                }
                else if(sendMode == APIRequester.SendMode.UploadCode)
                {
                    Debug.Log("发送代码");
                    codeList.Add(chatInput);
                }
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
            GUILayout.TextArea(content, session.ChatGUIDic[id].style, GUILayout.Width(width));
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("AI", AIChatTitleStyle);
            GUILayout.TextArea(content, session.ChatGUIDic[id].style, GUILayout.Width(500));
        }
        
        GUILayout.EndVertical();
    }

    private void SaveSize()
    {
        windowWidth = position.width;
        windowHeight = position.height;
        EditorPrefs.SetFloat("AIChatWindow_Width", windowWidth);
        EditorPrefs.SetFloat("AIChatWindow_Height", windowHeight);
    }

    private void SaveSessions()
    {
        EditorPrefs.SetInt("AIChatWindow_SessionId", sessionId);
        EditorPrefs.SetInt("AIChatWindow_SessionCount", sessionDic.Count);
        int i = 0;
        foreach(KeyValuePair<int, ChatSession> pairs in sessionDic)
        {
            EditorPrefs.SetInt("AIChatWindow_SessionList_" + i, pairs.Key);
            pairs.Value.Save();
            i++;
        }
        Debug.Log("会话总数据保存完成");
    }

    private void LoadAll()
    {
        windowWidth = EditorPrefs.GetFloat("AIChatWindow_Width");
        windowHeight = EditorPrefs.GetFloat("AIChatWindow_Height");
    }

    private void LoadSessions()
    {
        sessionDic = new Dictionary<int, ChatSession>();
        int count = EditorPrefs.GetInt("AIChatWindow_SessionCount");
        for(int i = 0; i < count; i++)
        {
            int id = EditorPrefs.GetInt("AIChatWindow_SessionList_" + i);
            ChatSession session = new ChatSession(id, this);
            session.Load();
            sessionDic.Add(id, session);
        }
        sessionId = EditorPrefs.GetInt("AIChatWindow_SessionId");
    }

    //清除所有会话数据
    private void DeleteAllSessionData()
    {
        int count = EditorPrefs.GetInt("AIChatWindow_SessionCount");
        for (int i = 0; i < count; i++)
        {
            int id = EditorPrefs.GetInt("AIChatWindow_SessionList_" + i);
            sessionDic[id].DeleteSaveData();
        }
        EditorPrefs.DeleteKey("AIChatWindow_SessionCount");
        EditorPrefs.DeleteKey("AIChatWindow_SessionId");
        Debug.Log("全部会话数据删除完成");
    }

    //聊天内容变化时，可用来重置style
    public void OnChatContentChange(ChatData data, ChatSession session)
    {
        ChatGUIData gui = session.ChatGUIDic[data.chatId];
        RefreshChatGUI(data, gui);
        Repaint();
    }

    public void RefreshChatGUI(ChatData data, ChatGUIData gui)
    {
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

        Vector2 size = GetContentSize(gui.style, data.content, chatContentMaxWidth);
        gui.style.normal.background = gui.lastTexture == null ? NewTexture((int)size.x, (int)size.y, color) : ResetTexture(gui.lastTexture, (int)size.x, (int)size.y, color);
        gui.lastTexture = gui.style.normal.background;
    }

    //获得内容的尺寸
    private Vector2 GetContentSize(GUIStyle style, string content, float maxWidth)
    {
        Vector2 size = style.CalcSize(new GUIContent(content));
        if (size.x > maxWidth)
        {
            size.y = style.CalcHeight(new GUIContent(content), maxWidth);
        }
        return size;
    }

    //获取一个新的贴图
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

    private void RefreshChatOutput()
    {
        foreach(ChatData item in session.ChatDic.Values)
        {
            OnChatContentChange(item, session);
        }
    }

    private int CreateSessionId()
    {
        sessionId++;
        lastSelectSessionId = sessionId;
        return sessionId;
    }

    private void CreateSession()
    {
        int id = CreateSessionId();
        ChatSession session = new ChatSession(id, this);
        sessionDic.Add(id, session);
        this.session = session;
        currentSelectSessionId = id;
        session.SetSessionName("会话" + sessionDic.Count);
    }

    private void ChangeSession(int sessionId)
    {
        session = sessionDic[sessionId];
        if(lastSelectSessionId != 0 && sessionId != lastSelectSessionId)
        {
            OnSessionChange(sessionDic[lastSelectSessionId]);
        }
        currentSelectSessionId = sessionId;
        lastSelectSessionId = currentSelectSessionId;
    }

    private void OnSessionChange(ChatSession lastSession)
    {

    }
}
