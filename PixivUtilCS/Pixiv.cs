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

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                if (request is HttpWebRequest)
                {
                    (request as HttpWebRequest).CookieContainer = cookie;
                }
                return request;
            }
        }

        CookieAwareWebClient client;

        List<Illustration> Illustrations = new List<Illustration>();
        CurrentState state;

        public Pixiv(String username, String password)
        {
            if(!Login(username, password))
            {
                throw new ArgumentException();
            }

            state = new CurrentState();
        }

        public void DownloadImages(System.ComponentModel.BackgroundWorker worker,
        System.ComponentModel.DoWorkEventArgs e, String tags, bool r18, ImageSearchOptions imageSearchOptions, int currentPage, int maximumPagesToDownload)
        {
            List<Illustration> illusts = new List<Illustration>();
            do
            {
                illusts = Search(tags, r18, imageSearchOptions, currentPage);
                Illustrations.AddRange(illusts);
                currentPage++;
                state.Status = Illustrations.Count + " images found...";
                worker.ReportProgress(0, state);
            } while (illusts.Count == 20 && currentPage <= maximumPagesToDownload);

            if (Illustrations.Count > 0)
            {
                int count = 0;
                foreach (Illustration i in Illustrations)
                {
                    state.Status = "Images downloaded: " + count++ + "  Downloading image id: " + i.IllustrationID;
                    worker.ReportProgress(0, state);
                    i.DownloadImage();
                }
            }
            else
            {
                state.Status = "No images to download!";
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

        //Returns a list of illustrations on a given page
        public List<Illustration> Search(String tags, bool r18, ImageSearchOptions imageSearchOptions, int currentPage)
        {
            string result = "";
            string url = "search.php?" + "word=" + tags + "&order=date_d" + (r18 ? "&r18=1" : "");

            if (imageSearchOptions == ImageSearchOptions.ILLUSTRATIONS) {
                result = client.DownloadString(url + "&type=illust&p=" + currentPage);
            }
            else if (imageSearchOptions == ImageSearchOptions.ALL) {
                result = client.DownloadString(url + "&type=0&p=" + currentPage);
            }
            else if (imageSearchOptions == ImageSearchOptions.MANGA) {
                result = client.DownloadString(url + "&type=manga&p=" + currentPage);
            }

            HtmlAgilityPack.HtmlDocument HTMLParser = new HtmlAgilityPack.HtmlDocument();
            HTMLParser.LoadHtml(result);

            String s = "http://spapi.pixiv.net/iphone/illust.php?illust_id=";

            var imageAttributes = GetImageAttributes(HTMLParser).ToArray();
            List<Illustration> illustrations = new List<Illustration>();

            foreach (ImageAttributes i in imageAttributes)
            {
                illustrations.Add(new Illustration(client.DownloadString(s + i.IllustrationID)));
            }

            return illustrations;
        }

        public String getNumberOfResultsFound(HtmlAgilityPack.HtmlDocument page)
        {
            var metaTags = page.DocumentNode.SelectNodes("//span");

            foreach (HtmlNode h in metaTags)
            {
                if (h.InnerText.Contains("result") && !h.InnerText.Contains(" "))
                {
                    return h.InnerText + " found"; //prints "x results found"
                }
            }
            return null;
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
