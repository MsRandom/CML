using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CML.Site
{
    public sealed class HttpRequestHandlers
    {
        private readonly HttpPath _index = new HttpPath();

        public HttpRequestHandlers(FileInfo endpoints)
        {
            var obj = JsonDocument.Parse(endpoints.OpenText().ReadToEnd());
            var dir = new DirectoryInfo(Program.ParsedArgs["wwwroot"] + "/");
            var types = new[] {"endpoint", "auth"};
            Fill(_index, obj.RootElement);
            Link(_index, dir);

            void Fill(HttpPath parent, JsonElement container)
            {
                var http = new HttpPath();
                foreach (var prop in container.EnumerateObject())
                {
                    if (types.Contains(prop.Name))
                    {
                        parent.Flags.Add(prop.Name);
                        continue;
                    }

                    parent.Children[prop.Name] = http;
                    Fill(http, prop.Value);
                }
            }

            void Link(HttpPath path, DirectoryInfo baseDir)
            {
                foreach (var file in baseDir.EnumerateFiles())
                {
                    var lower = file.Name.ToLower();
                    var end = ".html";
                    if (!lower.EndsWith(end)) continue;
                    var http = new HttpPath();
                    var authEnd = ".oa" + end; 
                    http.Flags.Add("path");
                    if (lower.EndsWith(authEnd))
                    {
                        end = authEnd;
                        http.Flags.Add("auth");
                    }

                    http.Data = file.OpenText().ReadToEnd();
                    path.Children[file.Name.Replace(end, "")] = http;
                }

                foreach (var inner in baseDir.EnumerateDirectories()) Link(path.Children.ContainsKey(inner.Name) ? path.Children[inner.Name] : path.Children[inner.Name] = new HttpPath(), inner);
            }

            var favicon = new HttpPath();
            const string name = nameof(favicon) + ".ico";
            favicon.Flags.Add(name);
            favicon.Data = File.ReadAllBytes(Program.ParsedArgs["wwwroot"] + "/" + name);
            _index.Children[name] = favicon;
            AssignEndpoints();
        }

        private void AssignEndpoints()
        {
            SetEndpoint("validateToken", (s, o) =>
            {
                dynamic res = new JObject();
                res.valid = o.token == Program.ParsedArgs["token"];
                return res.ToString();
            });
            SetEndpoint("getBattles", (s, o) =>
            {
                dynamic res = new JObject();
                res.contestants = new JArray();
                res.battles = new JArray();
                foreach (var submission in Program.Matches.Submissions) res.contestants.Add(submission);
                foreach (var (item1, item2) in Program.Matches.Battles)
                {
                    dynamic obj = new JObject();
                    obj.item1 = item1;
                    obj.item2 = item2;
                    res.battles.Add(obj);
                }
                return res.ToString();
            });
            SetEndpoint("submit", (s, o) =>
            {
                dynamic res = new JObject();
                if (o.speed + o.attack + o.defence <= 100)
                {
                    Program.Matches.Submissions.Add(o);
                    res.success = true;
                }
                else res.success = false;
                return res.ToString();
            });
        }
        
        private void SetEndpoint(string name, Func<string, dynamic, string> action)
        {
            var path = _index;
            foreach (var s in name.Split('/')) if (path.Children.ContainsKey(s)) path = path.Children[s];
            path.Data = action;
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

            if (path.Flags.Contains("auth"))
            {
                if (req.Headers["Authorization"] == Program.ParsedArgs["token"]) await Handle();
                else await ReturnError(resp, "Unauthorized", 401, "Must provide valid token");
            }
            else await Handle();

            async Task Handle()
            {
                if (path.Flags.Contains("path")) WriteString(resp, path.Data, "html");
                else if (path.Flags.Contains("endpoint"))
                {
                    dynamic body = null;
                    if(req.HttpMethod == "POST") body = JsonConvert.DeserializeObject<dynamic>(new StreamReader(req.InputStream).ReadToEnd());
                    await WriteString(resp, path.Data(url.Query, body));
                }
                else if (path.Flags.Contains("favicon.ico")) WriteBytes(resp, path.Data, "image/ico");
                else await NotFound(resp);
            }

            async Task NotFound(HttpListenerResponse r)
            {
                await ReturnError(r, "Not found", 404, "The page you were looking for does not exist");
            }
        }

        private static async Task ReturnError(HttpListenerResponse resp, string error, int status, string message)
        {
            await WriteString(resp, GenerateError(error, status, message));
        }

        private static string GenerateError(string error, int status, string message)
        {
            dynamic body = new JObject();
            body.error = error;
            body.status = status;
            body.message = message;
            return body.ToString();
        }

        private static async Task WriteString(HttpListenerResponse resp, string response, string type = "json")
        {
            var data = Encoding.UTF8.GetBytes(response);
            await WriteBytes(resp, data, "text/" + type);
        }
        
        private static async Task WriteBytes(HttpListenerResponse resp, byte[] data, string type)
        {
            resp.ContentType = type;
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = data.LongLength;
            await resp.OutputStream.WriteAsync(data, 0, data.Length);
            resp.Close();
        }

        private HttpPath Parse(params string[] segments)
        {
            HttpPath current = null;
            foreach (var dir in segments)
            {
                if (dir == "/") current = _index;
                else if (current != null)
                {
                    var child = dir.Replace("/", "");
                    if(current.Children.ContainsKey(child)) current = current.Children[child];
                    else return null;
                }
                else return null;
            }
            
            return current == _index ? current?.Children["index"] : current;
        }
        
        private sealed class HttpPath
        {
            public readonly Dictionary<string, HttpPath> Children = new Dictionary<string, HttpPath>();
            public readonly List<string> Flags = new List<string>();
            public dynamic Data;
        }
    }
}
