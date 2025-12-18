namespace CreepyUtil.Archipelago.PackReader.Bridge;

public class ImageReference(Pack pack)
{
    public Pack Pack = pack;

    public ImageRef FromPackRelativePath(string fileName)
        => new() { ImagePath = Path.Combine(Pack.BasePath, fileName) };

    public ImageRef FromImageReference(ImageRef original, string mods)
        => new() { ImagePath = original.ImagePath, Mods = mods };
}

public class ImageRef
{
    public string ImagePath;
    public string Mods;
}