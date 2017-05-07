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
            var server = new RedHttpServer(5001);
            var logger = new FileLogging("./LOG");
            server.Plugins.Register<ILogging, FileLogging>(logger);
            const string uploadFolder = "./uploads/";
            Directory.CreateDirectory(uploadFolder);

            server.Get("/", async (req, res) =>
            {
                await res.RenderPage("pages/index.ecs", new RenderParams
                {
                    {"id", ""},
                    {"canUpload", FreeSpace()}
                });
            });

            //server.Get("/favicon.ico", (req, res) =>
            //{
            //    res.SendFile("pages/favicon.ico");
            //});

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
                if (FreeSpace())
                {
                    var fname = "";
                    var sa = await req.SaveBodyToFile(uploadFolder, s =>
                    {
                        s = $"{GetRandomString(4)}-{s}";
                        fname = s;
                        return s;
                    }, 0x7D000);
                    if (!sa) await res.SendString("Error occurred while saving", status: 413);
                    else
                    {
                        await res.SendString(fname);
                        logger.Log("UPL", fname.Substring(5));
                    }
                }
                else
                    await res.SendString("Error, server is temporarily full", status: 400);
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
                var num = Random.Next(0, 26); // Zero to 25
                chars[i] = (char)('a' + num);
            }
            return new string(chars);
        }
    }
}