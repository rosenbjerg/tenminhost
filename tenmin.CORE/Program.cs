using System;
using System.IO;
using System.Linq;
using RedHttpServerCore;
using RedHttpServerCore.Plugins;
using RedHttpServerCore.Plugins.Interfaces;
using RedHttpServerCore.Response;

namespace tenmin.CORE
{
    internal class Program
    {
        private static readonly Random Random = new Random();
        private static readonly DirectoryInfo Di = new DirectoryInfo("./uploads/");
        private const long MaxUseLimit = 0x3AAAAAAA;


        public static void Main(string[] args)
        {
            var server = new RedHttpServer(5001, "./public");
            const string uploadFolder = "./uploads/";
            Directory.CreateDirectory(uploadFolder);


            server.Get("/:file", async (req, res) =>
            {
                var filename = System.Net.WebUtility.UrlDecode(req.Params["file"]);
                if (File.Exists(uploadFolder + filename))
                {
                    await res.RenderPage("pages/index.ecs", new RenderParams
                    {
                        {"id", filename},
                        {"canUpload", false}
                    });
                }
                else
                {
                    await res.Redirect("/");
                }

            });

            server.Get("/:file/download", async (req, res) =>
            {
                var filename = uploadFolder + System.Net.WebUtility.UrlDecode(req.Params["file"]);
                if (File.Exists(filename))
                    await res.Download(filename);
                else
                    await res.Redirect("/");
            });

            server.Post("/upload", async (req, res) =>
            {
                var form = await req.GetFormDataAsync();
                Console.WriteLine(form);
            });
            
            var cleaner = new Cleaner(uploadFolder, 12);
            
            cleaner.Start();
            server.Start();
            Console.WriteLine("Started: " + DateTime.Now.ToString("R"));
            Console.ReadKey();
        }

        private static bool FreeSpace()
        {
            return GetDirectorySize(Di) < MaxUseLimit;
        }

        private static long GetDirectorySize(DirectoryInfo directory)
        {
            return directory.GetDirectories().Sum(dir => GetDirectorySize(dir)) + directory.GetFiles().Sum(file => file.Length);
        }

        private static string GetRandomString(int length)
        {
            var chars = new char[length];
            for (var i = 0; i < length; i++)
            {
                var num = Random.Next(0, 26);
                chars[i] = (char)('a' + num);
            }
            return new string(chars);
        }
    }
}