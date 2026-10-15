using System;
using System.IO;
using System.Security.Cryptography;

namespace WalkingSimulator.ProjectAssets
{
    [Serializable]
    public sealed class AssetPart
    {
        public string path;
        public long bytes;
        public string sha256;
    }

    [Serializable]
    public sealed class StoredAsset
    {
        public string path;
        public long bytes;
        public string sha256;
        public AssetPart[] parts;
        public string metadataBase64;
    }

    [Serializable]
    public sealed class AssetManifest
    {
        public StoredAsset[] assets;
    }

    public static class ProjectAssetRestore
    {
        public static int Restore(string root, StoredAsset[] assets, Action<string, float> progress)
        {
            if (assets == null) throw new InvalidDataException("The project asset manifest is empty.");
            root = Path.GetFullPath(root);
            int restored = 0;
            for (int i = 0; i < assets.Length; i++)
            {
                StoredAsset asset = assets[i];
                string target = Resolve(root, asset.path, "Assets/");
                if (File.Exists(target)) continue;
                if (asset.parts == null || asset.parts.Length == 0)
                    throw new InvalidDataException("No parts are listed for " + asset.path);
                if (progress != null) progress(asset.path, (float)i / assets.Length);
                string cache = Path.Combine(root, "Library", "ProjectAssetRestore");
                Directory.CreateDirectory(cache);
                string temporary = Path.Combine(cache, Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    using (FileStream output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
                    {
                        byte[] buffer = new byte[1024 * 1024];
                        foreach (AssetPart part in asset.parts)
                        {
                            string source = Resolve(root, part.path, ".project-storage/");
                            using (FileStream input = File.OpenRead(source))
                            using (SHA256 hash = SHA256.Create())
                            {
                                if (input.Length != part.bytes)
                                    throw new InvalidDataException("Part length mismatch: " + part.path);
                                int read;
                                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    hash.TransformBlock(buffer, 0, read, buffer, 0);
                                    output.Write(buffer, 0, read);
                                }
                                hash.TransformFinalBlock(new byte[0], 0, 0);
                                if (!Matches(hash.Hash, part.sha256))
                                    throw new InvalidDataException("Part checksum mismatch: " + part.path);
                            }
                        }
                        if (output.Length != asset.bytes)
                            throw new InvalidDataException("Asset length mismatch: " + asset.path);
                    }
                    using (FileStream input = File.OpenRead(temporary))
                    using (SHA256 hash = SHA256.Create())
                    {
                        if (!Matches(hash.ComputeHash(input), asset.sha256))
                            throw new InvalidDataException("Asset checksum mismatch: " + asset.path);
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    string metadata = target + ".meta";
                    if (!File.Exists(metadata) && !string.IsNullOrEmpty(asset.metadataBase64))
                    {
                        using (FileStream output = new FileStream(metadata, FileMode.CreateNew, FileAccess.Write))
                        {
                            byte[] contents = Convert.FromBase64String(asset.metadataBase64);
                            output.Write(contents, 0, contents.Length);
                        }
                    }
                    File.Move(temporary, target);
                    restored++;
                }
                finally
                {
                    if (File.Exists(temporary)) File.Delete(temporary);
                }
            }
            return restored;
        }

        private static bool Matches(byte[] actual, string expected)
        {
            return string.Equals(BitConverter.ToString(actual).Replace("-", ""), expected,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string Resolve(string root, string relative, string prefix)
        {
            if (string.IsNullOrEmpty(relative) || !relative.StartsWith(prefix, StringComparison.Ordinal)
                || relative.IndexOf('\\') >= 0 || relative.IndexOf(':') >= 0)
                throw new InvalidDataException("Invalid asset path: " + relative);
            foreach (string segment in relative.Split('/'))
                if (segment == ".." || segment == "." || segment.Length == 0)
                    throw new InvalidDataException("Invalid asset path: " + relative);
            string full = Path.GetFullPath(Path.Combine(root, relative));
            string allowed = Path.GetFullPath(Path.Combine(root, prefix))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            StringComparison comparison = Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!full.StartsWith(allowed, comparison))
                throw new InvalidDataException("Asset path is outside the project: " + relative);
            return full;
        }
    }
}
