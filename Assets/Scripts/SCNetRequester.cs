using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;

/// <summary>
/// 超算互联网 www.scnet.cn
/// </summary>
public class SCNetRequester : APIRequester
{
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
            public string finish_reason;
        }
        public int index;
        public Delta delta;
        public string finish_reason;
    }

    private List<Message> messages;

    public SCNetRequester() : base()
    {
        messages = new List<Message>();
    }

    public override string Url => "https://api.scnet.cn/api/llm/v1/chat/completions";
    private string APIKey = "sk-MzE3LTExNjgzMDA1NTA4LTE3NDA0NTExMzA1Mjk=";

    protected override string GetRequestJson(string str)
    {
        messages.Add(new Message()
        {
            role = "user",
            content = str
        });
        RequestData data = new RequestData()
        {
            model = "DeepSeek-R1-Distill-Qwen-32B",
            messages = messages.ToArray(),
            stream = true
        };
        string json = JsonUtility.ToJson(data);
        return json;
    }

    protected override DispatchResState DispatchResponse(string resLine)
    {
        if (string.IsNullOrEmpty(resLine))
        {
            return DispatchResState.Skip;
        }

        string resJson = resLine.Replace("data:", "");
        ResponseData data = JsonUtility.FromJson<ResponseData>(resJson);
        if(data.choices[0].finish_reason.Equals("STOP"))
        {
            return DispatchResState.End;
        }

        return DispatchResState.Running;
    }

    protected override string GetResponseContent(string resLine)
    {
        string resJson = resLine.Replace("data:", "");
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

    protected override string GetRequestJson(string systemStr, List<string> str)
    {
        for (int i = 0; i < str.Count; i++)
        {
            messages.Add(new Message()
            {
                role = "system",
                content = systemStr
            });
            messages.Add(new Message()
            {
                role = "user",
                content = str[i]
            });
        }
        RequestData data = new RequestData()
        {
            model = "DeepSeek-R1-Distill-Qwen-32B",
            messages = messages.ToArray(),
            stream = true
        };
        string json = JsonUtility.ToJson(data);
        return json;
    }
}
