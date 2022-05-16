using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using IniParser;
using IniParser.Model;

var parser = new FileIniDataParser();
IniData appConfig = parser.ReadFile("Configuration.ini");

string forwardHost = appConfig["AppConfiguration"]["FORWARD_HOST"] ?? "localhost";
string schemaUrl = appConfig["AppConfiguration"]["SCHEMA_URL"] ?? "http";
string forceSchema = appConfig["AppConfiguration"]["FORCE_SCHEMA"] ?? "false";
int listenPort = Int32.Parse(appConfig["AppConfiguration"]["PROXY_PORT"] ?? "8080");
bool parseBody = bool.Parse(appConfig["AppConfiguration"]["PARSE_BODY"] ?? "false");

Task OnRequest(object sender, SessionEventArgs e)
{
    string[] listDomain = { "mihoyo", "hoyoverse", "yuanshen", "18.222.82.186:22401" };

    if (listDomain.Any(e.HttpClient.Request.RequestUri.AbsoluteUri.Contains))
    {

        if (parseBody && e.HttpClient.Request.Method.ToUpper() == "POST")
        {
            Console.WriteLine(e.GetRequestBodyAsString().Result);
        }

        string requestSchema = forceSchema is "true" ? schemaUrl : e.HttpClient.Request.RequestUri.Scheme;

        string hostUrl = e.HttpClient.Request.Host is not null ? e.HttpClient.Request.Host : e.HttpClient.Request.RequestUri.Host;

        string abPath = e.HttpClient.Request.RequestUri.AbsolutePath;

        var builder = new UriBuilder();

        builder.Scheme = requestSchema;
        builder.Host = forwardHost;
        builder.Path = abPath;

        e.HttpClient.Request.RequestUri = builder.Uri;

        Console.WriteLine("Redirected {0} to {1}", hostUrl, forwardHost);
    }

    return Task.CompletedTask;
}

void Main()
{
    var proxyServer = new ProxyServer();

    // locally trust root certificate used by this proxy 
    proxyServer.CertificateManager.CertificateEngine = Titanium.Web.Proxy.Network.CertificateEngine.DefaultWindows;
    proxyServer.CertificateManager.EnsureRootCertificate();
    proxyServer.CertificateManager.TrustRootCertificate(true);

    proxyServer.BeforeRequest += OnRequest;

    var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, listenPort, true) { };

    proxyServer.AddEndPoint(explicitEndPoint);
    proxyServer.Start();

    foreach (var endPoint in proxyServer.ProxyEndPoints)
    {
        Console.WriteLine("Listening on '{0}' endpoint at Ip {1} and port: {2} ",
            endPoint.GetType().Name, endPoint.IpAddress, endPoint.Port);
    }

    // Only explicit proxies can be set as system proxy!
    proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
    proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);

    Console.WriteLine("Press any to stop program !");
    Console.Read();

    proxyServer.BeforeRequest -= OnRequest;

    proxyServer.Stop();

    // Restore original proxy setting
    proxyServer.RestoreOriginalProxySettings();

}

Main();