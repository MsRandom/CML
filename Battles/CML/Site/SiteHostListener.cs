using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CML.Site
{
    public sealed class SiteHostListener : CmlListener
    {
        private HttpListener _listener;
        private readonly HttpRequestHandlers _handler;
        private readonly string _url;
        private bool _listening;

        public SiteHostListener(string url)
        {
            _handler = new HttpRequestHandlers(new FileInfo(Program.ParsedArgs["wwwroot"] + "/endpoints.json"));
            _url = url;
        }
        
        private void HandleConnections(IAsyncResult result)
        {
            Task.Run(async () =>
            {
                while (_listening)
                {
                    var ctx = _listener.EndGetContext(result);
                    _listener.BeginGetContext(HandleConnections, null);
                    var req = ctx.Request;
                    var resp = ctx.Response;
                    await _handler.HandleRequest(req, resp);
                }
            });
        }

        public override void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _listener.Start();
            _listener.BeginGetContext(HandleConnections, null);
            _listening = true;
            Console.WriteLine("Listening to connections on " + _url);
        }

        public override void Close()
        {
            _listener.Close();
            _listener.Prefixes.Clear();
            _listener = null;
            _listening = false;
            Console.WriteLine("Site listener has been stopped.");
        }
    }
}
