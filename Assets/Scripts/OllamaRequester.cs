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

public class OllamaRequester
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
        public string prompt;
        public int[] context;
        public bool stream;
    }

    [Serializable]
    public class ResponseData
    {
        public string model;
        public string created_at;
        public string response;
        public bool done;
        public string done_reason;
        public int[] context;
        public long total_duration;
        public long load_duration;
    }

    private int[] context;
    private HttpClient client;
    private Stream stream;
    private StreamReader reader;
    private OperateState state;
    private bool isCancel;

    public OllamaRequester()
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
    public async Task SendReq(string str, Action onResStart, Action<ResponseData> onResOnce, Action onResEnd)
    {
        string url = "http://localhost:11434/api/generate"; //ollama端口默认11434
        RequestData data = new RequestData()
        {
            model = "deepseek-r1:1.5b",
            prompt = str,
            context = context,
            stream = true, //建议用流式传输，不然响应比较慢
        };
        string json = JsonUtility.ToJson(data);
        HttpContent content = new StringContent(json);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        Debug.Log("发送请求..数据==" + json);
        state = OperateState.Sending;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
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
                    Debug.Log("str==" + resStr);
                    ResponseData res = JsonUtility.FromJson<ResponseData>(resStr);
                    onResOnce?.Invoke(res);
                    if (res.done)
                    {
                        context = res.context;
                        break;
                    }
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
