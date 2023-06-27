using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace WebDriverPilot.Helpers
{
    public enum DriverType
    {
        Chrome = 0,
        Edge = 1
    }

    public static class FolderHelpers
    {
        public static string GetProgramPath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
    }

    public static class FileHelpers
    {
        public static FileVersionInfo GetFileVersionInfo(this string filePath)
        {
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
            return fileVersionInfo;
        }

        // rename
        public static FileInfo Rename(this FileInfo file, string newName, bool overwrite = false)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentNullException(nameof(newName));
            }

            if (!file.Exists)
            {
                throw new FileNotFoundException("File not found", file.FullName);
            }

            if (file.Directory != null && !file.Directory.Exists)
            {
                throw new DirectoryNotFoundException("Directory not found");
            }

            if (file.Directory != null)
            {
                var newFile = new FileInfo(Path.Combine(file.Directory.FullName, newName));
                if (newFile.Exists && !overwrite)
                {
                    throw new IOException("File already exists");
                }

                if (newFile.Exists && overwrite)
                {
                    newFile.Delete();
                }

                file.MoveTo(newFile.FullName);
                return newFile;
            }

            throw new DirectoryNotFoundException("Directory not found");
        }
    }

    public static class HashHelpers
    {
        // Readonly Dictionary to get the hash of the file based on provided crypto algorithm e.g. SHA256 => GetSha256Hash
        private static readonly Dictionary<string, Func<FileInfo, string>> HashAlgorithmsZip =
            new Dictionary<string, Func<FileInfo, string>>
            {
                { "MD5", GetZipFileMd5Hash }
            };
        private static readonly Dictionary<string, Func<FileInfo, string>> HashAlgorithms =
            new Dictionary<string, Func<FileInfo, string>>
            {
                { "MD5", GetFileMd5Hash }
            };
        public static string GetZipFileHash(this FileInfo input, string algorithm)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            // if the algorithm is not in the dictionary throw an exception
            if (!HashAlgorithmsZip.ContainsKey(algorithm))
            {
                throw new ArgumentException($"Algorithm {algorithm} is not supported");
            }

            return HashAlgorithmsZip[algorithm](input);
        }

        public static string GetFileHash(this FileInfo input, string algorithm)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            // if the algorithm is not in the dictionary throw an exception
            if (!HashAlgorithms.ContainsKey(algorithm))
            {
                throw new ArgumentException($"Algorithm {algorithm} is not supported");
            }

            return HashAlgorithms[algorithm](input);
        }
        
        public static string GetZipFileMd5Hash(FileInfo zipFile)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var zipArchive = ZipFile.Open(zipFile.FullName, ZipArchiveMode.Read))
                {
                    var fileHashes = new List<byte[]>();

                    foreach (var entry in zipArchive.Entries)
                    {
                        using (var entryStream = entry.Open())
                        {
                            var entryHash = md5.ComputeHash(entryStream);
                            fileHashes.Add(entryHash);
                        }
                    }

                    var combinedHash = md5.ComputeHash(fileHashes.SelectMany(x => x).ToArray());
                    return Convert.ToBase64String(combinedHash);
                }
            }
        }

        public static string GetZipFileSha256Hash(FileInfo zipFile)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                using (var zipArchive = ZipFile.Open(zipFile.FullName, ZipArchiveMode.Read))
                {
                    var fileHashes = new List<byte[]>();

                    foreach (var entry in zipArchive.Entries)
                    {
                        using (var entryStream = entry.Open())
                        {
                            var entryHash = sha256.ComputeHash(entryStream);
                            fileHashes.Add(entryHash);
                        }
                    }

                    var combinedHash = sha256.ComputeHash(fileHashes.SelectMany(x => x).ToArray());
                    return Convert.ToBase64String(combinedHash);
                }
            }
        }

        public static string GetFileMd5Hash(this FileInfo input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = File.OpenRead(input.FullName))
                {
                    var hash = md5.ComputeHash(stream);
                    return Convert.ToBase64String(hash);
                }
            }
        }

        public static string GetFileSha256Hash(this FileInfo input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                using (var stream = File.OpenRead(input.FullName))
                {
                    var hash = sha256.ComputeHash(stream);
                    return Convert.ToBase64String(hash);
                }
            }
        }
    }
}