using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using IniParser;
using IniParser.Model;
using System.Web;

var parser = new FileIniDataParser();
IniData appConfig = parser.ReadFile("Configuration.ini");

string forwardHost = appConfig["AppConfiguration"]["FORWARD_HOST"] ?? "localhost";
string schemaUrl = appConfig["AppConfiguration"]["SCHEMA_URL"] ?? "http";
string forceSchema = appConfig["AppConfiguration"]["FORCE_SCHEMA"] ?? "false";
int listenPort = Int32.Parse(appConfig["AppConfiguration"]["PROXY_PORT"] ?? "8080");
bool parseBody = bool.Parse(appConfig["AppConfiguration"]["PARSE_BODY"] ?? "false");

Task OnRequest(object sender, SessionEventArgs e)
{
    string[] listDomain = { "mihoyo.com", "hoyoverse.com", "yuanshen.com", "18.222.82.186:22401", "starrails.com" };

    if (listDomain.Any(e.HttpClient.Request.RequestUri.AbsoluteUri.Contains))
    {

        if (parseBody && e.HttpClient.Request.Method.ToUpper() == "POST")
        {
            Console.WriteLine(e.GetRequestBodyAsString().Result);
        }

        string requestSchema = forceSchema is "true" ? schemaUrl : e.HttpClient.Request.RequestUri.Scheme;

        string hostUrl = e.HttpClient.Request.Host is not null ? e.HttpClient.Request.Host : e.HttpClient.Request.RequestUri.Host;

        string path = e.HttpClient.Request.RequestUri.AbsolutePath;
        string query = e.HttpClient.Request.RequestUri.Query;

        var builder = new UriBuilder();

        builder.Scheme = requestSchema;
        builder.Host = forwardHost;
        builder.Path = path;
        builder.Query = query;

        var fullUrl = e.HttpClient.Request.Url.ToString();

        var newUrl = fullUrl.Replace(hostUrl, "localhost");



        //Console.WriteLine(builder.ToString());

        e.HttpClient.Request.RequestUri = new Uri(builder.Uri.ToString());
        //e.HttpClient.Request.RequestUri = new Uri(newUrl);

        //e.HttpClient.Request.Host = "localhost";

        Console.WriteLine(e.HttpClient.Request.Url.ToString());

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