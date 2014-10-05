using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixivUtilCS
{
    class Program
    {
        static void Main(string[] args)
        {
            Pixiv pixiv = new Pixiv();

            Console.WriteLine("PixivDownloader v1.0");
            Console.WriteLine("--------------------");

            String search = String.Empty;

            while(search == String.Empty)
            {
                Console.WriteLine("Enter the image tags you would like to search for: ");
                Console.Write("Input: ");
                search = Console.ReadLine();
            }

            pixiv.Search(search);

            pixiv.DownloadImages();

            Console.WriteLine("Download completed, press any key to continue.");
            Console.Read();
        }
    }
}
