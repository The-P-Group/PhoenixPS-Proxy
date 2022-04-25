using System.Net;
using System.Configuration;
using System.Linq;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

var forwardUrl = ConfigurationManager.AppSettings["FORWARD_URL"] ?? "http://localhost";
var listenPort = Int32.Parse(ConfigurationManager.AppSettings["PROXY_LISTEN_PORT"] ?? "8080");
var parseBody = bool.Parse(ConfigurationManager.AppSettings["PARSE_BODY"] ?? "false");

Task OnRequest(object sender, SessionEventArgs e)
{
    string[] listDomain = { "mihoyo", "hoyoverse", "yuanshen", "18.222.82.186:22401" };

    if (listDomain.Any(e.HttpClient.Request.RequestUri.AbsoluteUri.Contains))
    {

        if (parseBody && e.HttpClient.Request.Method.ToUpper() == "POST")
        {
            Console.WriteLine(e.GetRequestBodyAsString().Result);
        }

        var hostUrl = e.HttpClient.Request.Host is not null ? e.HttpClient.Request.Host : e.HttpClient.Request.RequestUri.Host;

        var abPath = e.HttpClient.Request.RequestUri.AbsolutePath;

        e.HttpClient.Request.RequestUri = new Uri(forwardUrl + abPath);

        Console.WriteLine("Redirected {0} to {1}", hostUrl, forwardUrl.Replace("http://", ""));
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