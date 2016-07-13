using CloudFareBypass.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CloudFareBypass
{
    public class CloudFareWebClient : IDisposable
    {
        public WebClientEx BaseWebClient { get; private set; }
        public string UserAgent { get; set; }

        public CloudFareWebClient()
        {
            UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:45.0) Gecko/20100101 Firefox/45.0";

            BaseWebClient = new WebClientEx();
            BaseWebClient.CookieContainer = new CookieContainer();
            //BaseWebClient.Headers.Add(HttpRequestHeader.UserAgent, UserAgent);
        }

        public string DownloadString(Uri address)
        {
            try
            {
                //UserAgent must be the same throughout all requests
                BaseWebClient.Headers.Add(HttpRequestHeader.UserAgent, UserAgent);

                return BaseWebClient.DownloadString(address);
            }
            catch (WebException ex)
            {
                if(ex.Status != WebExceptionStatus.ProtocolError)
                {
                    throw;
                }

                HttpWebResponse httpResponse = (HttpWebResponse)ex.Response;

                if (httpResponse.StatusCode != HttpStatusCode.ServiceUnavailable)
                {
                    throw;
                }

                using (StreamReader stream = new StreamReader(ex.Response.GetResponseStream()))
                {
                    string response = stream.ReadToEnd();

                    List<CFValues> inputs = RegexScraper.ScrapeFromString<CFValues>(response, @"name=""(?<Name>.*?)"" value=""(?<Value>.*?)""");
                    List<string> script = RegexScraper.ScrapeFromString<string>(response, @"<script.*?>(?<val>.*?)</script>", options: RegexOptions.Singleline);

                    //Shouldn't be needed, but causes no harm
                    List<string> action = RegexScraper.ScrapeFromString<string>(response, @"action=""(?<val>.*?)""");

                    if(inputs.Count == 0 || script.Count == 0 || action.Count == 0)
                    {
                        //Site's different or page changed
                        throw;
                    }

                    BaseWebClient.Headers.Add(HttpRequestHeader.Referer, address.ToString());
                    BaseWebClient.Headers.Add(HttpRequestHeader.UserAgent, UserAgent);

                    string baseAddress = address.GetLeftPart(UriPartial.Authority);
                    string solveChallengeAddress = baseAddress + action[0];

                    CFChallenge challenge = new CFChallenge(script[0], address);

                    BaseWebClient.QueryString.Add("jschl_answer", challenge.SolveChallenge().ToString());

                    foreach(CFValues value in inputs)
                    {
                        BaseWebClient.QueryString.Add(value.Name, WebUtility.UrlEncode(value.Value));
                    }

                    Thread.Sleep(3000);

                    string siteResponse = BaseWebClient.DownloadString(solveChallengeAddress);

                    BaseWebClient.QueryString.Clear();

                    return siteResponse;
                }
            }
        }

        public string DownloadString(string address)
        {
            return DownloadString(new Uri(address));
        }

        #region AsyncMethods

        public async Task<string> DownloadStringTaskAsync(string address)
        {
            return await Task.Run(() => DownloadString(address));
        }

        public async Task<string> DownloadStringTaskAsync(Uri address)
        {
            return await Task.Run(() => DownloadString(address));
        }

        #endregion

        public void Dispose()
        {
            BaseWebClient.Dispose();
        }
    }
}
