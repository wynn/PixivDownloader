using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using HtmlAgilityPack;

namespace PixivUtilCS
{
    public class Illustration
    {
        public String IllustrationID = "";
        public String ArtistID = "";
        public String FileFormat = "";
        public String Title = "";
        public String UnknownNumber = ""; 
        public String ArtistName = "";
        public String SmallImageURL = "";
        public String BigImageURL = "";

        public String DatePublished = "";
        public String Program = "";
        public String[] tags; 
        public int NumberOfPages = 0;
        public String Ratings = "";
        public String Total = "";
        public String Views = "";
        public String Description = "";
        public bool isManga = false;
        public String authorName = "";
        public String bookmarks = "";

        public String URLBase = "http://www.pixiv.net/member_illust.php?mode=medium&illust_id=";

        public Illustration(String illustrationString)
        {
            List<String> IllustrationComponents = new List<String>();

            String[] illustrationStringParts = illustrationString.Split(',');


            foreach (String IllustrationComponent in illustrationStringParts)
            {
                IllustrationComponents.Add(IllustrationComponent.Replace("\"", "").Trim());
            }

            try
            {
                if (IllustrationComponents.Count == 31)
                {
                    this.IllustrationID = IllustrationComponents[0];
                    this.ArtistID = IllustrationComponents[1];
                    this.FileFormat = IllustrationComponents[2];
                    this.Title = IllustrationComponents[3];
                    this.UnknownNumber = IllustrationComponents[4];
                    this.ArtistName = IllustrationComponents[5];
                    this.SmallImageURL = IllustrationComponents[6];
                    this.BigImageURL = IllustrationComponents[9];
                    this.DatePublished = IllustrationComponents[12];
                    this.tags = IllustrationComponents[13].Split(' ');
                    this.Program = IllustrationComponents[14];
                    this.Ratings = IllustrationComponents[15];
                    this.Total = IllustrationComponents[16];
                    this.Views = IllustrationComponents[17];
                    this.Description = IllustrationComponents[18];
                    if (IllustrationComponents[19] != String.Empty)
                    {
                        this.NumberOfPages = Convert.ToInt32(IllustrationComponents[19]);
                        this.isManga = true;
                    }
                    this.bookmarks = IllustrationComponents[22];
                }

                else
                {
                    if (IllustrationComponents.Count < 31)
                    {
                        Console.WriteLine("Error parsing input string: input string too short, it is likely no results were found.");
                    }

                    //Commas are present in the tags, need to get image data another way
                    else if (IllustrationComponents.Count > 31)
                    {
                        this.IllustrationID = IllustrationComponents[0];
                        this.ArtistID = IllustrationComponents[1];
                        this.FileFormat = IllustrationComponents[2];
                        this.Title = IllustrationComponents[3];
                        this.UnknownNumber = IllustrationComponents[4];
                        this.ArtistName = IllustrationComponents[5];
                        this.SmallImageURL = IllustrationComponents[6];
                        this.BigImageURL = IllustrationComponents[9];
                        this.DatePublished = IllustrationComponents[12];
                        this.tags = IllustrationComponents[13].Split(' ');

                        this.URLBase += this.IllustrationID;
                        HtmlDocument doc = new HtmlDocument();

                        WebClient Client = new WebClient();
                        String downloadedString = "";

                        try
                        {
                            downloadedString = Client.DownloadString(URLBase);
                        }
                        catch { }

                        doc.LoadHtml(downloadedString);

                        if (doc.DocumentNode.InnerText.Contains("manga"))
                        {
                            this.isManga = true;
                            NumberOfPages = Convert.ToInt32(illustrationStringParts[illustrationStringParts.Length - 12]);
                        }
                    }
                }
            }

            catch
            {
                Console.WriteLine("Error parsing input string");
            }
        }

        

        public void DownloadImage()
        {
            string localFilename = Directory.GetCurrentDirectory() + @"\Downloaded Images\" + IllustrationID + "." + FileFormat;
            string path = Directory.GetCurrentDirectory() + @"\Downloaded Images\";

            if (!Directory.Exists(path))
            {
                DirectoryInfo di = Directory.CreateDirectory(path);
            }

            using (WebClient client = new WebClient())
            {
                try
                {
                    if (isManga)
                    {
                        for (int i = 0; i < NumberOfPages; i++)
                        {
                            if (!File.Exists(Directory.GetCurrentDirectory() + @"\Downloaded Images\" + IllustrationID + "_p" + i + "." + this.FileFormat))
                            {
                                String s = this.BigImageURL.Replace("mobile/", "").Replace("480mw", "p" + i).Replace(".jpg", "." + this.FileFormat);
                                Console.WriteLine("Image url: " + s);
                                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(s);

                                myRequest.CookieContainer = new CookieContainer();

                                myRequest.Method = "GET";
                                myRequest.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; zh-CN; rv:1.9.0.6) Gecko/2009011913 Firefox/3.0.6";
                                myRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                                myRequest.Headers.Add("Accept-Language", "zh-cn,zh;q=0.7,ja;q=0.3");
                                myRequest.Headers.Add("Accept-Encoding", "gzip,deflate");
                                myRequest.Headers.Add("Accept-Charset", "gb18030,utf-8;q=0.7,*;q=0.7");
                                myRequest.Referer = "http://www.pixiv.net/member_illust.php?mode=manga&illust_id=38889015";

                                // Get response
                                HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();

                                StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);


                                using (var fileStream = File.Create(Directory.GetCurrentDirectory() + @"\Downloaded Images\" + IllustrationID + "_p" + i + "." + this.FileFormat))
                                {
                                    Console.WriteLine("Downloading image " + (i + 1) + " out of " + NumberOfPages + " for illustration ID: " + IllustrationID);
                                    myResponse.GetResponseStream().CopyTo(fileStream);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Duplicate file detected in downloaded images folder.");
                            }
                        }
                    }

                    else
                    {
                        if (!File.Exists(Directory.GetCurrentDirectory() + @"\Downloaded Images\" + IllustrationID + "." + this.FileFormat))
                        {
                            String s = this.BigImageURL.Replace("mobile/", "").Replace("_480mw", "").Replace(".jpg", "." + this.FileFormat);
                            Console.WriteLine("Image url: " + s);
                            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(s);

                            myRequest.CookieContainer = new CookieContainer();

                            myRequest.Method = "GET";
                            myRequest.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; zh-CN; rv:1.9.0.6) Gecko/2009011913 Firefox/3.0.6";
                            myRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                            myRequest.Headers.Add("Accept-Language", "zh-cn,zh;q=0.7,ja;q=0.3");
                            myRequest.Headers.Add("Accept-Encoding", "gzip,deflate");
                            myRequest.Headers.Add("Accept-Charset", "gb18030,utf-8;q=0.7,*;q=0.7");
                            myRequest.Referer = "http://www.pixiv.net/member_illust.php?mode=manga&illust_id=38889015";

                            // Get response
                            HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();

                            StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);

                            using (var fileStream = File.Create(Directory.GetCurrentDirectory() + @"\Downloaded Images\" + IllustrationID + "." + this.FileFormat))
                            {
                                Console.WriteLine("Downloading illustration ID: " + IllustrationID);
                                Console.WriteLine();
                                myResponse.GetResponseStream().CopyTo(fileStream);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Duplicate file detected in downloaded images folder.");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Error happened for image id: " + IllustrationID);
                }
            }
        }
    }
}