using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json;

namespace CreepyUtil.Archipelago.Overrides;

class DataCache : IDataPackageCache
{
    readonly IArchipelagoSocketHelper socket;

    // ReSharper disable once ArrangeObjectCreationWhenTypeEvident
    public readonly Dictionary<string, IGameDataLookup> inMemoryCache = new Dictionary<string, IGameDataLookup>();

    internal IFileSystemDataPackageProvider FileSystemDataPackageProvider;

    public DataCache(IArchipelagoSocketHelper socket)
    {
        this.socket = socket;

        socket.PacketReceived += Socket_PacketReceived;
    }

    public DataCache(IArchipelagoSocketHelper socket, IFileSystemDataPackageProvider fileSystemProvider)
    {
        this.socket = socket;

        FileSystemDataPackageProvider = fileSystemProvider;

        socket.PacketReceived += Socket_PacketReceived;
    }

    void Socket_PacketReceived(ArchipelagoPacketBase packet)
    {
        switch (packet)
        {
            case RoomInfoPacket roomInfoPacket:
                if (FileSystemDataPackageProvider == null)
                    FileSystemDataPackageProvider = new FileSystemCheckSumDataPackageProvider();

                var invalidated = GetCacheInvalidatedGamesByChecksum(roomInfoPacket);
                if (invalidated.Any())
                {
                    socket.SendPacket(new GetDataPackagePacket
                    {
                        Games = invalidated.ToArray()
                    });
                }

                break;
            case DataPackagePacket packagePacket:
                UpdateDataPackageFromServer(packagePacket.DataPackage);
                break;
        }
    }

    public bool TryGetDataPackageFromCache(out Dictionary<string, IGameDataLookup> gameData)
    {
        gameData = inMemoryCache;

        return inMemoryCache.Any();
    }

    public bool TryGetGameDataFromCache(string game, out IGameDataLookup gameData)
    {
        if (inMemoryCache.TryGetValue(game, out var cachedGameData))
        {
            gameData = cachedGameData;
            return true;
        }

        gameData = null;
        return false;
    }

    internal void UpdateDataPackageFromServer(DataPackage package)
    {
        foreach (var packageGameData in package.Games)
        {
            inMemoryCache[packageGameData.Key] = new GameDataLookup(packageGameData.Value);

            FileSystemDataPackageProvider.SaveDataPackageToFile(packageGameData.Key, packageGameData.Value);
        }
    }

    List<string> GetCacheInvalidatedGamesByChecksum(RoomInfoPacket packet)
    {
        var gamesNeedingUpdating = new List<string>();

        foreach (var game in packet.Games)
        {
            if (packet.DataPackageChecksums != null
                && packet.DataPackageChecksums.TryGetValue(game, out var checksum)
                && FileSystemDataPackageProvider.TryGetDataPackage(game, checksum, out var cachedGameData)
                && cachedGameData.Checksum == checksum)
                inMemoryCache[game] = new GameDataLookup(cachedGameData);
            else
                gamesNeedingUpdating.Add(game);
        }

        return gamesNeedingUpdating;
    }
}

interface IFileSystemDataPackageProvider
{
    bool TryGetDataPackage(string game, string checksum, out GameData gameData);
    void SaveDataPackageToFile(string game, GameData gameData);
}

class FileSystemCheckSumDataPackageProvider : IFileSystemDataPackageProvider
{
    static readonly string CacheFolder;
			
    static FileSystemCheckSumDataPackageProvider()
    {
        CacheFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                Path.Combine("Archipelago", Path.Combine("Cache", "datapackage")));
    }
		
    public bool TryGetDataPackage(string game, string checksum, out GameData gameData)
    {
        var folderPath = Path.Combine(CacheFolder, GetFileSystemSafeFileName(game));
        var filePath = Path.Combine(folderPath, $"{checksum}.json");

        if (!File.Exists(filePath))
        {
            gameData = null;
            return false;
        }

        try
        {
            var fileText = File.ReadAllText(filePath);
            gameData = JsonConvert.DeserializeObject<GameData>(fileText);
            return true;
        }
        catch
        {
            gameData = null;
            return false;
        }
    }

    public void SaveDataPackageToFile(string game, GameData gameData)
    {
        var folderPath = Path.Combine(CacheFolder, GetFileSystemSafeFileName(game));
        var filePath = Path.Combine(folderPath, $"{GetFileSystemSafeFileName(gameData.Checksum)}.json");

        try
        {
            Directory.CreateDirectory(folderPath);

            var contents = JsonConvert.SerializeObject(gameData);
            File.WriteAllText(filePath, contents);
        }
        catch
        {
            // ignored
        }
    }

    string GetFileSystemSafeFileName(string gameName)
    {
        var safeName = gameName;

        foreach (var c in Path.GetInvalidFileNameChars())
            gameName = gameName.Replace(c.ToString(), string.Empty);

        return safeName;
    }
}