using System;
using System.IO;
using System.Linq;
using RedHttpServer;
using RedHttpServer.Rendering;
using tenminhost.RHttp;

namespace tenmin.CORE
{
    internal class Program
    {
        private static readonly Random Random = new Random();
        private static readonly DirectoryInfo Di = new DirectoryInfo("./uploads/");
        private const long MaxUseLimit = 0x3AAAAAAA;


        public static void Main(string[] args)
        {
            var server = new RedHttpServer.RedHttpServer(5001);
            const string uploadFolder = "./uploads/";
            Directory.CreateDirectory(uploadFolder);

            server.Get("/", (req, res) =>
            {
                res.RenderPage("pages/index.ecs", new RenderParams
                {
                    {"id", ""},
                    {"canUpload", FreeSpace()}
                });
            });

            //server.Get("/favicon.ico", (req, res) =>
            //{
            //    res.SendFile("pages/favicon.ico");
            //});

            server.Get("/:file", (req, res) =>
            {
                var filename = System.Net.WebUtility.UrlDecode(req.Params["file"]);
                if (File.Exists(uploadFolder + filename))
                {
                    res.RenderPage("pages/index.ecs", new RenderParams
                    {
                        {"id", filename},
                        {"canUpload", false}
                    });
                }
                else
                {
                    res.Redirect("/");
                }

            });

            server.Get("/:file/download", (req, res) =>
            {
                var filename = uploadFolder + System.Net.WebUtility.UrlDecode(req.Params["file"]);
                if (File.Exists(filename))
                    res.Download(filename, filename.Substring(5));
                else
                    res.Redirect("/");
            });

            server.Post("/upload", async (req, res) =>
            {
                if (!FreeSpace())
                {
                    res.SendString("Error, server is temporarily full", status: 400);
                    return;
                }
                var fname = "";
                var sa = await req.SaveBodyToFile("./uploads", s =>
                {
                    s = $"{GetRandomString(4)}-{s}";
                    fname = s;
                    return s;
                }, 0x7D000);
                if (!sa) res.SendString("Error occurred while saving", status: 413);
                else
                {
                    res.SendString(fname);
                    File.AppendAllText("uploadedfiles.txt", DateTime.Now.ToString("g") + "\t" + fname.Substring(5) + "\n");
                }
            });
            
            var cleaner = new Cleaner("./uploads", 13);
            
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
                var num = Random.Next(0, 26); // Zero to 25
                chars[i] = (char)('a' + num);
            }
            return new string(chars);
        }
    }
}