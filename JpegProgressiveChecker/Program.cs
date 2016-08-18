using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Codeplex.Data;

namespace JpegProgressiveChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            IList<Task> tasklist = new List<Task>();
            foreach (var s in args)
            {
                //Console.WriteLine(s);
                if (s.Contains("https://hooks.slack.com/"))
                {
                    enableSlack = true;
                    slackIntgUrl = s.Replace("\"","");
                }
                else if (Directory.Exists(s))
                {
                    foreach (var file in Directory.EnumerateFiles(s, "*.jpg", SearchOption.AllDirectories))
                    {
                        tasklist.Add(CheckJpegProgressive(file));
                    }
                }
                else if (File.Exists(s))
                {
                    if (s.ToLower().EndsWith(".jpg"))
                        tasklist.Add(CheckJpegProgressive(s));
                }
            }
            Task.WhenAll(tasklist).Wait();
        }

        private const int Bufsize = 4096;//?
        private static bool enableSlack = false;
        private static string slackIntgUrl = "";

        //32    slow
        //64    1:19
        //128   slow
        //2048  55
        //4096  48

        static async Task<bool> IsJpegProgressive(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                var sofFlag = false;
                byte[] buf = new byte[Bufsize];
                while (true)
                {
                    var read = await stream.ReadAsync(buf, 0, Bufsize);
                    foreach (var b in buf)
                    {
                        if (sofFlag)
                        {
                            if (b == 0xC2)
                                return true;
                            sofFlag = false;
                        }
                        if (b == 0xFF)
                            sofFlag = true;
                    }
                    if (read <= 0)
                        break;
                };
            }
            return false;
        }

        static async Task CheckJpegProgressive(string filePath)
        {
            if (await IsJpegProgressive(filePath))
            {
                Console.Out.WriteLine(filePath + " is Progressive Jpeg.");
                if (enableSlack)
                {
                    using (var client = new HttpClient())
                    {
                        var att = new
                        {
                            color = "#FF0000",
                            text = filePath + " is Progressive Jpeg.",
                        };
                        var data = DynamicJson.Serialize(new
                        {
                            text = "",
                            attachments = new object[] { att },
                        });
                        await client.PostAsync(slackIntgUrl, new StringContent(data));
                    }
                }
            }
            //Console.Out.WriteLine(filePath + " is BaseLine Jpeg.");
        }
    }
}
