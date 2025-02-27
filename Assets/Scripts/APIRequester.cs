using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

public abstract class APIRequester
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

    /// <summary>
    /// 处理返回数据行的状态
    /// </summary>
    protected enum DispatchResState
    {
        Running,    //继续执行
        Skip,   //跳过本次操作
        End,  //处理结束
    }

    public abstract string Url { get; }

    private HttpClient client;
    private Stream stream;
    private StreamReader reader;
    private double timeout = 20;
    private double tempTime;
    private DateTime lastTime;
    private string fullContent;

    public APIRequester()
    {
        client = new HttpClient();
        fullContent = "";
    }

    /// <summary>
    /// 发送请求
    /// </summary>
    /// <param name="str">输入的内容</param>
    /// <param name="onResStart"></param>
    /// <param name="onResOnce">数据流每返回一次调用一次</param>
    /// <param name="onResEnd"></param>
    public void SendReq(string str, Action onResStart, Action<string> onResOnce, Action onResEnd)
    {
        try
        {
            string json = GetRequestJson(str);
            SendJson(json, onResStart, onResOnce, onResEnd);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// 发送多段文字给ai
    /// </summary>
    /// <param name="strList"></param>
    /// <param name="onResStart"></param>
    /// <param name="onResOnce"></param>
    /// <param name="onResEnd"></param>
    public void SendListReq(string systemStr, List<string> strList, Action onResStart, Action<string> onResOnce, Action onResEnd)
    {
        try
        {
            string json = GetRequestJson(systemStr, strList);
            SendJson(json, onResStart, onResOnce, onResEnd);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private async Task SendJson(string json, Action onResStart, Action<string> onResOnce, Action onResEnd)
    {
        Debug.Log("发送请求..数据==" + json);
        var request = new HttpRequestMessage(HttpMethod.Post, Url);
        SetRequestHeaders(request);

        HttpContent content = new StringContent(json);
        SetHttpContentHeaders(content);
        request.Content = content;

        HttpResponseMessage msg = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        try
        {
            //不是200则直接报错
            if (msg.StatusCode != System.Net.HttpStatusCode.OK)
            {
                string errorContent = await msg.Content.ReadAsStringAsync();
                Debug.LogError($"错误！statusCode=={msg.StatusCode}, 错误消息=={errorContent}");
                return;
            }
            stream = await msg.Content.ReadAsStreamAsync();
            reader = new StreamReader(stream);
            onResStart?.Invoke();

            lastTime = DateTime.Now;
            fullContent = "";
            while (true)
            {
                double deltaTime = (DateTime.Now - lastTime).TotalSeconds;
                tempTime += deltaTime;
                if (tempTime >= timeout)
                {
                    Debug.LogError("处理返回的数据超时！");
                    tempTime = 0;
                    break;
                }
                lastTime = DateTime.Now;

                string resStr = await reader.ReadLineAsync();
                Debug.Log("返回数据==" + resStr);
                DispatchResState dispatchState = DispatchResponse(resStr);
                if (dispatchState == DispatchResState.Skip)
                {
                    continue;
                }
                else if (dispatchState == DispatchResState.End)
                {
                    break;
                }

                string aiChatContent = GetResponseContent(resStr);
                fullContent += aiChatContent;
                tempTime = 0;
                onResOnce?.Invoke(aiChatContent);
            }
            reader.Dispose();
            stream.Dispose();
            OnResponseEnd(fullContent);
            onResEnd?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// 生成需要发送的json
    /// </summary>
    /// <param name="str">对话输入的内容</param>
    /// <returns></returns>
    protected abstract string GetRequestJson(string str);

    /// <summary>
    /// 生成需要发送的json
    /// </summary>
    /// <param name="systemStr">系统提示消息</param>
    /// <param name="strList">多段内容</param>
    /// <returns></returns>
    protected abstract string GetRequestJson(string systemStr, List<string> strList);

    /// <summary>
    /// 设置httpContent的头部
    /// </summary>
    /// <param name="content"></param>
    protected virtual void SetHttpContentHeaders(HttpContent content) 
    {
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
    }

    /// <summary>
    /// 设置requestMessage的头部
    /// </summary>
    /// <param name="message"></param>
    protected virtual void SetRequestHeaders(HttpRequestMessage message) { }

    /// <summary>
    /// 处理数据行
    /// </summary>
    /// <param name="resLine"></param>
    /// <returns></returns>
    protected abstract DispatchResState DispatchResponse(string resLine);

    /// <summary>
    /// 从这一行数据中拿到ai回复的消息
    /// </summary>
    /// <param name="resLine"></param>
    /// <returns></returns>
    protected abstract string GetResponseContent(string resLine);

    /// <summary>
    /// 当处理完所有数据流的数据时调用
    /// </summary>
    protected virtual void OnResponseEnd(string content) { }
}
