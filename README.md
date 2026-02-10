# Unity Network Bypass - Turkiye ISP Bloklarini Atlatma

## ne bu ya

Turkiye'deki ISP'ler Discord, OpenAI falan engelliyor ya hani. Iste bu proje o engelleri atlatmak icin. Unity Quest 2 projelerine DoH (DNS-over-HTTPS) ekliyor, basit.

## kurulum falan

### script'leri at iceri

Su dosyalar `Assets/Scripts/` klasorune gitmeli:
- `NetworkBypassManager.cs` - ana sistem
- `NetworkBypassExample.cs` - ornekler var icinde

### scene'e ekle

otomatik olur zaten:
```csharp
// direkt cagir, kendisi halleder
NetworkBypassManager.Instance.ResolveDomain("api.openai.com", (ips) => {
    Debug.Log("Resolved IPs: " + string.Join(", ", ips));
});
```

elle yapmak istersen:
1. bos GameObject olustur
2. isim ver "NetworkBypassManager" diye
3. script'i ekle
4. inspector'dan ayarla

## nasil kullanilir

### domain cozme

```csharp
using NetworkBypass;

// callback ile
NetworkBypassManager.Instance.ResolveDomain("api.litai.com", (ips) =>
{
    if (ips != null && ips.Length > 0)
    {
        Debug.Log($"IP'ler: {string.Join(", ", ips)}");
    }
});

// async tercih edersen
async void ResolveDomain()
{
    string[] ips = await NetworkBypassManager.Instance.ResolveDomainAsync("api.litai.com");
    Debug.Log($"IP'ler: {string.Join(", ", ips)}");
}
```

### bypass'li request

```csharp
using UnityEngine.Networking;
using NetworkBypass;

IEnumerator MakeBypassedRequest()
{
    bool ready = false;
    UnityWebRequest request = null;
    
    // DoH ile IP cozumlu request
    NetworkBypassManager.Instance.CreateBypassedRequest("https://api.litai.com/v1/models", (req) =>
    {
        req.SetRequestHeader("Authorization", "Bearer YOUR_API_KEY");
        request = req;
        ready = true;
    });
    
    yield return new WaitUntil(() => ready);
    yield return request.SendWebRequest();
    
    if (request.result == UnityWebRequest.Result.Success)
    {
        Debug.Log("oldu: " + request.downloadHandler.text);
    }
    else
    {
        Debug.LogError("olmadi: " + request.error);
    }
    
    request.Dispose();
}
```

### Lit AI baglantisi

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using NetworkBypass;

public class LitAIClient : MonoBehaviour
{
    private string litAiApiKey = "YOUR_API_KEY_HERE";
    private string litAiEndpoint = "https://api.litai.com/v1/chat/completions";
    
    public void SendMessageToLitAI(string message)
    {
        StartCoroutine(SendMessageCoroutine(message));
    }
    
    private IEnumerator SendMessageCoroutine(string message)
    {
        string jsonPayload = $@"{{
            ""model"": ""gpt-3.5-turbo"",
            ""messages"": [{{""role"": ""user"", ""content"": ""{message}""}}]
        }}";
        
        bool ready = false;
        UnityWebRequest request = null;
        
        NetworkBypassManager.Instance.CreateBypassedRequest(litAiEndpoint, (req) =>
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.method = "POST";
            
            req.SetRequestHeader("Authorization", $"Bearer {litAiApiKey}");
            req.SetRequestHeader("Content-Type", "application/json");
            
            request = req;
            ready = true;
        });
        
        yield return new WaitUntil(() => ready);
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Lit AI cevap verdi: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError($"Lit AI hata verdi: {request.error}");
        }
        
        request.Dispose();
    }
}
```

## ayarlar

inspector'dan bunlari degistirebilirsin:

### DoH provider'lar
- primary provider: ana DNS (default: Cloudflare)
- fallback provider: yedek DNS (default: Google)

secenekler:
- Cloudflare (1.1.1.1) - bunu kullan
- CloudflareAlt (1.0.0.1)
- Google (8.8.8.8)
- Quad9 (9.9.9.9)
- AdGuard

### cache
- enable cache: DNS cevaplarini sakla (ac bunu)
- cache expiration: ne kadar saklayacak (default: 300 saniye)

### timeout
- query timeout: DNS sorgusu max ne kadar beklesin (default: 5 saniye)

### debug
- enable debug logs: log'lari gormek istersen ac

## Quest 2 notlari

### build ayarlari

1. platform: Android
2. minimum API: Android 7.0 (API 24) veya ust
3. internet permission: otomatik eklenir zaten

### Quest 2'de test

```bash
# APK build et
# Unity: File -> Build Settings -> Android -> Build

# Quest'e yukle
adb install -r YourApp.apk

# log'lara bak
adb logcat -s Unity
```

### performans

DoH biraz gecikme ekler. VR'da sorun olmasin diye:

1. cache'i ac - ayni sorguyu tekrar yapmaz
2. baslangicta domain'leri yukle - onceden coz
3. async kullan - ana thread'i bloklama

```csharp
// baslangicta yukle
async void Start()
{
    await NetworkBypassManager.Instance.ResolveDomainAsync("api.litai.com");
    await NetworkBypassManager.Instance.ResolveDomainAsync("api.openai.com");
    Debug.Log("domain'ler yuklendi");
}
```

## sorun giderme

### "DNS resolution failed" diyor

cozum 1: baska provider dene
```csharp
NetworkBypassManager.Instance.primaryProvider = NetworkBypassManager.DoHProvider.Google;
```

cozum 2: timeout'u artir
```csharp
NetworkBypassManager.Instance.queryTimeoutSeconds = 10;
```

cozum 3: cache'i temizle
```csharp
NetworkBypassManager.Instance.ClearCache();
```

### "Connection failed" diyor

1. Quest'in interneti var mi bak
2. debug log'lari ac
3. firewall/antivirus kapat bi

### editor'de calisiyor ama Quest'te calisimiyor

1. Android build settings'e bak
2. internet permission var mi kontrol et (Player Settings -> Android -> Other Settings)
3. API level'a bak (minimum Android 7.0 olmali)

## test sonuclari

### calisan servisler
- Discord API - calisiyor
- OpenAI API - calisiyor
- Cloudflare - calisiyor
- Google - calisiyor

### test edilen ISP'ler
- Superonline - bypass ediyor
- TTNet - bypass ediyor
- Turk Telekom - bypass ediyor
- Vodafone TR - bypass ediyor

## guvenlik

- DoH DNS sorgularini sifreler (HTTPS)
- ISP hangi domain'lere baktigini goremiyor
- ama IP baglantilarini gorebilir hala
- tam gizlilik istersen VPN kullan

## yardim lazimsa

sorun yasarsan:
1. debug log'lari ac
2. Unity Console'a bak
3. `adb logcat` ile Quest log'larina bak

## yapilacaklar listesi

- SNI Fragmentation (DPI bypass)
- Cloudflare WARP entegrasyonu
- VPN service
- otomatik ISP tespiti
- akilli routing
