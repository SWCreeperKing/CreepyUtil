using System.Text;

namespace CreepyUtil.Archipelago.WorldFactory;

public class PythonFactory
{
    private List<string> Imports = [];
    private List<IPythonObject> Objects = [];

    public PythonFactory AddImport(string import)
    {
        Imports.AddRange(import.ShredString());
        return this;
    }

    public PythonFactory AddImports(params string[] imports)
    {
        foreach (var import in imports)
        {
            AddImport(import);
        }
        return this;
    }

    public PythonFactory AddObject(IPythonObject obj)
    {
        Objects.Add(obj);
        return this;
    }

    public PythonFactory AddObjects(params IPythonObject[] objects)
    {
        Objects.AddRange(objects);
        return this;
    }

    public string GetText()
    {
        StringBuilder sb = new();
        if (Imports.Any())
        {
            foreach (var import in Imports)
            {
                sb.Append(import).Append('\n');
            }
            sb.Append('\n');
        }

        sb.Append(string.Join("\n\n", Objects.Select(obj => obj.GetText())));

        return sb.ToString();
    }
}

public class PythonClassFactory(string className) : IPythonObject
{
    private string Name = className;
    private List<string> Parameters = [];
    private List<string> Comment = [];
    private List<string> Decorators = [];

    private List<IPythonVariable> Variables = [];
    private List<MethodFactory> Methods = [];

    public PythonClassFactory AddDecorator(string decorator)
    {
        Decorators.Add(decorator);
        return this;
    }

    public PythonClassFactory AddDecorators(params string[] decorator)
    {
        Decorators.AddRange(decorator);
        return this;
    }

    public PythonClassFactory AddParameter(params string[] parameter)
    {
        Parameters.AddRange(parameter);
        return this;
    }

    public PythonClassFactory AddComment(string comment)
    {
        Comment.AddRange(comment.ShredString());
        return this;
    }

    public PythonClassFactory AddComment(params string[] comment)
    {
        foreach (var commentLine in comment)
        {
            AddComment(commentLine);
        }
        return this;
    }

    public PythonClassFactory AddVariable(IPythonVariable variable)
    {
        Variables.Add(variable);
        return this;
    }

    public PythonClassFactory AddVariables(params IPythonVariable[] variables)
    {
        Variables.AddRange(variables);
        return this;
    }

    public PythonClassFactory AddMethod(MethodFactory method)
    {
        Methods.Add(method);
        return this;
    }

    public PythonClassFactory AddMethods(params MethodFactory[] methods)
    {
        Methods.AddRange(methods);
        return this;
    }

    public string GetText()
    {
        StringBuilder sb = new();

        if (Decorators.Any())
        {
            Decorators.ForEach(decorator => sb.Append(decorator));
            sb.Append('\n');
        }

        sb.Append("class ").Append(Name).Append('(').Append(string.Join(", ", Parameters)).Append("):\n");

        if (Comment.Count != 0)
        {
            sb.Append("\t\"\"\"");
            foreach (var line in Comment)
            {
                sb.Append("\n\t").Append(line);
            }

            sb.Append("\n\t\"\"\"\n");
        }

        if (Variables.Count != 0)
        {
            foreach (var variable in Variables)
            {
                sb.Append(variable.GetVariable(1)).Append('\n');
            }
            if (Methods.Any())
            {
                sb.Append('\n');
            }
        }

        sb.Append(string.Join("\n\n", Methods.Select(m => m.GetMethod(1))));

        return sb.ToString();
    }
}

public class MethodFactory(string methodName) : IPythonObject
{
    private string MethodName = methodName;
    private List<string> Parameters = [];
    private List<string> CodeLines = [];
    private string Return = ""; // not needed, just there because

    public MethodFactory AddParam(string param)
    {
        Parameters.Add(param);
        return this;
    }

    public MethodFactory AddParams(params string[] param)
    {
        Parameters.AddRange(param);
        return this;
    }

    public MethodFactory AddCode(string code)
    {
        CodeLines.AddRange(code.ShredString());
        return this;
    }

    public MethodFactory AddCode(params string[] code)
    {
        foreach (var line in code)
        {
            AddCode(line);
        }

        return this;
    }

    public MethodFactory SetReturn(string retrn)
    {
        Return = retrn;
        return this;
    }

    public string GetMethod(int indentLevel = 0)
    {
        StringBuilder sb = new();
        var indent = '\t'.Repeat(indentLevel);

        sb.Append(indent).Append("def ").Append(MethodName).Append('(').Append(string.Join(", ", Parameters)).Append(')');

        if (Return is not "")
        {
            sb.Append(" -> ").Append(Return);
        }

        indent += '\t';
        sb.Append(":\n").Append(indent);

        if (CodeLines.Count == 0)
        {
            sb.Append("pass");
        }
        else
        {
            sb.Append(string.Join($"\n{indent}", CodeLines));
        }

        return sb.ToString();
    }

    public string GetText() => GetMethod();
}

public class Comment(string text) : IPythonObject
{
    public string GetText() => $"# {text}";
}

public class Variable(string name, string value = "", string type = "") : PythonVariable<string>(name, value, type)
{
    public override bool HasValue(string value) => value is not "";
    public override void AddValue(string value, int indentLevel, StringBuilder sb) => sb.Append(value);
}

public class StringMap(string name, Dictionary<string, string>? value = null, string type = "") : MappedVariable<string, string>(name, value, type)
{
    public override string ParseKey(string key) => key.Surround('"');
    public override string ParseValue(string value) => value.Surround('"');
}

public class StringArrayMap(string name, Dictionary<string, string[]>? value = null, string type = "") : MappedVariable<string, string[]>(name, value, type)
{
    public override string ParseKey(string key) => key.Surround('"');
    public override string ParseValue(string[] value) => $"[{string.Join(", ", value.Select(s => s.Surround('"')))}]";
}

public class StringArray(string name, IEnumerable<string>? value = null, string type = "", bool stringify = true) : ListedVariable<string>(name, value, type)
{
    public override string ParseValue(string value) => stringify ? value.Surround('"') : value;
}

public class StringDoubleArray(string name, IEnumerable<IEnumerable<string>>? value = null, string type = "") : ListedVariable<IEnumerable<string>>(name, value, type)
{
    public override string ParseValue(IEnumerable<string> value) => $"[{string.Join(", ", value.Select(s => s.Surround('"')))}]";
}

public abstract class MappedVariable<TKey, TValue>(string name, Dictionary<TKey, TValue>? value = null, string type = "") : ListedVariable<KeyValuePair<TKey, TValue>>(name, value, type) where TKey : notnull
{
    public override string ParseValue(KeyValuePair<TKey, TValue> value) => $"{ParseKey(value.Key)}: {ParseValue(value.Value)}";
    public abstract string ParseKey(TKey key);
    public abstract string ParseValue(TValue value);
}

public abstract class ListedVariable<T>(string name, IEnumerable<T>? value = null, string type = "") : PythonVariable<IEnumerable<T>?>(name, value, type)
{
    public override bool HasValue(IEnumerable<T>? value) => value is not null;

    public override void AddValue(IEnumerable<T>? value, int indentLevel, StringBuilder sb)
    {
        if (!value.Any())
        {
            sb.Append("[]");
            return;
        }

        var indent = '\t'.Repeat(indentLevel);
        var innerIndent = '\t'.Repeat(indentLevel + 1);

        sb.Append("[\n").Append(innerIndent)
          .Append(string.Join($",\n{innerIndent}", value.Select(ParseValue)))
          .Append('\n').Append(indent).Append(']');
    }

    public abstract string ParseValue(T value);
}

public abstract class PythonVariable<T>(string name, T value, string type = "") : IPythonVariable
{
    public string GetVariable(int indentLevel = 0)
    {

        StringBuilder sb = new();
        sb.Append('\t'.Repeat(indentLevel)).Append(name);

        if (type is not "")
        {
            sb.Append(": ").Append(type);
        }

        if (!HasValue(value)) return sb.ToString();
        sb.Append(" = ");
        AddValue(value, indentLevel, sb);

        return sb.ToString();
    }

    public abstract bool HasValue(T value);
    public abstract void AddValue(T value, int indentLevel, StringBuilder sb);

    public string GetText() => GetVariable();
}

public interface IPythonVariable : IPythonObject
{
    public string GetVariable(int indentLevel = 0);
}

public interface IPythonObject
{
    public string GetText();
}