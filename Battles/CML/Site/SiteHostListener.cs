using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CML.Site
{
    public sealed class SiteHostListener : CmlListener
    {
        private HttpListener _listener;
        private readonly HttpRequestHandlers _handler;
        private readonly int _port;
        private bool _listening;

        public SiteHostListener(int port)
        {
            _handler = new HttpRequestHandlers();
            _port = port;
        }
        
        private async void HandleConnections()
        {
            await Task.Run(async () =>
            {
                while (_listening)
                {
                    if (_listener == null) continue;
                    var ctx = await _listener.GetContextAsync();
                    var req = ctx.Request;
                    var resp = ctx.Response;
                    await _handler.HandleRequest(req, resp);
                }
            });
        }

        public void Refresh()
        {
            _handler.SetupLocations();
        }

        public override void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://*:{_port}/");
            _listener.Start();
            _listening = true;
            HandleConnections();
            Console.WriteLine("Listening to connections");
        }

        public override void Close()
        {
            if (_listener != null)
            {
                _listener.Close();
                _listener.Prefixes.Clear();
            }

            _listener = null;
            _listening = false;
            Console.WriteLine("Site listener has been stopped.");
        }
    }
}
