using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CML.Site
{
    public sealed class SiteHostListener : CmlListener
    {
        private HttpListener _listener;
        private readonly string _url;
        private readonly string _data;

        public SiteHostListener(FileInfo html, string url)
        {
            _data = html.OpenText().ReadToEnd();
            _url = url;
        }
        
        private void HandleConnections()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var ctx = await _listener.GetContextAsync();
                    //var req = ctx.Request;
                    var resp = ctx.Response;
                    //TODO handle http requests
                    var data = Encoding.UTF8.GetBytes(_data);
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    //for future error handling
                    const bool error = false;
                    if (error)
                    {
                        break;
                    }

                    resp.Close();
                }
            });
        }

        public override void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _listener.Start();
            HandleConnections();
        }
    }
}
