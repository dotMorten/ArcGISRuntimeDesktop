using Esri.ArcGISRuntime.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ArcGISRuntimeDesktop.Services.ArcGISServer
{
    public class Client
    {   
        private readonly HttpClient _client = new HttpClient(new ArcGISHttpClientHandler());
        private string _url;
        public Client(string url) : this(url, new HttpClient(new ArcGISHttpClientHandler()))
        {
        }
        private Client(string url, HttpClient client)
        {
            _url = url;
            _client = client;
        }
        public bool IsLoaded { get; private set; }

        public async Task LoadAsync()
        {
            using var response = await _client.GetStreamAsync(_url + "?f=json");
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            var directory = JsonSerializer.Deserialize<ServerDirectory>(response, options);
            Directory = directory;
            IsLoaded = true;
        }

        public ServerDirectory? Directory { get; private set; }
    }
    public struct ServerDirectory
    {
        [JsonConstructor]
        public ServerDirectory(double currentVersion, ArcGISService[] services, string[] folders) =>
            (CurrentVersion, Services, Folders) = (currentVersion, services, folders);

        public double CurrentVersion { get; }

        public ArcGISService[] Services { get; }

        public string[] Folders { get; }
    }
    public struct ArcGISService
    {
        [JsonConstructor]
        public ArcGISService(string name, string type) =>
            (Name, Type) = (name, type);

        public string Name { get; }

        public string Type { get; }
    }
}
