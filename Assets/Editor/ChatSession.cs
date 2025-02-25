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
    public APIRequester Requester { get; private set; }

    public Dictionary<int, ChatData> ChatDic { get; private set; }
    public Dictionary<int, ChatGUIData> ChatGUIDic { get; private set; }

    public AIChatWindow window;
    private int chatId;
    
    public ChatSession(int sessionId, AIChatWindow window)
    {
        SessionId = sessionId;
        Requester = new SCNetRequester();
        ChatDic = new Dictionary<int, ChatData>();
        ChatGUIDic = new Dictionary<int, ChatGUIData>();
        this.window = window;
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
        EditorPrefs.SetInt("Session_" + SessionId + "_ChatDicCount", ChatDic.Count);
        int i = 0;
        foreach(KeyValuePair<int, ChatData> pairs in ChatDic)
        {
            EditorPrefs.SetInt("Session_" + SessionId + "_ChatDic_" + i + "_chatId", pairs.Value.chatId);
            EditorPrefs.SetBool("Session_" + SessionId + "_ChatDic_" + i + "_isMy", pairs.Value.isMy);
            EditorPrefs.SetString("Session_" + SessionId + "_ChatDic_" + i + "_content", pairs.Value.content);
            i++;
        }
        Debug.Log($"会话{SessionId}保存完成");
    }

    public void Load()
    {
        SessionName = EditorPrefs.GetString("SessionName_" + SessionId);
        ChatDic = new Dictionary<int, ChatData>();
        int chatCount = EditorPrefs.GetInt("Session_" + SessionId + "_ChatDicCount");
        for(int i = 0; i < chatCount; i++)
        {
            int chatId = EditorPrefs.GetInt("Session_" + SessionId + "_ChatDic_" + i + "_chatId");
            bool isMy = EditorPrefs.GetBool("Session_" + SessionId + "_ChatDic_" + i + "_isMy");
            string content = EditorPrefs.GetString("Session_" + SessionId + "_ChatDic_" + i + "_content");
            AddChatData(chatId, isMy, content);
            window.RefreshChatGUI(ChatDic[chatId], ChatGUIDic[chatId]);
        }
    }

    //清除所有已经保存的数据
    public void DeleteSaveData()
    {
        EditorPrefs.DeleteKey("SessionName_" + SessionId);
        int chatCount = EditorPrefs.GetInt("Session_" + SessionId + "_ChatDicCount");
        for (int i = 0; i < chatCount; i++)
        {
            EditorPrefs.DeleteKey("Session_" + SessionId + "_ChatDic_" + i + "_chatId");
            EditorPrefs.DeleteKey("Session_" + SessionId + "_ChatDic_" + i + "_isMy");
            EditorPrefs.DeleteKey("Session_" + SessionId + "_ChatDic_" + i + "_content");
        }
        EditorPrefs.DeleteKey("Session_" + SessionId + "_ChatDicCount");
        Debug.Log("会话" + SessionId + "数据删除完成");
    }
}
