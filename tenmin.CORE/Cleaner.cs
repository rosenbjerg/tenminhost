using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RedHttpServerCore.Plugins.Interfaces;

namespace tenmin.CORE
{
    class Cleaner
    {
        private readonly Thread _thread;
        private readonly string _path;
        private readonly TimeSpan _interval;
        private readonly int _maxAgeMin;

        public Cleaner(string path, int maxAgeMinutes)
        {
            _thread = new Thread(Clean);
            _path = path;
            _maxAgeMin = maxAgeMinutes;
            _interval = TimeSpan.FromMinutes(maxAgeMinutes / 3);
        }

        public void Start()
        {
            _thread.Start();
        }

        private async void Clean()
        {
            while (true)
            {
                var files = Directory.EnumerateFiles(_path);
                var now = DateTime.UtcNow;
                foreach (var file in files)
                {
                    var cdate = File.GetCreationTimeUtc(file);
                    if (now.Subtract(cdate).Minutes < _maxAgeMin) continue;
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }
                await Task.Delay(_interval);

            }
        }
    }
}