using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StreamMarkers.Managers;
using StreamMarkers.Twitch;
using Zenject;

namespace StreamMarkers
{
    public class WebServer: IInitializable, IDisposable
    {
        delegate void RequestHandler(HttpListenerRequest req, HttpListenerResponse resp);

        public static string ListenAddress { get; private set; } = "localhost:7710";
        private HttpListener _listener;
        private Dictionary<string, RequestHandler> _handlers = new Dictionary<string, RequestHandler>();
        private SynchronizationContext _context;
        [Inject] private CredentialProvider _credentialProvider = null;

        public void Initialize()
        {
            _context = SynchronizationContext.Current;
            Start(_context);
        }

        public void Dispose()
        {
            Stop();
        }
        
        public void Start(SynchronizationContext context)
        {
            _context = context;
            _handlers.Clear();
            _handlers.Add("/login", OnLogin);
            _handlers.Add("/callback", OnCallback);
            
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://" + ListenAddress + "/");
            _listener.Start();
            _listener.BeginGetContext(OnRequested, null);
        }
        
        public void Stop()
        {
            if (_listener != null)
            {
                _listener.Stop();
                _listener.Close();
            }
        }

        void OnRequested(IAsyncResult ar)
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

        private void HandleRequest(HttpListenerContext ctx)
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
        private void OnCallback(HttpListenerRequest req, HttpListenerResponse resp)
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

                _context.Post((state) =>
                {
                    _credentialProvider.SetToken(token);
                }, null);

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
                var assembly = Assembly.GetExecutingAssembly();
                var content = "";
                using (var stream = assembly.GetManifestResourceStream("StreamMarkers.Resources.message.html"))
                {
                    if (stream != null)
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            content = sr.ReadToEnd();
                        }
                    }
                }

                using (var writer = new StreamWriter(resp.OutputStream, Encoding.UTF8))
                {
                    content = content.Replace("{title}", title).Replace("{message}", message);
                    writer.Write(content);
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