using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class DeepseekRequester : APIRequester
{
    private enum OperateState
    {
        /// <summary>
        /// 空闲
        /// </summary>
        Idle,

        /// <summary>
        /// 发送消息中
        /// </summary>
        Sending,
        
        /// <summary>
        /// 消息返回中
        /// </summary>
        Responsing,
    }

    [Serializable]
    public class RequestData
    {
        public string model;
        public Message[] messages;
        public bool stream;
    }

    [Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class ResponseData
    {
        public string id;
        public string model;
        public long created;
        public string system_fingerprint;
        public Choices[] choices;
    }

    [Serializable]
    public class Choices
    {
        [Serializable]
        public class Delta
        {
            public string role;
            public string content;
        }
        public int index;
        public Delta delta;
        public string finish_reason;
    }

    private List<Message> messages;

    public DeepseekRequester() : base()
    {
        messages = new List<Message>();
    }

    public override string Url => "https://api.deepseek.com/chat/completions";

    public string APIKey => "sk-5e363a5790154f639004e53ac5eacb30";

    protected override string GetRequestJson(string str)
    {
        messages.Add(new Message()
        {
            role = "user",
            content = str
        });
        RequestData data = new RequestData()
        {
            model = "deepseek-chat",
            messages = messages.ToArray(),
            stream = true
        };
        string json = JsonUtility.ToJson(data);
        return json;
    }

    protected override DispatchResState DispatchResponse(string resLine)
    {
        if(string.IsNullOrEmpty(resLine))
        {
            return DispatchResState.Skip;
        }

        string resJson = resLine.Replace("data: ", "");
        if(resJson == "[DONE]")
        {
            return DispatchResState.End;
        }

        return DispatchResState.Running;
    }

    protected override string GetResponseContent(string resLine)
    {
        string resJson = resLine.Replace("data: ", "");
        ResponseData data = JsonUtility.FromJson<ResponseData>(resJson);
        string result = data.choices[0].delta.content;
        return result;
    }

    protected override void SetRequestHeaders(HttpRequestMessage message)
    {
        message.Headers.Add("Authorization", $"Bearer {APIKey}");
    }

    protected override void OnResponseEnd(string content)
    {
        messages.Add(new Message()
        {
            role = "assistant",
            content = content
        });
    }

    protected override string GetRequestJson(string systemStr, List<string> strList)
    {
        for (int i = 0; i < strList.Count; i++)
        {
            messages.Add(new Message()
            {
                role = "system",
                content = systemStr
            });
            messages.Add(new Message()
            {
                role = "user",
                content = strList[i]
            });
        }
        RequestData data = new RequestData()
        {
            model = "deepseek-chat",
            messages = messages.ToArray(),
            stream = true
        };
        string json = JsonUtility.ToJson(data);
        return json;
    }
}
