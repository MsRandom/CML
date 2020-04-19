using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HttpMultipartParser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CML.Site
{
    public sealed class HttpRequestHandlers
    {
        private readonly Dictionary<string, HttpPath> _paths = new Dictionary<string, HttpPath>();
        private readonly HttpPath _index = new HttpPath();
        
        public HttpRequestHandlers()
        {
            SetupLocations();
            AssignEndpoints();
        }

        public void SetupLocations()
        {
            _paths.Clear();
            Link(_index, new DirectoryInfo(Program.Config.SiteRoot));
            
            static void Link(HttpPath path, DirectoryInfo baseDir)
            {
                foreach (var file in baseDir.EnumerateFiles())
                {
                    const string end = ".html";
                    const string authEnd = ".oa" + end;
                    var http = new HttpPath
                    {
                        Auth = file.Name.ToLower().EndsWith(authEnd),
                        ContentType = MimeTypes.GetMimeType(file.Name),
                        Data = File.ReadAllBytes(file.FullName)
                    };
                    path.Children[file.Name.Replace(authEnd, "").Replace(end, "")] = http;
                }

                foreach (var inner in baseDir.EnumerateDirectories()) Link(path.Children.ContainsKey(inner.Name) ? path.Children[inner.Name] : path.Children[inner.Name] = new HttpPath(), inner);
            }
        }

        private void AssignEndpoints()
        {
            //Uses reflection to get the "public static JContainer x(HttpListenerRequest req, string s, dynamic o, bool auth = true)" methods of EndpointHandlers and assigns them to endpoints using their names, the auth parameter can be omitted to make authentication not required   
            var methods = typeof(EndpointHandlers).GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (var method in methods)
            {
                JContainer Action(HttpListenerRequest r, string s, dynamic o) => (method.GetParameters().Length == 4 ? method.Invoke(null, new []{ r, s, o, true }) : method.Invoke(null, new []{ r, s, o })) as JContainer;
                SetEndpoint(method.Name, Action, method.GetParameters().Length == 4);
            }
        }
        
        private void SetEndpoint(string name, Func<HttpListenerRequest, string, dynamic, JContainer> action, bool auth = false)
        {
            var path = _index;
            foreach (var s in name.Split("0"))
            {
                var n = s.Substring(0, 1).ToLower() + s.Substring(1);
                if (path.Children.ContainsKey(n)) path = path.Children[n];
                else
                    path = path.Children[n] = new HttpPath
                    {
                        Auth = auth,
                    };
            }
            path.Handler = action;
        }

        public async Task HandleRequest(HttpListenerRequest req, HttpListenerResponse resp)
        {
            var url = req.Url;
            var segments = url.Segments;
            var path = Parse(segments);
            if (path == null)
            {
                //might want to redirect to another page here instead of just returning an html error
                await NotFound(resp);
                return;
            }

            if (path.Data != null)
            {
                var idCookie = req.Cookies["Client-Token"];
                var index = idCookie?.Value.IndexOf("/", StringComparison.Ordinal);
                var id = idCookie == null || index <= 0 ? Guid.Empty : Guid.Parse(idCookie.Value.Substring(0, index.Value));
                var auth = path.Auth && id != Guid.Empty && EndpointHandlers.Authorized.Contains(id);
                if (!path.Auth || auth)
                {
                    if (auth) EndpointHandlers.Authorized.Remove(id);
                    await WriteBytes(resp, path.Data, path.ContentType);
                }
                else await ReturnError(resp, "Unauthorized", 401, "Must have valid auth from the validateToken POST endpoint");
            }
            else if (path.Handler != null)
            {
                try
                {
                    var a = req.Headers["Authorization"];
                    if (!path.Auth || req.Headers["Authorization"] == Program.Config.DiscordToken)
                    {
                        dynamic body = null;
                        if (req.HttpMethod != "GET")
                        {
                            if (req.ContentType == "application/json") body = JsonConvert.DeserializeObject<dynamic>(new StreamReader(req.InputStream).ReadToEnd());
                            else
                            {
                                var dict = new Dictionary<string, object>();
                                var parser = new StreamingMultipartFormDataParser(req.InputStream)
                                {
                                    ParameterHandler = part =>
                                    {
                                        if (!dict.ContainsKey(part.Name)) dict[part.Name] = part.Data;
                                    },
                                    FileHandler = (name, fileName, type, disposition, buffer, bytes, number) =>
                                    {
                                        if (!dict.ContainsKey(name)) dict[name] = (fileName, bytes);
                                    }
                                };
                                await parser.RunAsync();
                                body = dict;
                            }
                        }

                        await WriteJson(resp, path.Handler(req, url.Query, body));
                    }
                    else await ReturnError(resp, "Unauthorized", 401, "Must provide valid token");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            }
            else await NotFound(resp);


            static async Task NotFound(HttpListenerResponse r)
            {
                await ReturnError(r, "Not found", 404, "The page you were looking for does not exist");
            }
        }

        private static async Task ReturnError(HttpListenerResponse resp, string error, int status, string message)
        {
            await WriteJson(resp, GenerateError(error, status, message));
        }

        private static JContainer GenerateError(string error, int status, string message)
        {
            dynamic body = new JObject();
            body.error = error;
            body.status = status;
            body.message = message;
            return body;
        }

        private static async Task WriteJson(HttpListenerResponse resp, JContainer response)
        {
            await WriteBytes(resp, Encoding.UTF8.GetBytes(response == null ? "{}" : response.ToString()), "application/json");
        }
        
        private static async Task WriteBytes(HttpListenerResponse resp, byte[] data, string type)
        {
            resp.ContentType = type;
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = data.LongLength;
            await resp.OutputStream.WriteAsync(data, 0, data.Length);
            resp.Close();
        }

        private HttpPath Parse(string[] segments, bool noRetry = false)
        {
            var path = string.Join("", segments);
            if (_paths.ContainsKey(path)) return _paths[path];
            HttpPath current = null;
            foreach (var dir in segments)
            {
                if (dir == "/") current = _index;
                else if (current != null)
                {
                    var child = dir.Replace("/", "").Replace(".html", "").Replace(".oa", "");
                    if(current.Children.ContainsKey(child)) current = current.Children[child];
                    else return null;
                }
                else return null;
            }
            
            var parsed = current == _index ? current?.Children["index"] : current;
            if (!noRetry && parsed == null)
            {
                SetupLocations();
                var newEndpoint = Parse(segments, true);
                if (newEndpoint != null)
                {
                    _paths[path] = newEndpoint;
                    return newEndpoint;
                } 
            }

            _paths[path] = parsed;
            return parsed;
        }
        
        private sealed class HttpPath
        {
            public readonly Dictionary<string, HttpPath> Children = new Dictionary<string, HttpPath>();
            public bool Auth;
            public string ContentType;
            public byte[] Data;
            public Func<HttpListenerRequest, string, dynamic, JContainer> Handler;
        }
    }
}
