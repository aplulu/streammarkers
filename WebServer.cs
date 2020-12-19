using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using StreamMarkers.Twitch;

namespace StreamMarkers
{
    internal static class WebServer
    {
        delegate void RequestHandler(HttpListenerRequest req, HttpListenerResponse resp);

        public static string ListenAddress { get; private set; } = "localhost:7710";
        private static HttpListener _listener;
        private static Dictionary<string, RequestHandler> _handlers = new Dictionary<string, RequestHandler>();

        public static void Start()
        {
            _handlers.Clear();
            _handlers.Add("/login", OnLogin);
            _handlers.Add("/callback", OnCallback);
            
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://" + ListenAddress + "/");
            _listener.Start();
            _listener.BeginGetContext(OnRequested, null);
        }
        
        public static void Stop()
        {
            if (_listener != null)
            {
                _listener.Stop();
                _listener.Close();
            }
        }

        static void OnRequested(IAsyncResult ar)
        {
            if (!_listener.IsListening)
                return;

            HttpListenerContext ctx = _listener.EndGetContext(ar);
            _listener.BeginGetContext(OnRequested, _listener);
            
            try
            {
                HandleRequest(ctx);
            }
            catch (Exception e)
            {
                ShowMessage(ctx.Response, (int) HttpStatusCode.InternalServerError, "Error", e.ToString());
            }
        }

        private static void HandleRequest(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var resp = ctx.Response;

            Plugin.Logger.Info("HandleRequest Method=" + req.HttpMethod + ", AbsPath=" + req.Url.AbsolutePath + ", Query=" + req.Url.Query);

            if (_handlers.ContainsKey(req.Url.AbsolutePath))
            {
                _handlers[req.Url.AbsolutePath](req, resp);
            }
            else
            {
                ShowMessage(resp, 404, "Error", "Not Found");
            }
        }
        
        /**
         * ログインページへリダイレクト
         */
        private static void OnLogin(HttpListenerRequest req, HttpListenerResponse resp)
        {
            resp.Redirect(TwitchAPI.GetAuthorizeUrl());
            resp.Close();
        }


        /**
         * コールバック処理
         */
        private static void OnCallback(HttpListenerRequest req, HttpListenerResponse resp)
        {
            var error = req.QueryString.Get("error");
            if (error != null)
            {
                ShowMessage(resp, (int) HttpStatusCode.BadRequest, "Error", error);
                return;
            }

            var code = req.QueryString.Get("code");
            if (code == null)
            {
                ShowMessage(resp, (int) HttpStatusCode.BadRequest, "Error", "Not Enough Parameter: code");
                return;
            }

            try
            {
                var token = TwitchAPI.ExchangeToken(code).Result;
                Plugin.Log(token.IDToken);

                PluginConfig.Instance.SetToken(token);
                Plugin.Instance.SettingsViewController.UpdateLoginState();

                ShowMessage(resp, 200, "Login Complete", "You are now logged in, please return to Beat Saber.");
            }
            catch (TwitchAPIException e)
            {
                ShowMessage(resp, 200, "Error", $"Login failed. Please try again: {e.Message}");
            }
        }

        /**
         * メッセージ表示
         */
        private static void ShowMessage(HttpListenerResponse resp, int statusCode, string title, string message)
        {
            resp.StatusCode = statusCode;
            resp.Headers.Add("Content-Type", "text/html; charset=utf-8");
            try
            {
                using (var writer = new StreamWriter(resp.OutputStream, Encoding.UTF8))
                {
                    writer.Write("<html><head><title>" + title+ "</title></head><body><p>");
                    writer.Write(message);
                    writer.Write("</p></body></html>");
                }

                resp.Close();
            }
            catch (Exception e)
            {
                Plugin.Logger.Error(e);
                resp.Abort();
            }
        }
    }
}