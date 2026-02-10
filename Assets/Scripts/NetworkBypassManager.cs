using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace NetworkBypass
{
    /// <summary>
    /// DNS-over-HTTPS (DoH) resolver for bypassing DNS-based restrictions in Turkey.
    /// Uses Cloudflare and Google DoH endpoints to resolve domain names securely.
    /// </summary>
    public class NetworkBypassManager : MonoBehaviour
    {
        #region Singleton
        private static NetworkBypassManager _instance;
        public static NetworkBypassManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("NetworkBypassManager");
                    _instance = go.AddComponent<NetworkBypassManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        #endregion

        #region Configuration
        [Header("DoH Providers")]
        [Tooltip("Primary DoH provider")]
        public DoHProvider primaryProvider = DoHProvider.Cloudflare;
        
        [Tooltip("Fallback DoH provider")]
        public DoHProvider fallbackProvider = DoHProvider.Google;

        [Header("Cache Settings")]
        [Tooltip("Enable DNS response caching")]
        public bool enableCache = true;
        
        [Tooltip("Cache expiration time in seconds")]
        public int cacheExpirationSeconds = 300;

        [Header("Timeout Settings")]
        [Tooltip("DNS query timeout in seconds")]
        public int queryTimeoutSeconds = 5;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool enableDebugLogs = true;
        #endregion

        #region DoH Endpoints
        private static readonly Dictionary<DoHProvider, string> DoHEndpoints = new Dictionary<DoHProvider, string>
        {
            { DoHProvider.Cloudflare, "https://1.1.1.1/dns-query" },
            { DoHProvider.CloudflareAlt, "https://1.0.0.1/dns-query" },
            { DoHProvider.Google, "https://dns.google/resolve" },
            { DoHProvider.Quad9, "https://dns.quad9.net/dns-query" },
            { DoHProvider.AdGuard, "https://dns.adguard.com/dns-query" }
        };
        #endregion

        #region Cache
        private class DNSCacheEntry
        {
            public string[] IpAddresses;
            public DateTime ExpirationTime;
        }

        private Dictionary<string, DNSCacheEntry> _dnsCache = new Dictionary<string, DNSCacheEntry>();
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LogDebug("NetworkBypassManager initialized");
            LogDebug($"Primary Provider: {primaryProvider}");
            LogDebug($"Fallback Provider: {fallbackProvider}");
        }
        #endregion

        #region Public API
        /// <summary>
        /// Resolves a domain name to IP addresses using DoH
        /// </summary>
        /// <param name="domain">Domain name to resolve</param>
        /// <param name="callback">Callback with resolved IP addresses (null on failure)</param>
        public void ResolveDomain(string domain, Action<string[]> callback)
        {
            StartCoroutine(ResolveDomainCoroutine(domain, callback));
        }

        /// <summary>
        /// Resolves a domain name to IP addresses using DoH (async)
        /// </summary>
        /// <param name="domain">Domain name to resolve</param>
        /// <returns>Array of IP addresses (null on failure)</returns>
        public async Task<string[]> ResolveDomainAsync(string domain)
        {
            // Check cache first
            if (enableCache && _dnsCache.TryGetValue(domain, out DNSCacheEntry cacheEntry))
            {
                if (DateTime.UtcNow < cacheEntry.ExpirationTime)
                {
                    LogDebug($"Cache hit for {domain}: {string.Join(", ", cacheEntry.IpAddresses)}");
                    return cacheEntry.IpAddresses;
                }
                else
                {
                    // Remove expired entry
                    _dnsCache.Remove(domain);
                    LogDebug($"Cache expired for {domain}");
                }
            }

            // Try primary provider
            string[] ips = await QueryDoHProvider(primaryProvider, domain);
            
            // Try fallback if primary failed
            if (ips == null || ips.Length == 0)
            {
                LogDebug($"Primary provider failed, trying fallback: {fallbackProvider}");
                ips = await QueryDoHProvider(fallbackProvider, domain);
            }

            // Cache the result
            if (ips != null && ips.Length > 0 && enableCache)
            {
                _dnsCache[domain] = new DNSCacheEntry
                {
                    IpAddresses = ips,
                    ExpirationTime = DateTime.UtcNow.AddSeconds(cacheExpirationSeconds)
                };
                LogDebug($"Cached {domain}: {string.Join(", ", ips)}");
            }

            return ips;
        }

        /// <summary>
        /// Clears the DNS cache
        /// </summary>
        public void ClearCache()
        {
            _dnsCache.Clear();
            LogDebug("DNS cache cleared");
        }

        /// <summary>
        /// Creates a UnityWebRequest with DoH-resolved IP address
        /// This bypasses DNS resolution and uses direct IP connection
        /// </summary>
        /// <param name="url">Full URL to request</param>
        /// <param name="callback">Callback with configured UnityWebRequest</param>
        public void CreateBypassedRequest(string url, Action<UnityWebRequest> callback)
        {
            StartCoroutine(CreateBypassedRequestCoroutine(url, callback));
        }
        #endregion

        #region Private Methods
        private IEnumerator ResolveDomainCoroutine(string domain, Action<string[]> callback)
        {
            Task<string[]> task = ResolveDomainAsync(domain);
            yield return new WaitUntil(() => task.IsCompleted);
            
            if (task.Exception != null)
            {
                LogError($"Error resolving {domain}: {task.Exception.Message}");
                callback?.Invoke(null);
            }
            else
            {
                callback?.Invoke(task.Result);
            }
        }

        private async Task<string[]> QueryDoHProvider(DoHProvider provider, string domain)
        {
            string endpoint = DoHEndpoints[provider];
            
            try
            {
                LogDebug($"Querying {provider} for {domain}");

                // Build DoH query URL (using Google's JSON API format for simplicity)
                string queryUrl = $"{endpoint}?name={domain}&type=A";

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(queryTimeoutSeconds);
                    client.DefaultRequestHeaders.Add("Accept", "application/dns-json");

                    HttpResponseMessage response = await client.GetAsync(queryUrl);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        LogError($"DoH query failed: {response.StatusCode}");
                        return null;
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    LogDebug($"DoH response: {jsonResponse}");

                    // Parse JSON response
                    DNSResponse dnsResponse = JsonUtility.FromJson<DNSResponse>(jsonResponse);
                    
                    if (dnsResponse == null || dnsResponse.Answer == null || dnsResponse.Answer.Length == 0)
                    {
                        LogError($"No DNS answers for {domain}");
                        return null;
                    }

                    // Extract IP addresses
                    List<string> ips = new List<string>();
                    foreach (var answer in dnsResponse.Answer)
                    {
                        if (answer.type == 1) // A record
                        {
                            ips.Add(answer.data);
                        }
                    }

                    if (ips.Count > 0)
                    {
                        LogDebug($"Resolved {domain} to: {string.Join(", ", ips)}");
                        return ips.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Exception querying {provider}: {ex.Message}");
            }

            return null;
        }

        private IEnumerator CreateBypassedRequestCoroutine(string url, Action<UnityWebRequest> callback)
        {
            // Parse URL to extract domain
            Uri uri = new Uri(url);
            string domain = uri.Host;

            // Resolve domain using DoH
            Task<string[]> resolveTask = ResolveDomainAsync(domain);
            yield return new WaitUntil(() => resolveTask.IsCompleted);

            if (resolveTask.Exception != null || resolveTask.Result == null || resolveTask.Result.Length == 0)
            {
                LogError($"Failed to resolve {domain}, falling back to normal request");
                callback?.Invoke(UnityWebRequest.Get(url));
                yield break;
            }

            // Use first resolved IP
            string ip = resolveTask.Result[0];
            
            // Replace domain with IP in URL
            string bypassedUrl = url.Replace(domain, ip);
            
            // Create request with IP
            UnityWebRequest request = UnityWebRequest.Get(bypassedUrl);
            
            // Add Host header to maintain SNI
            request.SetRequestHeader("Host", domain);
            
            LogDebug($"Created bypassed request: {bypassedUrl} (Host: {domain})");
            
            callback?.Invoke(request);
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[NetworkBypass] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[NetworkBypass] {message}");
        }
        #endregion

        #region Data Structures
        public enum DoHProvider
        {
            Cloudflare,
            CloudflareAlt,
            Google,
            Quad9,
            AdGuard
        }

        [Serializable]
        private class DNSResponse
        {
            public int Status;
            public DNSAnswer[] Answer;
        }

        [Serializable]
        private class DNSAnswer
        {
            public string name;
            public int type;
            public int TTL;
            public string data;
        }
        #endregion
    }
}
