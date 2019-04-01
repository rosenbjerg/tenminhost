using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Fluent;
using Red;
using Red.HandlebarsRenderer;
using Validation;

namespace TenMinHost
{
    class Program
    {
        const string UploadDirectory = "./uploads";
        const int DisplayKiBLimit = 10_000_000; // 1 MiB
        const int MaxFileSize = 1_000_000_000; // 1 GiB
        const long MaxSpaceConsumption = 10_000_000_000; // 10 GiB

        private static readonly char[] PossibleRandomCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();


        static async Task Main(string[] args)
        {
            var server = new RedHttpServer(5123, "public")
            {
                ConfigureApplication = app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                           ForwardedHeaders.XForwardedProto
                    });
                },
                ConfigureServices = serviceCollection =>
                {
                    serviceCollection.Configure<FormOptions>(formOptions =>
                    {
                        formOptions.ValueCountLimit = 1;
                        formOptions.ValueLengthLimit = MaxFileSize;
                        formOptions.MultipartBodyLengthLimit = MaxFileSize;
                    });
                }
            };

            
            var uploadLogger = LogManager.GetLogger("Upload");
            var maxSizeMiB = GetSizeString(MaxFileSize);

            Directory.CreateDirectory(UploadDirectory);
            var validForm = ValidatorBuilder.New().RequiresFile("file", file => file.Length < MaxFileSize).Build();

            server.Get("/:filename", async (req, res) =>
            {
                var filename = HttpUtility.UrlDecode(req.Parameters["filename"]);
                var filepath = Path.Combine(UploadDirectory, Path.GetFileName(filename));

                if (File.Exists(filepath))
                {
                    var fileInfo = new FileInfo(filepath);
                    await res.RenderTemplate("./templates/download.hbs", new FileInfoWrapper(fileInfo));
                }
                else
                {
                    await res.Redirect("/");
                }
            });

            server.Post("/upload", async (req, res) =>
            {
                req.UnderlyingContext.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = MaxFileSize;
                IFormCollection form;
                try
                {
                    form = await req.GetFormDataAsync();
                }
                catch (Exception)
                {
                    await res.SendString(
                        $"Your file exceeds the maximum size ({maxSizeMiB})",
                        status: HttpStatusCode.InsufficientStorage);
                    return;
                }
                
                if (validForm.Validate(form))
                {
                    var file = form.Files["file"];
                    if (!CheckSpaceConsumption(file.Length))
                    {
                        await res.SendString(
                            "Not enough space for your file right now, try again in a couple of minutes",
                            status: HttpStatusCode.InsufficientStorage);
                        uploadLogger.Log(LogLevel.Warn, "Maximum space consumption reached");
                        return;
                    }


                    var randomChars = GenerateRandom(6);
                    var filename = $"{randomChars}-{Path.GetFileName(file.FileName)}";
                    var filepath = Path.Combine(UploadDirectory, filename);
                    try
                    {
                        using (var outputStream = File.OpenWrite(filepath))
                        {
                            await file.CopyToAsync(outputStream);
                        }
                        await res.SendString(filename);
                        uploadLogger.Log(LogLevel.Info,
                            $"{req.UnderlyingContext.Connection.RemoteIpAddress.ToString()}: {filename}");
                    }
                    catch (Exception e)
                    {
                        if (File.Exists(filepath))
                        {
                            File.Delete(filepath);
                            Console.WriteLine("File deleted after failed upload: " + filename);
                        }
                        uploadLogger.Log(LogLevel.Error, e);
                        
                        await res.SendString("File upload failed", status: HttpStatusCode.InternalServerError);
                    }
                }
                else
                {
                    await res.SendString($"The maximum allowed filesize is {maxSizeMiB}",
                        status: HttpStatusCode.BadRequest);
                }
            });

            server.Get("/download/:filename", async (req, res) =>
            {
                var filename = HttpUtility.UrlDecode(req.Parameters["filename"]);
                var filepath = Path.Combine(UploadDirectory, Path.GetFileName(filename));

                if (File.Exists(filepath))
                {
                    await res.Download(filepath, filename.Substring(7));
                }
                else
                {
                    await res.SendString("The file was not found. Perhaps it has been deleted",
                        status: HttpStatusCode.NotFound);
                }
            });
            
            SetupLogging();
            StartFileReaper();
            await server.RunAsync();
        }


        public static string GetSizeString(long size)
        {
            if (size > DisplayKiBLimit)
            {
                return $"{size / 1024f / 1024f:F2} MiB";
            }

            return $"{size / 1024f:F1} KiB";
        }

        public static string FormatTime(DateTime uploadedUtc)
        {
            var uploaded = (int) DateTime.UtcNow.Subtract(uploadedUtc).TotalSeconds;
            if (uploaded == 0)
                return "Expires in 10 minutes";
            if (uploaded > 540)
                return "Expires in less than a minute!";
            return $"Expires in {(10 - (uploaded / 60))} minutes";
        }

        private static void SetupLogging()
        {
            var config = new NLog.Config.LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "LOG.txt" };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
            
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
            
            LogManager.Configuration = config;
        }

        private static bool CheckSpaceConsumption(long uploadedFileSize)
        {
            var files = Directory.GetFiles(UploadDirectory);
            long consumedSpace = files.Sum(file => new FileInfo(file).Length);
            return (consumedSpace + uploadedFileSize) < MaxSpaceConsumption;
        }

        private static void StartFileReaper()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    ReapFiles();
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
            });
        }

        private static void ReapFiles()
        {
            var files = Directory.EnumerateFiles(UploadDirectory);
            var threshold = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));

            Parallel.ForEach(files, file =>
            {
                var info = new FileInfo(file);
                if (info.CreationTimeUtc < threshold)
                {
                    File.Delete(file);
                }
            });
        }

        private static string GenerateRandom(int length)
        {
            var sb = new StringBuilder();
            var random = new Random();
            var possibleChars = PossibleRandomCharacters.Length - 1;
            for (int i = 0; i < length; i++)
            {
                sb.Append(PossibleRandomCharacters[random.Next(0, possibleChars)]);
            }

            return sb.ToString();
        }
    }
}