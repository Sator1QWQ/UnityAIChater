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
        public int[] context;
    }

    [Serializable]
    public class RequestData2
    {
        public string model;
        public Message[] messages;
        public bool stream;

        public class Message
        {
            public string role;
            public string content;
        }
    }

    public string[] inputs;
    private int[] context;

    private HttpClient client;

    private static OllamaRequester instance;
    public static OllamaRequester Instance
    {
        get
        {
            if(instance == null)
            {
                instance = new OllamaRequester();
                instance.Awake();
            }
            return instance;
        }
    }

    private void Awake()
    {
        client = new HttpClient();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.K))
        {
            //StartCoroutine(Connect("你好", () =>
            //{
            //    StartCoroutine(Connect("print(\"Hello World\")这个lua语言的", () =>
            //    {
            //        StartCoroutine(Connect("根据这个语言再生成一段对应语言的代码", () =>
            //        {
            //            StartCoroutine(Connect("解释下你刚才写的这段代码", null));
            //        }));
            //    }));
            //}));

            //int i = 0;
            //string text = inputs[i];
            //void Run(string str)
            //{
            //    StartCoroutine(Connect(str, () =>
            //    {
            //        i++;
            //        if (i == inputs.Length)
            //        {
            //            return;
            //        }
            //        Run(inputs[i]);
            //    }));
            //}
            //Run(text);
        }
    }

    private IEnumerator Connect(string str, Action onRequestEnd)
    {
        string url = "http://localhost:11434/api/generate";
        UnityWebRequest request = new UnityWebRequest();
        request.url = url;
        request.method = "POST";
        request.SetRequestHeader("Content-Type", "application/json");
        request.downloadHandler = new DownloadHandlerBuffer();
        RequestData data = new RequestData()
        {
            model = "deepseek-r1:1.5b",
            prompt = str,
            context = context,
            stream = false,
            thinking_enabled = false
        };
        string json = JsonUtility.ToJson(data);
        byte[] bts = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bts);
        Debug.Log("发送请求... json==" + json);
        yield return request.SendWebRequest();
        Debug.Log("text==" + request.downloadHandler.text);
        ResponseData res = JsonUtility.FromJson<ResponseData>(request.downloadHandler.text);
        context = res.context;
        onRequestEnd?.Invoke();
    }

    private IEnumerator Connect2()
    {
        string url = "http://localhost:11434/api/chat";
        UnityWebRequest request = new UnityWebRequest();
        request.url = url;
        request.method = "POST";
        request.SetRequestHeader("Content-Type", "application/json");
        request.downloadHandler = new DownloadHandlerBuffer();

        RequestData2 data = new RequestData2()
        {
            model = "deepseek-r1:1.5b",
            messages = new RequestData2.Message[]
            {
                new RequestData2.Message()
                {
                    role = "user",
                    content = "你好！"
                }
            },
            stream = false
        };

        string json = JsonUtility.ToJson(data);
        byte[] bts = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bts);
        Debug.Log("发送请求...");
        yield return request.SendWebRequest();
        Debug.Log("text==" + request.downloadHandler.text);
    }

    public void SendReq(string str, Action onRes)
    {
        string url = "http://localhost:11434/api/generate"; //ollama端口默认11434
        RequestData data = new RequestData()
        {
            model = "deepseek-r1:1.5b",
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
            client.PostAsync(url, content).ContinueWith(t =>
            {
                try
                {
                    //不是200则直接报错
                    HttpResponseMessage msg = t.Result;
                    if (msg.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        Debug.LogError($"错误！statusCode=={msg.StatusCode}, 错误消息=={msg.Content}");
                        return;
                    }
                    msg.Content.ReadAsStreamAsync().ContinueWith(t =>
                    {
                        Task readTask = new Task(() =>
                        {
                            StreamReader reader = new StreamReader(t.Result);
                            string str = "";
                            while (str != null)
                            {
                                str = reader.ReadLine();
                                Debug.Log("str==" + str);
                                System.Threading.Thread.Sleep(100);
                            }
                        });
                        readTask.Start();
                    });
                }
                catch(Exception e)
                {
                    Debug.LogError(e);
                }
            });
        }
        catch(Exception e)
        {
            Debug.LogError(e);
        }
    }
}
