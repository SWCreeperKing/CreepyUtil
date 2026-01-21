namespace CreepyUtil.Archipelago;

public class CsvParser
{
    private string[][] Csv;

    public CsvParser(string file, int topLineSkip, int leftLineSkip)
    {
        Csv = File.ReadAllText(file)
                  .Replace("\r", "")
                  .Split('\n')
                  .Skip(topLineSkip)
                  .Select(line =>
                       line.Split(',').Skip(leftLineSkip).ToArray())
                  .ToArray();
    }

    public T[] ReadTable<T>(CsvTableRowCreator<T> creator, int start, int length, Action<Exception, int>? onError = null)
        => Csv
          .Select((line, i) =>
           {
               try
               {
                   return creator.CreateRowData(line.Skip(start).Take(length).ToArray());
               }
               catch (Exception e)
               {
                   onError?.Invoke(e, i);
                   return default;
               }
           })
          .Where(line => line is not null && creator.IsValidData(line)).ToArray()!;

    public CsvFactory ToFactory(Action<Exception, int>? onError = null) => new(this, onError);
}

public class CsvFactory(CsvParser parser, Action<Exception, int>? onError = null)
{
    private readonly CsvParser Parser = parser;
    private readonly Action<Exception, int>? OnError = onError;
    private int Index;

    public CsvFactory SkipColumn(int count = 1)
    {
        Index += count;
        return this;
    }

    public CsvFactory ReadTable<T>(CsvTableRowCreator<T> creator, int length, out T[] table)
    {
        table = Parser.ReadTable(creator, Index, length, onError);
        Index += length;
        return this;
    }
}

public abstract class CsvTableRowCreator<T>
{
    public abstract T CreateRowData(string[] param);

    public virtual bool IsValidData(T t) => true;
}