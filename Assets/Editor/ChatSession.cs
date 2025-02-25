using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ChatData
{
    public int chatId;
    public bool isMy;
    public string content;
}

public class ChatGUIData
{
    public GUIStyle style;
    public Texture2D lastTexture;
}

/// <summary>
/// 聊天会话
/// </summary>
public class ChatSession
{
    public int SessionId { get; private set; }
    public string SessionName { get; private set; }
    public int[] ContextList { get; private set; }  //上下文
    public DeepseekRequester Requester { get; private set; }

    public Dictionary<int, ChatData> ChatDic { get; private set; }
    public Dictionary<int, ChatGUIData> ChatGUIDic { get; private set; }
    private int chatId;
    
    public ChatSession(int sessionId)
    {
        SessionId = sessionId;
        Requester = new DeepseekRequester();
        ChatDic = new Dictionary<int, ChatData>();
        ChatGUIDic = new Dictionary<int, ChatGUIData>();
    }

    public void SetSessionName(string name)
    {
        SessionName = name;
    }

    public ChatData AddChatData(int chatId, bool isMy, string content)
    {
        ChatData data = new ChatData();
        data.chatId = chatId;
        data.isMy = isMy;
        data.content = content;
        ChatDic.Add(chatId, data);
        ChatGUIData gui = new ChatGUIData();
        ChatGUIDic.Add(chatId, gui);
        return data;
    }
    
    public int CreateChatId()
    {
        chatId++;
        return chatId;
    }

    public void Save()
    {
        EditorPrefs.SetString("SessionName_" + SessionId, SessionName);
        EditorPrefs.SetInt("ContextListCount_" + SessionId, ContextList.Length);
        for(int i = 0; i < ContextList.Length; i++)
        {
            EditorPrefs.SetInt("ContextList_" + SessionId + "_" + i, ContextList[i]);
        }
    }

    public void Load()
    {
        SessionName = EditorPrefs.GetString("SessionName_" + SessionId);
        int count = EditorPrefs.GetInt("ContextListCount_" + SessionId);
        ContextList = new int[count];
        for(int i = 0; i < count; i++)
        {
            ContextList[i] = EditorPrefs.GetInt("ContextList_" + SessionId + "_" + i);
        }
    }
}
