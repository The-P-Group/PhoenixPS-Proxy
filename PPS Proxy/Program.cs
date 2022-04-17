using System.Net;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

var proxyServer = new ProxyServer();

// locally trust root certificate used by this proxy 
proxyServer.CertificateManager.CertificateEngine = Titanium.Web.Proxy.Network.CertificateEngine.DefaultWindows;
proxyServer.CertificateManager.EnsureRootCertificate();
proxyServer.CertificateManager.TrustRootCertificate(true);

Task OnRequest(object sender, SessionEventArgs e)
{
    string[] listDomain = { "mihoyo", "hoyoverse", "yuanshen", "18.222.82.186:22401" };

    // TODO: Improve this fukking logic
    for (int i = 0; i < listDomain.Length; i++)
    {
        if (e.HttpClient.Request.RequestUri.AbsoluteUri.Contains(listDomain[i]))
        {

#if DEBUG
            var method = e.HttpClient.Request.Method.ToUpper();
            if (method == "POST")
            {
                Console.WriteLine(e.GetRequestBodyAsString().Result);
            }
#endif

            var https = e.HttpClient.Request.IsHttps;

            var hostUrl = e.HttpClient.Request.Host;
            var fullUrl = e.HttpClient.Request.Url.ToString();

            var newUrl = fullUrl.Replace(hostUrl, "localhost");

            if (https)
            {
                newUrl = newUrl.Replace("https", "http");
            }

            //Console.WriteLine(newUrl);

            Console.WriteLine("Redirect " + hostUrl + " to localhost");

            e.Redirect(newUrl);

        }
    }

    return Task.CompletedTask;
}

proxyServer.BeforeRequest += OnRequest;


var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8080, true)
{
    // Use self-issued generic certificate on all https requests
    // Optimizes performance by not creating a certificate for each https-enabled domain
    // Useful when certificate trust is not required by proxy clients
    //GenericCertificate = new X509Certificate2(Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "genericcert.pfx"), "password")
};

proxyServer.AddEndPoint(explicitEndPoint);
proxyServer.Start();

foreach (var endPoint in proxyServer.ProxyEndPoints)
    Console.WriteLine("Listening on '{0}' endpoint at Ip {1} and port: {2} ",
        endPoint.GetType().Name, endPoint.IpAddress, endPoint.Port);

// Only explicit proxies can be set as system proxy!
proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);

// wait here (You can use something else as a wait function)
Console.Read();

proxyServer.BeforeRequest -= OnRequest;


proxyServer.Stop();

// Restore original proxy setting
proxyServer.RestoreOriginalProxySettings();