using System.IO.Compression;
using CreepyUtil.Archipelago.PackReader.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CreepyUtil.Archipelago.PackReader;

public static class Porter
{
    // Either string or T : Exception
    public static event Action<object>? LoggerEvent;

    public static bool LoadPack(string path, out Pack? pack)
    {
        pack = null;
        Func<string, string> readFile;
        try
        {
            using var _ = ZipFile.OpenRead(path);
            readFile = file =>
            {
                using var zip = ZipFile.OpenRead(path);
                var entry = zip.GetEntry(file);
                return entry is null
                    ? throw new FileNotFoundException($"File not found: [{path}/{file}]")
                    : entry.Read();
            };
        }
        catch
        {
            if (File.Exists(path) || !Directory.Exists(path)) return false;
            readFile = file => !File.Exists($"{path}/{file}")
                ? throw new FileNotFoundException($"File not found: [{path}/{file}]")
                : File.ReadAllText($"{path}/{file}");
        }

        pack = new Pack(readFile)
        {
            Manifest = JsonConvert.DeserializeObject<Manifest>(readFile("manifest.json"))
        };

        return true;
    }

    public static string Read(this ZipArchiveEntry entry)
    {
        using StreamReader reader = new(entry.Open());
        var text = reader.ReadToEnd();
        reader.Close();
        return text;
    }

    internal static void LogInfo(object obj) => LoggerEvent?.Invoke(obj);
    
    internal static T[] DynamicToArr<T>(dynamic dyn)
        => dyn switch
        {
            T => [dyn],
            JArray jArray => jArray.ToObject<T[]>()!,
            _ => dyn
        };
}