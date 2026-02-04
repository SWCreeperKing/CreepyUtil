namespace CreepyUtil.Archipelago;

public class CsvParser
{
    private string[][] Csv;

    public CsvParser(string file, int topLineSkip, int leftLineSkip)
    {
        var rawSplit = File.ReadAllLines(file)
                           .Skip(topLineSkip)
                           .Select(line => line.Split(',').Skip(leftLineSkip));

        List<List<string>> cells = [];

        foreach (var row in rawSplit)
        {
            List<string> current = [];
            var isOpen = false;
            foreach (var col in row)
            {
                if (col is "")
                {
                    current.Add("");
                    continue;
                }
                if (isOpen)
                {
                    var leng = col.Length - 1;

                    if ((col[leng] is '"' || col.Length == 1) && col[leng - 1] is not '\\')
                    {
                        isOpen = false;
                    }
                    
                    if (col is "" or "\"") continue;
                    current[current.Count - 1] = current.Last() + ',' + (isOpen ? col : col.Substring(0, col.Length - 1));
                    continue;
                }

                isOpen = col[0] is '"';
                current.Add(isOpen ? col.Substring(1) : col);
            }
            
            cells.Add(current);
        }

        Csv = cells.Select(arr => arr.ToArray()).ToArray();
    }

    public T[] ReadTable<T>(CsvTableRowCreator<T> creator, int start, int length, Action<Exception, int>? onError = null)
        => Csv
          .Select(line => line.Skip(start).Take(length).ToArray())
          .Where(line => line.Any())
          .Where(creator.IsTableNotEmpty)
          .Select((line, i) =>
           {
               try
               {
                   return creator.CreateRowData(line);
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
    public virtual bool IsTableNotEmpty(string[] param) => !param.All(s => s.Trim() is "");
    public virtual bool IsValidData(T t) => true;
}