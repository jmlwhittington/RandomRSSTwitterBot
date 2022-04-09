using System;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Runtime;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using System.IO;

namespace RandomRSSTwitterBot
{
	class RandomRSS
    {
        private static string path = Directory.GetCurrentDirectory();
        private static string now = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
        private static List<string> titles = new List<string>();
        private static List<Uri> links = new List<Uri>();
        private static int i;
        private static int urlNum;
        private static bool failure = false;
        private static int attempts = 0;
        private static Random rand = new Random();
        static async Task Main(string[] args)
        {
            if (!Directory.Exists(path + "/logs"))
            {
                Directory.CreateDirectory(path + "/logs");
            }
            File.WriteAllText(path + "/logs/" + now + ".txt", "");
            if (!File.Exists(path + "/keys.txt"))
            {
                File.WriteAllText(path + "/keys.txt", "<CONSUMER_KEY>" + Environment.NewLine + "<CONSUMER_SECRET>" + Environment.NewLine + "<ACCESS_TOKEN>" + Environment.NewLine + "<ACCESS_TOKEN_SECRET>");
                Console.WriteLine(DateTime.Now + ": You need to input your authentication keys in keys.txt!" + Environment.NewLine + "Press any key to exit...");
                File.AppendAllText(path + "/logs/" + now + ".txt", DateTime.Now + ": You need to input your authentication keys in keys.txt!" + Environment.NewLine + "Press any key to exit..." + Environment.NewLine);
                Console.Read();
                Environment.Exit(0);
            }
            if (!File.Exists(path + "/sources.txt"))
            {
                File.WriteAllText(path + "/sources.txt", "https://news.google.com/rss/topics/CAAqJggKIiBDQkFTRWdvSUwyMHZNRGRqTVhZU0FtVnVHZ0pWVXlnQVAB");
            }
            if (!File.Exists(path + "/postings.txt"))
            {
                File.WriteAllText(path + "/postings.txt", "");
            }
            await Seek();
        }
        static async Task Seek()
        {
            string[] sources = File.ReadAllLines(path + "/sources.txt");
            urlNum = rand.Next(0, sources.Length);
            string url = sources[urlNum];
            XmlReader read = XmlReader.Create(url);
            SyndicationFeed rss = SyndicationFeed.Load(read);
            read.Close();
            foreach (SyndicationItem item in rss.Items)
            {
                titles.Add(item.Title.Text);
                links.Add(new Uri(item.Links[0].Uri.ToString()));
            }
            i = rand.Next(0, titles.Count);
            foreach (string item in File.ReadAllLines(path + "/postings.txt"))
            {
                if (titles[i] == item)
                {
                    failure = true;
                }
            }
            if (failure == false)
            {
                await Tweet();
            }
            else
            {
                if (attempts < 100)
                {
                    Console.WriteLine(DateTime.Now + ": Duplicate chosen, choosing again... (Attempt number: " + attempts + ")");
                    File.AppendAllText(path + "/logs/" + now + ".txt", DateTime.Now + ": Duplicate chosen, choosing again... (Attempt number: " + attempts + ")" + Environment.NewLine);
                    await Seek();
                }
                else
                {
                    Console.WriteLine(DateTime.Now + ": Failure to find suitable article within 100 attempts, press any key to exit...");
                    File.AppendAllText(path + "/logs/" + now + ".txt", DateTime.Now + ": Failure to find suitable article within 100 attempts, press any key to exit..." + Environment.NewLine);
                    Console.Read();
                    Environment.Exit(0);
                }
            }
        }
        static async Task Tweet()
        {
            Console.WriteLine(DateTime.Now + ": Suitable article found!" + Environment.NewLine + titles[i] + Environment.NewLine + links[i]);
            File.AppendAllText(path + "/logs/" + now + ".txt", DateTime.Now + ": Suitable article found!" + Environment.NewLine + titles[i] + Environment.NewLine + links[i] + Environment.NewLine);
            File.AppendAllText(path + "/postings.txt", titles[i] + Environment.NewLine);
            string[] keys = File.ReadAllLines(path + "/keys.txt");
            var userClient = new TwitterClient(keys[0], keys[1], keys[2], keys[3]);
            var user = await userClient.Users.GetAuthenticatedUserAsync();
            var tweet = await userClient.Tweets.PublishTweetAsync(titles[i] + "\n" + links[i]);
        }
    }
}
