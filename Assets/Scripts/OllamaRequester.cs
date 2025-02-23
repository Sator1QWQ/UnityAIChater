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
    [Serializable]
    public class RequestData
    {
        public string model;
        public string prompt;
        public int[] context;
        public bool stream;
        public bool thinking_enabled;
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

    private static OllamaRequester instance;
    public static OllamaRequester Instance
    {
        get
        {
            if(instance == null)
            {
                instance = new OllamaRequester();
                instance.Init();
            }
            return instance;
        }
    }


    private int[] context;
    private HttpClient client;

    private void Init()
    {
        client = new HttpClient();
    }

    public async Task SendReq(string str, Action<ResponseData> onResOnce)
    {
        string url = "http://localhost:11434/api/generate"; //ollama端口默认11434
        RequestData data = new RequestData()
        {
            model = "deepseek-r1:7b",
            prompt = str,
            context = context,
            stream = true, //建议用流式传输，不然响应比较慢
            thinking_enabled = false
        };
        string json = JsonUtility.ToJson(data);
        HttpContent content = new StringContent(json);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        Debug.Log("发送请求..");
        try
        {
            HttpResponseMessage msg = await client.PostAsync(url, content);
            try
            {
                //不是200则直接报错
                if (msg.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Debug.LogError($"错误！statusCode=={msg.StatusCode}, 错误消息=={msg.Content}");
                    return;
                }
                Stream stream = await msg.Content.ReadAsStreamAsync();

                StreamReader reader = new StreamReader(stream);
                string resStr = "";
                while (resStr != null)
                {
                    resStr = reader.ReadLine();
                    Debug.Log("str==" + resStr);
                    ResponseData res = JsonUtility.FromJson<ResponseData>(resStr);
                    onResOnce?.Invoke(res);
                    await Task.Delay(100);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        catch(Exception e)
        {
            Debug.LogError(e);
        }
    }
}
