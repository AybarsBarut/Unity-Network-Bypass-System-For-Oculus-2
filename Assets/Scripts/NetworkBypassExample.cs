using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using NetworkBypass;

/// <summary>
/// Example usage of NetworkBypassManager for bypassing Turkish ISP restrictions
/// This script demonstrates how to use DoH to connect to blocked services like Lit AI
/// </summary>
public class NetworkBypassExample : MonoBehaviour
{
    [Header("Test Configuration")]
    [Tooltip("Domain to test DNS resolution")]
    public string testDomain = "api.openai.com";
    
    [Tooltip("Full URL to test bypassed request")]
    public string testUrl = "https://api.openai.com/v1/models";

    [Header("Lit AI Configuration")]
    [Tooltip("Lit AI API endpoint")]
    public string litAiEndpoint = "https://api.litai.com";
    
    // TODO: Add your API key here or load from a secure config file
    // DO NOT commit your actual API key to version control
    private string litAiApiKey = ""; // Set this at runtime

    private void Start()
    {
        // Example 1: Simple domain resolution
        TestDomainResolution();
        
        // Example 2: Create bypassed web request
        StartCoroutine(TestBypassedRequest());
        
        // Example 3: Connect to Lit AI (if API key is provided)
        if (!string.IsNullOrEmpty(litAiApiKey))
        {
            StartCoroutine(ConnectToLitAI());
        }
    }

    /// <summary>
    /// Example 1: Resolve a domain using DoH
    /// </summary>
    private void TestDomainResolution()
    {
        Debug.Log($"[Example] Resolving domain: {testDomain}");
        
        NetworkBypassManager.Instance.ResolveDomain(testDomain, (ips) =>
        {
            if (ips != null && ips.Length > 0)
            {
                Debug.Log($"[Example] Successfully resolved {testDomain}:");
                foreach (string ip in ips)
                {
                    Debug.Log($"[Example]   - {ip}");
                }
            }
            else
            {
                Debug.LogError($"[Example] Failed to resolve {testDomain}");
            }
        });
    }

    /// <summary>
    /// Example 2: Create a bypassed web request
    /// </summary>
    private IEnumerator TestBypassedRequest()
    {
        Debug.Log($"[Example] Creating bypassed request to: {testUrl}");
        
        bool requestReady = false;
        UnityWebRequest request = null;
        
        NetworkBypassManager.Instance.CreateBypassedRequest(testUrl, (req) =>
        {
            request = req;
            requestReady = true;
        });
        
        // Wait for request to be created
        yield return new WaitUntil(() => requestReady);
        
        if (request == null)
        {
            Debug.LogError("[Example] Failed to create bypassed request");
            yield break;
        }
        
        // Send the request
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[Example] Request successful! Response length: {request.downloadHandler.text.Length}");
            Debug.Log($"[Example] Response preview: {request.downloadHandler.text.Substring(0, Mathf.Min(200, request.downloadHandler.text.Length))}...");
        }
        else
        {
            Debug.LogError($"[Example] Request failed: {request.error}");
        }
        
        request.Dispose();
    }

    /// <summary>
    /// Example 3: Connect to Lit AI using bypassed connection
    /// </summary>
    private IEnumerator ConnectToLitAI()
    {
        Debug.Log("[Example] Connecting to Lit AI...");
        
        bool requestReady = false;
        UnityWebRequest request = null;
        
        // Create bypassed request to Lit AI
        NetworkBypassManager.Instance.CreateBypassedRequest(litAiEndpoint, (req) =>
        {
            // Add authentication header
            req.SetRequestHeader("Authorization", $"Bearer {litAiApiKey}");
            req.SetRequestHeader("Content-Type", "application/json");
            
            request = req;
            requestReady = true;
        });
        
        yield return new WaitUntil(() => requestReady);
        
        if (request == null)
        {
            Debug.LogError("[Example] Failed to create Lit AI request");
            yield break;
        }
        
        // Send request
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[Example] ✅ Successfully connected to Lit AI!");
            Debug.Log($"[Example] Response: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"[Example] ❌ Failed to connect to Lit AI: {request.error}");
            Debug.LogError($"[Example] Response Code: {request.responseCode}");
        }
        
        request.Dispose();
    }

    /// <summary>
    /// Example 4: Manual async usage (for advanced scenarios)
    /// </summary>
    private async void TestAsyncResolution()
    {
        Debug.Log($"[Example] Async resolving: {testDomain}");
        
        string[] ips = await NetworkBypassManager.Instance.ResolveDomainAsync(testDomain);
        
        if (ips != null && ips.Length > 0)
        {
            Debug.Log($"[Example] Async resolution successful: {string.Join(", ", ips)}");
        }
        else
        {
            Debug.LogError("[Example] Async resolution failed");
        }
    }

    /// <summary>
    /// Example 5: Clear DNS cache (useful for testing or when IPs change)
    /// </summary>
    public void ClearDNSCache()
    {
        NetworkBypassManager.Instance.ClearCache();
        Debug.Log("[Example] DNS cache cleared");
    }
}
