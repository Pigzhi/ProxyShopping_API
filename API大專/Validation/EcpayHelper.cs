using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;

public static class EcpayHelper
{
    public static string Generate(
     Dictionary<string, string> data,
     string hashKey,
     string hashIV)
    {
        // 1. 排序
        var sorted = data
            .Where(x => x.Key != "CheckMacValue")
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}");

        // 2. 組合原始字串 (此時不對 Value 做任何 Encode)
        string raw = $"HashKey={hashKey}&{string.Join("&", sorted)}&HashIV={hashIV}";

        // 3. 全體 URL Encode 並轉小寫 (這步會把 %E5 變成 %e5，也會把 HashKey 轉小寫)
        string rawEncoded = HttpUtility.UrlEncode(raw).ToLower();
    
        //logger.LogInformation("Final Raw for SHA256: " + rawEncoded); 在DEBUG看的 log出來看SHA256

        // 5. SHA256
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawEncoded));
        string hashResult = BitConverter.ToString(bytes).Replace("-", "").ToUpperInvariant().Trim();
        //logger.LogInformation("Final Raw for hashResult: " + hashResult);在DEBUG看的 log出來看 加密後的檢核瑪
        return hashResult;
    }

   
    /// 驗證 callback 傳回的 CheckMacValue
    public static bool Verify(
        Dictionary<string, string> data,
        string hashKey,
        string hashIV)
    {
        if (!data.ContainsKey("CheckMacValue"))
            return false;

        var received = data["CheckMacValue"];
        var generated = Generate(data, hashKey, hashIV);


        return received.Equals(generated, StringComparison.OrdinalIgnoreCase);
    }

    /// 產生自動送出的 HTML 表單
    public static string BuildAutoSubmitForm(string actionUrl, Dictionary<string, string> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html>");
        sb.AppendLine("<head><meta charset='utf-8'><title>Redirecting...</title></head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"<form method='post' action='{actionUrl}'>");

        foreach (var item in data)
        {
            // 使用 WebUtility.HtmlEncode 確保 Value 安全
            sb.AppendLine($"<input type='hidden' name='{item.Key}' value='{System.Net.WebUtility.HtmlEncode(item.Value)}' />");
        }

        sb.AppendLine("<noscript><p>您的瀏覽器不支援 JavaScript，請點擊按鈕繼續。</p><button type='submit'>繼續付款</button></noscript>");
        sb.AppendLine("</form>");

        // 解決 CSP 問題：不使用標籤內的 onload，改用獨立 Script
        sb.AppendLine("<script>");
        sb.AppendLine("  (function() {");
        sb.AppendLine("    document.forms[0].submit();");
        sb.AppendLine("  })();");
        sb.AppendLine("</script>");

        sb.AppendLine("</body></html>");

        return sb.ToString();
    }
}
