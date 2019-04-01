using System;
using System.IO;

namespace TenMinHost
{
    public class FileInfoWrapper
    {
        public string FullName { get; }
        public string Name { get; }
        public string Size { get; }
        public int UploadedSecondsAgo { get; }
        public string Uploaded { get; }

        public FileInfoWrapper(FileInfo fileInfo)
        {
            FullName = fileInfo.Name;
            Name = fileInfo.Name.Substring(7);
            Size = Program.GetSizeString(fileInfo.Length);
            Uploaded = Program.FormatTime(fileInfo.CreationTimeUtc);
            UploadedSecondsAgo = (int) DateTime.UtcNow.Subtract(fileInfo.CreationTimeUtc).TotalSeconds;
        }
    }
}