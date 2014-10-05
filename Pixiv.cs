using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PixivUtilCS
{
    public class Pixiv
    {
        bool loggedIn = false;
        string mode = "login";
        string url = "https://www.secure.pixiv.net/login.php";
        string searchURL = "http://spapi.pixiv.net/iphone/search.php?s_mode=s_tag&word={0}&p={1}";
        string pixiv_id = "YourUserHere";
        string password = "YourPassHere";
        int skip = 1;
        int PixivImageCap = 200;

        List<Illustration> Illustrations = new List<Illustration>();

        public Pixiv()
        {

        }

        public void Search(String tags)
        {
            WebClient Client = new WebClient();
            String downloadedString = "";

            try
            {
                int PageCount = 1;
                while (true)
                {
                    //searchURL = "http://spapi.pixiv.net/iphone/search.php?s_mode=s_tag&word=" + tags;
                    searchURL = string.Format(searchURL, tags, PageCount);
                    downloadedString = Client.DownloadString(searchURL);
                    int IllustrationsOnPage = 0;

                    foreach (String illustrationString in downloadedString.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        Illustrations.Add(new Illustration(illustrationString));
                        IllustrationsOnPage++;
                    }
                    Console.WriteLine("Search in progress. Found " + Illustrations.Count + " illustrations matching your tags.");
                    
                    //continue to next page if more than 50 illustrations were found, or if the image cap was reached
                    if(IllustrationsOnPage < 50 || Illustrations.Count >= PixivImageCap)
                    {
                        if (Illustrations.Count >= PixivImageCap)
                        {
                            Console.WriteLine("Image cap reached, ending search.");
                        }
                        break;
                    }
                    PageCount++;
                }
                Console.WriteLine("Search finished. Found " + Illustrations.Count + " illustrations matching your tags.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Search failed.");
            }
        }

        public void DownloadImages()
        {
            if (Illustrations.Count > 0)
            {
                foreach (Illustration i in Illustrations)
                {
                    i.DownloadImage();
                }
            }
            else
            {
                Console.WriteLine("No images to download!");
            }
        }

        //Credits to http://www.dreamincode.net/forums/topic/152297-c%23-log-in-to-website-programmatically/
        private void Login()
        {
            WebBrowser b = new WebBrowser();
            b.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(b_DocumentCompleted);
            b.Navigate(url);
        }

        private void b_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser b = sender as WebBrowser;
            string response = b.DocumentText;

            // unregisters the first event handler
            // adds a second event handler
            b.DocumentCompleted -= new WebBrowserDocumentCompletedEventHandler(b_DocumentCompleted);
            b.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(b_DocumentCompleted2);

            // format our data that we are going to post to the server
            // this will include our post parameters.  They do not need to be in a specific
            //	order, as long as they are concatenated together using an ampersand ( & )
            string postData = string.Format("mode={0}&pixiv_id={1}&pass={2}&skip={3}", mode, pixiv_id, password, skip);
            ASCIIEncoding enc = new ASCIIEncoding();

            //  we are encoding the postData to a byte array
            b.Navigate("https://www.secure.pixiv.net/login.php", "", enc.GetBytes(postData), "Content-Type: application/x-www-form-urlencoded\r\n");
        }

        private void b_DocumentCompleted2(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser b = sender as WebBrowser;
            string response = b.DocumentText;

            //Console.WriteLine(b.DocumentTitle);

            if (response.Contains("pixiv.user.loggedIn = true"))
            {
                loggedIn = true;
            }
        }
    }
}
