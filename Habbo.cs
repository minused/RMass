﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog.Core;

namespace RMass
{
    internal class Habbo : IDisposable
    {
        private readonly string _captchaToken;

        private readonly Logger _logger;

        public readonly short Id;

        private Avatar[] _avatars = null!;

        private HttpClient _client = null!;

        private IEnumerable<string> _cookies = null!;

        private Avatar _currentAvatar = null!;

        public string CurrentSso = null!;

        public Habbo([NotNull] string captchaToken, short id)
        {
            _captchaToken = captchaToken;
            Id            = id;

            _logger = LogCreator.Create("Habbo");
        }

        public int Count => _avatars?.Length ?? 0;

        public Avatar this[short index] => _avatars[index];

        public void Dispose()
        {
            _client?.Dispose();
        }

        public async Task<bool> TryLoginAsync([NotNull] string email, [NotNull] string pass)
        {
            try
            {
                _client = new HttpClient();

                using var request = new HttpRequestMessage(new HttpMethod("POST"),
                                                           "https://www.habbo.fr/api/public/authentication/login");

                request.Headers.TryAddWithoutValidation("Host", "www.habbo.fr");

                request.Headers.TryAddWithoutValidation("Accept",
                                                        "text/xml, application/xml, application/xhtml+xml, text/html;q=0.9, text/plain;q=0.8, text/css, image/png, image/jpeg, image/gif;q=0.8, application/x-shockwave-flash, video/mp4;q=0.9, flv-application/octet-stream;q=0.8, video/x-flv;q=0.7, audio/mp4, application/futuresplash, */*;q=0.5");

                request.Headers.TryAddWithoutValidation("User-Agent",
                                                        "Mozilla/5.0 (Android; U; pt-BR) AppleWebKit/533.19.4 (KHTML, like Gecko) AdobeAIR/33.1");

                request.Headers.TryAddWithoutValidation("x-flash-version", "33,1,1,98");
                request.Headers.TryAddWithoutValidation("Referer", "app:/HabboTablet.swf");
                request.Headers.TryAddWithoutValidation("X-Habbo-Device-Type", "android");

                request.Headers.TryAddWithoutValidation("x-habbo-api-deviceid",
                                                        "534c27bcebfc41d3a2b6d11aae9a10ff00013390ef0df38ed56f161a5a4f95845bdd3cd1a988:79d112b14b886083a0027ec98642c01f3aa06926");

                request.Headers.TryAddWithoutValidation("X-Habbo-Device-ID",
                                                        "534c27bcebfc41d3a2b6d11aae9a10ff00013390ef0df38ed56f161a5a4f95845bdd3cd1a988:79d112b14b886083a0027ec98642c01f3aa06926");

                request.Headers.TryAddWithoutValidation("Cookie",
                                                        "browser_token=s%3Aes6e3pTWEmIF8GU1m6mB-IxjXH_EIQymM5QIP5EVMqg.zZTMagmddSa%2F4fSv1OZs83gcx5JIXU2Vnx5qlP3zBhY");

                request.Content =
                    new
                        StringContent($"{{\"captchaToken\":\"{_captchaToken}\",\"email\":\"{email}\",\"password\":\"{pass}\"}}");

                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var response = await _client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _cookies = response.Headers.GetValues("set-cookie")
                                       .Append("browser_token=s%3Aes6e3pTWEmIF8GU1m6mB-IxjXH_EIQymM5QIP5EVMqg.zZTMagmddSa%2F4fSv1OZs83gcx5JIXU2Vnx5qlP3zBhY");

                    _logger.Information("[Acc #{Id}] Login successful.", Id);

                    return true;
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    if (content.Contains("invalid_password"))
                        _logger.Error("[Acc #{Id}] Failed to login, wrong password.", Id);
                    else if (content.Contains("user_banned"))
                        _logger.Error("[Acc #{Id}] Failed to login, banned account.", Id);
                    else if (content.Contains("invalid-captcha"))
                        _logger.Error("[Acc #{Id}] Failed to login, invalid captcha.", Id);
                    else
                        _logger.Error("[Acc #{Id}] Failed to login, wrong error.", Id);
                }
                else
                    _logger.Error("[Acc #{Id}] Failed to login, wrong error.", Id);

                return false;
            }
            catch (Exception e)
            {
                _logger.Error(e, "[Acc #{Id}] Could not log in.", Id);

                return false;
            }
        }

        public async Task<bool> TryLoadAvatarsAsync()
        {
            try
            {
                _client = new HttpClient();

                using var request =
                    new HttpRequestMessage(new HttpMethod("GET"), "https://www.habbo.fr/api/user/avatars");

                request.Headers.TryAddWithoutValidation("Host", "www.habbo.fr");

                request.Headers.TryAddWithoutValidation("Accept",
                                                        "text/xml, application/xml, application/xhtml+xml, text/html;q=0.9, text/plain;q=0.8, text/css, image/png, image/jpeg, image/gif;q=0.8, application/x-shockwave-flash, video/mp4;q=0.9, flv-application/octet-stream;q=0.8, video/x-flv;q=0.7, audio/mp4, application/futuresplash, */*;q=0.5");

                request.Headers.TryAddWithoutValidation("User-Agent",
                                                        "Mozilla/5.0 (Android; U; pt-BR) AppleWebKit/533.19.4 (KHTML, like Gecko) AdobeAIR/33.1");

                request.Headers.TryAddWithoutValidation("x-flash-version", "33,1,1,98");
                request.Headers.TryAddWithoutValidation("Referer", "app:/HabboTablet.swf");
                request.Headers.TryAddWithoutValidation("X-Habbo-Device-Type", "android");

                request.Headers.TryAddWithoutValidation("x-habbo-api-deviceid",
                                                        "534c27bcebfc41d3a2b6d11aae9a10ff00013390ef0df38ed56f161a5a4f95845bdd3cd1a988:79d112b14b886083a0027ec98642c01f3aa06926");

                request.Headers.TryAddWithoutValidation("X-Habbo-Device-ID",
                                                        "534c27bcebfc41d3a2b6d11aae9a10ff00013390ef0df38ed56f161a5a4f95845bdd3cd1a988:79d112b14b886083a0027ec98642c01f3aa06926");

                request.Headers.TryAddWithoutValidation("Cookie", _cookies);

                var response = await _client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _avatars = JsonConvert.DeserializeObject<IEnumerable<Avatar>>(content).ToArray();


                    _logger.Information("[Acc #{Id}] Loaded {AvatarCount} avatar(s).", Id, Count);

                    return true;
                }

                _logger.Error("[Acc #{Id}] Error fetching avatars ", Id);

                return false;
            }
            catch (Exception e)
            {
                _logger.Error(e, "[Acc #{Id}] Error fetching avatars.", Id);

                return false;
            }
        }

        public async Task<bool> TrySelectAvatarAsync([NotNull] Avatar avatar)
        {
            if (!_avatars.Contains(avatar)) throw new Exception("Avatar does not exist in the current account.");

            _currentAvatar = avatar;

            try
            {
                _client = new HttpClient();

                using var request =
                    new HttpRequestMessage(new HttpMethod("POST"), "https://www.habbo.fr/api/user/avatars/select");

                request.Headers.TryAddWithoutValidation("Host", "www.habbo.fr");

                request.Headers.TryAddWithoutValidation("Accept",
                                                        "text/xml, application/xml, application/xhtml+xml, text/html;q=0.9, text/plain;q=0.8, text/css, image/png, image/jpeg, image/gif;q=0.8, application/x-shockwave-flash, video/mp4;q=0.9, flv-application/octet-stream;q=0.8, video/x-flv;q=0.7, audio/mp4, application/futuresplash, */*;q=0.5");

                request.Headers.TryAddWithoutValidation("User-Agent",
                                                        "Mozilla/5.0 (Android; U; pt-BR) AppleWebKit/533.19.4 (KHTML, like Gecko) AdobeAIR/33.1");

                request.Headers.TryAddWithoutValidation("x-flash-version", "33,1,1,98");
                request.Headers.TryAddWithoutValidation("Referer", "app:/HabboTablet.swf");
                request.Headers.TryAddWithoutValidation("X-Habbo-Device-Type", "android");

                request.Headers.TryAddWithoutValidation("x-habbo-api-deviceid",
                                                        "534c27bcebfc41d3a2b6d11aae9a10ff00013390ef0df38ed56f161a5a4f95845bdd3cd1a988:79d112b14b886083a0027ec98642c01f3aa06926");

                request.Headers.TryAddWithoutValidation("X-Habbo-Device-ID",
                                                        "534c27bcebfc41d3a2b6d11aae9a10ff00013390ef0df38ed56f161a5a4f95845bdd3cd1a988:79d112b14b886083a0027ec98642c01f3aa06926");

                request.Headers.TryAddWithoutValidation("Cookie", _cookies);

                request.Content                     = new StringContent(avatar.ToString());
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var response = await _client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _logger.Information("[Acc #{Id}] {AvatarName} selected.", Id, avatar.Name);

                    return true;
                }

                _logger.Error("[Acc #{Id}] Error selecting avatar ({AvatarName}).", Id, avatar.Name);

                return false;
            }
            catch (Exception e)
            {
                _logger.Error(e, "[Acc #{Id}] Error selecting avatar ({AvatarName}).", Id, avatar.Name);

                return false;
            }
        }

        public async Task<bool> TryGetSsoTokenAsync()
        {
            try
            {
                _client = new HttpClient();

                using var request = new HttpRequestMessage(new HttpMethod("GET"), "https://www.habbo.fr/api/ssotoken");

                request.Headers.TryAddWithoutValidation("Host", "www.habbo.fr");

                request.Headers.TryAddWithoutValidation("Accept",
                                                        "text/xml, application/xml, application/xhtml+xml, text/html;q=0.9, text/plain;q=0.8, text/css, image/png, image/jpeg, image/gif;q=0.8, application/x-shockwave-flash, video/mp4;q=0.9, flv-application/octet-stream;q=0.8, video/x-flv;q=0.7, audio/mp4, application/futuresplash, */*;q=0.5");

                request.Headers.TryAddWithoutValidation("User-Agent",
                                                        "Mozilla/5.0 (Android; U; pt-BR) AppleWebKit/533.19.4 (KHTML, like Gecko) AdobeAIR/33.1");

                request.Headers.TryAddWithoutValidation("x-flash-version", "33,1,1,98");
                request.Headers.TryAddWithoutValidation("Referer", "app:/HabboTablet.swf");
                request.Headers.TryAddWithoutValidation("X-Habbo-Device-Type", "android");

                request.Headers.TryAddWithoutValidation("x-habbo-api-deviceid",
                                                        "534c27bcebfc41d3a2b6d11aae9a10ff00013390ef0df38ed56f161a5a4f95845bdd3cd1a988:79d112b14b886083a0027ec98642c01f3aa06926");

                request.Headers.TryAddWithoutValidation("X-Habbo-Device-ID",
                                                        "534c27bcebfc41d3a2b6d11aae9a10ff00013390ef0df38ed56f161a5a4f95845bdd3cd1a988:79d112b14b886083a0027ec98642c01f3aa06926");

                request.Headers.TryAddWithoutValidation("Cookie", _cookies);

                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    var sso = JsonConvert.DeserializeObject<dynamic>(content)
                                         .ssoToken; //{"ssoToken":"fcef80f9-51cb-414a-ba13-88d9235a2ae4-88314676"}

                    _logger.Information("[Acc #{Id} - {AvatarName}] SSO received.", Id, _currentAvatar.Name);
                    //_Logger.Debug("[Conta #{Id} - {AvatarName}] SSO: {Sso}", Id, _currentAvatar.Name, sso.ToString());
                    // _Logger.Debug("Para produção somente: {SSO}", content);

                    CurrentSso = sso;

                    return true;
                }

                _logger.Error("[Acc #{Id} - {AvatarName}] Error getting SSO.", Id, _currentAvatar.Name);

                return false;
            }
            catch (Exception e)
            {
                _logger.Error(e, "[Acc #{Id} - {AvatarName}] Error getting SSO.", Id, _currentAvatar.Name);

                return false;
            }
        }
    }

    internal class Avatar
    {
        [JsonProperty("uniqueId")] public string UniqueId { get; set; }

        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("figureString")] public string FigureString { get; set; }

        [JsonProperty("motto")] public string Motto { get; set; }

        [JsonProperty("buildersClubMember")] public bool BuildersClubMember { get; set; }

        [JsonProperty("habboClubMember")] public bool HabboClubMember { get; set; }

        [JsonProperty("lastWebAccess")]
        public DateTime LastWebAccess { get; set; }

        [JsonProperty("creationTime")]
        public DateTime CreationTime { get; set; }

        [JsonProperty("banned")] public bool Banned { get; set; }

        public override string ToString()
        {
            return $"{{\"uniqueId\":\"{UniqueId}\"}}";
        }
    }
}