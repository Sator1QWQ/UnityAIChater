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

public class DeepseekRequester
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
        public string id;
        public string model;
        public Message[] messages;
        public int[] context;
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

    private int[] context;
    private HttpClient client;
    private Stream stream;
    private StreamReader reader;
    private OperateState state;
    private bool isCancel;
    private string apiKey = "sk-5e363a5790154f639004e53ac5eacb30";
    private string id; //这次会话的id

    public DeepseekRequester()
    {
        client = new HttpClient();
        state = OperateState.Idle;
    }

    /// <summary>
    /// 发送请求
    /// </summary>
    /// <param name="str">文本</param>
    /// <param name="onResStart">返回时调用</param>
    /// <param name="onResOnce">每次读取到流数据时调用</param>
    /// <returns></returns>
    public async Task SendReq(string str, Action onResStart, Action<string> onResOnce, Action onResEnd)
    {
        string url = "https://api.deepseek.com/chat/completions";
        RequestData data = new RequestData()
        {
            id = id,
            model = "deepseek-chat",
            messages = new Message[]
            {
                new Message()
                {
                    role = "user",
                    content = str
                }
            },
            context = context,
            stream = true, //建议用流式传输，不然响应比较慢
        };
        
        try
        {
            string json = JsonUtility.ToJson(data);
            HttpContent content = new StringContent(json);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            Debug.Log("发送请求..数据==" + json);
            state = OperateState.Sending;

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", "Bearer " + apiKey);
            request.Content = content;
            HttpResponseMessage msg = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            try
            {
                state = OperateState.Responsing;
                if(isCancel)
                {
                    Debug.Log("请求被取消");
                    state = OperateState.Idle;
                    isCancel = false;
                    return;
                }

                //不是200则直接报错
                if (msg.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Debug.LogError($"错误！statusCode=={msg.StatusCode}, 错误消息=={msg.Content}");
                    isCancel = false;
                    state = OperateState.Idle;
                    return;
                }
                stream = await msg.Content.ReadAsStreamAsync();
                reader = new StreamReader(stream);
                onResStart?.Invoke();

                while (true)
                {
                    if(isCancel)
                    {
                        if(reader != null)
                        {
                            reader.Dispose();
                            stream.Dispose();
                        }
                        state = OperateState.Idle;
                        Debug.Log("在生成文本时请求被取消");
                        isCancel = false;
                        return;
                    }
                    string resStr = await reader.ReadLineAsync();
                    
                    //会读取到空行
                    if(string.IsNullOrEmpty(resStr))
                    {
                        continue;
                    }

                    string resJson = resStr.Replace("data: ", "");

                    //deepseek结束的条件
                    if(resJson.Equals("[DONE]"))
                    {
                        break;
                    }
                    Debug.Log("str==" + resStr);
                    ResponseData res = JsonUtility.FromJson<ResponseData>(resJson);
                    id = res.id;
                    onResOnce?.Invoke(res.choices[0].delta.content);
                }
                onResEnd?.Invoke();
                reader.Dispose();
                stream.Dispose();
                state = OperateState.Idle;
                isCancel = false;
            }
            catch (Exception e)
            {
                state = OperateState.Idle;
                isCancel = false;
                Debug.LogError(e);
            }
        }
        catch(Exception e)
        {
            state = OperateState.Idle;
            isCancel = false;
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// 取消发送请求
    /// </summary>
    public void CancelReq()
    {
        if(state == OperateState.Idle)
        {
            return;
        }

        isCancel = true;
        Debug.Log("准备取消");
    }
}
