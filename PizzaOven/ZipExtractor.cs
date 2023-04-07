using Onova.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SharpCompress.Common;
using SharpCompress.Readers;
using System.Windows;

namespace PizzaOven
{
    public class ZipExtractor : IPackageExtractor
    {
        public async Task ExtractPackageAsync(string sourceFilePath, string destDirPath,
            IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                using (Stream stream = File.OpenRead(sourceFilePath))
                    using (var reader = ReaderFactory.Open(stream))
                    {
                        while (reader.MoveToNextEntry())
                        {
                            if (!reader.Entry.IsDirectory)
                            {
                                reader.WriteEntryToDirectory(destDirPath, new ExtractionOptions()
                                {
                                    ExtractFullPath = true,
                                    Overwrite = true
                                });
                            }
                        }
                    }
            }
            catch
            {
                Global.logger.WriteLine("Failed to extract update", LoggerType.Error);
            }
            File.Delete(@$"{sourceFilePath}");
            // Move the folders to the right place
            string parentPath = Directory.GetParent(destDirPath).FullName;
            Directory.Move(Directory.GetDirectories(destDirPath)[0], $@"{parentPath}{Global.s}PizzaOven");
            Directory.Delete(destDirPath);
            Directory.Move($@"{parentPath}{Global.s}PizzaOven", destDirPath);
        }

    }
}
