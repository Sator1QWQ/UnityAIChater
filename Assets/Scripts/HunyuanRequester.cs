using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;

/// <summary>
/// 腾讯混元大模型
/// </summary>
public class HunyuanRequester : DeepseekRequester
{
    public override string Url => "https://api.hunyuan.cloud.tencent.com/v1/chat/completions";

    protected override string APIKey => "sk-U5KBLgzn8lilOTozu11zX5aiyYNnrNtcOiMhoes4eI2Lk3aF";

    protected override string Model => "hunyuan-turbo";

    protected override string GetRequestJson(string systemStr, List<string> strList)
    {
        messages.Add(new Message()
        {
            role = "user",
            content = systemStr
        });
        for (int i = 0; i < strList.Count; i++)
        { 
            messages.Add(new Message()
            {
                role = "user",
                content = strList[i]
            });
        }
        RequestData data = new RequestData()
        {
            model = Model,
            messages = messages.ToArray(),
            stream = true
        };
        string json = JsonUtility.ToJson(data);
        return json;
    }
}
