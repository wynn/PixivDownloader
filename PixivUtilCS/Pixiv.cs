using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Collections.Specialized;
using HtmlAgilityPack;

namespace PixivUtilCS
{
    public class CurrentState
    {
        public String Status;
    }

    public class Pixiv
    {
        public enum ImageSearchOptions
        {
            ILLUSTRATIONS,
            MANGA, 
            ALL
        }

        struct AuthorAttributes
        {
            public String ArtistID;
            public String ArtistName;
        }

        struct ImageAttributes
        {
            public String IllustrationID;
            public String Title;
        }

        public class CookieAwareWebClient : WebClient
        {
            private CookieContainer cookie = new CookieContainer();
            private Cookie PHPSESSID = new Cookie();

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                if (request is HttpWebRequest)
                {
                    (request as HttpWebRequest).CookieContainer = cookie;
                }
                if (cookie.Count > 0)
                {
                    PHPSESSID = cookie.GetCookies(address)[0];
                }
                return request;
            }

            public Cookie getPHPSESSID()
            {
                return PHPSESSID;
            }
        }

        CookieAwareWebClient client;

        List<Illustration> Illustrations = new List<Illustration>();
        CurrentState state;
        const int ImagesPerPage = 20;

        public Pixiv(String username, String password)
        {
            if(!Login(username, password))
            {
                throw new ArgumentException();
            }

            state = new CurrentState();
        }

        public void DownloadImages(String[] illustNums )
        {
            Illustration i = new Illustration(illustNums);
            i.DownloadImage();

        }

        public void DownloadImages(System.ComponentModel.BackgroundWorker worker,
        System.ComponentModel.DoWorkEventArgs e, String tags, bool r18, ImageSearchOptions imageSearchOptions, int currentPage, int maximumPagesToDownload)
        {
            List<Illustration> illusts = new List<Illustration>();
            int resultsFound = getNumberOfResultsFound(tags, r18, imageSearchOptions);   
            int totalPages = resultsFound / ImagesPerPage + (resultsFound % 20 == 0 ? 0 : 1);
            int imagesDownloaded = 0;

            if (resultsFound <= 0)
            {
                state.Status = "No results found!";
                worker.ReportProgress(0, state);
                return;
            }

            state.Status = resultsFound + " results found.";
            worker.ReportProgress(0, state);

            for (int x = 0; x <= maximumPagesToDownload && x <= totalPages; x++)
            {
                foreach (Illustration i in IllustrationsOnPage(tags, r18, imageSearchOptions, currentPage))
                {
                    state.Status = "Images downloaded: " + imagesDownloaded++ + "  Downloading image id: " + i.IllustrationID;
                    worker.ReportProgress(0, state);
                    i.DownloadImage();
                }
                currentPage++;
            }
        }

        private bool Login(String username, String password)
        {
            client = new CookieAwareWebClient();
            client.BaseAddress = @"http://www.pixiv.net/";
            NameValueCollection loginData = new NameValueCollection();
            loginData.Add("mode", "login");
            loginData.Add("pixiv_id", username);
            loginData.Add("pass", password);
            loginData.Add("skip", "1");
            client.UploadValues("https://www.secure.pixiv.net/login.php", "POST", loginData);
 
            string htmlSource = client.DownloadString("mypage.php");
            Console.WriteLine(client.DownloadString("mypage.php").Contains("pixiv.user.loggedIn = true"));
            return client.DownloadString("mypage.php").Contains("pixiv.user.loggedIn = true");
        }

        public HtmlAgilityPack.HtmlDocument Search(String tags, bool r18, ImageSearchOptions imageSearchOptions, int currentPage)
        {
            string result = "";
            string url = "search.php?" + "word=" + tags + "&order=date_d" + (r18 ? "&r18=1" : "");

            if (imageSearchOptions == ImageSearchOptions.ILLUSTRATIONS)
            {
                result = client.DownloadString(url + "&type=illust&p=" + currentPage);
            }
            else if (imageSearchOptions == ImageSearchOptions.ALL)
            {
                result = client.DownloadString(url + "&type=0&p=" + currentPage);
            }
            else if (imageSearchOptions == ImageSearchOptions.MANGA)
            {
                result = client.DownloadString(url + "&type=manga&p=" + currentPage);
            }

            HtmlAgilityPack.HtmlDocument page = new HtmlAgilityPack.HtmlDocument();
            page.LoadHtml(result);

            return page;
        }

        //Returns a list of illustrations on a given page
        public List<Illustration> IllustrationsOnPage(String tags, bool r18, ImageSearchOptions imageSearchOptions, int currentPage)
        {
            HtmlAgilityPack.HtmlDocument HTMLParser = Search(tags, r18, imageSearchOptions, currentPage);

            String s = "http://spapi.pixiv.net/iphone/illust.php?illust_id=";

            var imageAttributes = GetImageAttributes(HTMLParser).ToArray();
            List<Illustration> illustrations = new List<Illustration>();

            foreach (ImageAttributes i in imageAttributes)
            {
                illustrations.Add(new Illustration(client.DownloadString(s + i.IllustrationID + "&" + client.getPHPSESSID())));
            }

            return illustrations;
        }

        public int getNumberOfResultsFound(String tags, bool r18, ImageSearchOptions imageSearchOptions)
        {
            HtmlAgilityPack.HtmlDocument HTMLParser = Search(tags, r18, imageSearchOptions, 1);

            var metaTags = HTMLParser.DocumentNode.SelectNodes("//span");

            foreach (HtmlNode h in metaTags)
            {
                if (h.InnerText.Contains("result") && !h.InnerText.Contains(" "))
                {
                    return Convert.ToInt32(h.InnerText.Replace("results", ""));
                }
            }
            return -1;
        }

        IEnumerable<AuthorAttributes> GetAuthorAttributes(HtmlAgilityPack.HtmlDocument page)
        {
            return from lnks in page.DocumentNode.Descendants()
                   where lnks.Name == "a" &&
                        lnks.Attributes["data-user_id="] != null &&
                        lnks.Attributes["data-user_name="] != null
                   select new AuthorAttributes
                   {
                       ArtistID = lnks.Attributes["data-user_id="].Value,
                       ArtistName = lnks.Attributes["data-user_name="].Value
                   };
        }

        IEnumerable<ImageAttributes> GetImageAttributes(HtmlAgilityPack.HtmlDocument page)
        {
            return from lnks in page.DocumentNode.Descendants()
                   where lnks.Name == "a" &&
                        lnks.Attributes["href"] != null &&
                        lnks.Attributes["href"].Value.Contains(";illust_id") &&
                        !lnks.Attributes["href"].Value.Contains("showcase") &&
                        lnks.InnerText.Trim().Length > 0
                   select new ImageAttributes
                   {
                       IllustrationID = lnks.Attributes["href"].Value.Substring(lnks.Attributes["href"].Value.IndexOf("id=") + "id=".Length),
                       Title = lnks.InnerText
                   };
        }
    }
}
