using System.Text;

namespace CreepyUtil.Archipelago;

// this is to aid in creation of apworlds
public class WorldFactory
{
    private static readonly string[] DefaultParams = ["state", "player"];

    public string LocationGeneratorLink = "No Link Given"; // link to (preferably github) of where you use the location generator, inserts it into the .py
    public string LogicGeneratorLink = "No Link Given"; // see above

    private Dictionary<string, IEnumerable<string>> LocationVariablesSingle = [];
    private Dictionary<string, IEnumerable<IEnumerable<string>>> LocationVariablesDouble = [];
    private HashSet<string> LocationVariablesFinalList = [];
    private Dictionary<string, string> LogicMethods = [];
    private Dictionary<string, string> LogicMethodTokens = [];
    private HashSet<string> LogicRules = [];
    private Action<Exception, string>? OnCompilerError;

    public ILogicCompiler LogicCompiler = new DefaultLogicCompiler();

    /// <param name="selectionModifier">selects from the array what to put in the final location dict, "*[selectionModifier for items in variable]"</param>
    public WorldFactory AddLocations(string variable, IEnumerable<string> locations, bool stringify = true, string selectionModifier = "items", bool addToFinalList = true)
    {
        LocationVariablesSingle[variable] = stringify ? locations.Select(s => $"\"{s}\"") : locations;

        if (addToFinalList)
        {
            LocationVariablesFinalList.Add($"*[{selectionModifier} for items in {variable}]");
        }

        return this;
    }

    /// <param name="selectionModifier">selects from the array what to put in the final location dict, "*[selectionModifier for items in variable]"</param>
    public WorldFactory AddLocations(string variable, IEnumerable<IEnumerable<string>> locations, bool stringify = true, string selectionModifier = "items[0]", bool addToFinalList = true)
    {
        LocationVariablesDouble[variable] = stringify ? locations.Select(arr => arr.Select(s => $"\"{s}\"")) : locations;

        if (addToFinalList)
        {
            LocationVariablesFinalList.Add($"*[{selectionModifier} for items in {variable}]");
        }

        return this;
    }

    public void GenerateLocationFile(string fileOutput)
    {
        StringBuilder sb = new();
        sb.Append($"# File is Auto-generated, see: [{LocationGeneratorLink}]\n\n");

        foreach (var singles in LocationVariablesSingle)
        {
            var varName = singles.Key;
            var vals = singles.Value;

            sb.Append(varName).Append(" = [");

            foreach (var val in vals)
            {
                sb.Append("\n\t").Append(val).Append(',');
            }
            sb.Append("\n]\n\n");
        }

        foreach (var doubles in LocationVariablesDouble)
        {
            var varName = doubles.Key;
            var valsArr = doubles.Value;

            sb.Append(varName).Append(" = [");

            foreach (var vals in valsArr)
            {
                sb.Append("\n\t[").Append(string.Join(", ", vals)).Append("],");
            }
            sb.Append("\n]\n\n");
        }

        sb.Append("location_dict = [");
        foreach (var item in LocationVariablesFinalList)
        {
            sb.Append("\n\t").Append(item).Append(',');
        }

        sb.Append("\n]");

        File.WriteAllText(fileOutput, sb.ToString());
    }

    public WorldFactory AddLogicFunction(string token, string methodName, IEnumerable<string> code, params string[] parameters) => AddLogicFunction(token, methodName, string.Join("\n\t", code), parameters);

    public WorldFactory AddLogicFunction(string token, string methodName, string code, params string[] parameters)
    {
        LogicMethodTokens[token] = methodName;
        LogicMethods[methodName] = $"def {methodName}({string.Join(", ", DefaultParams.Concat(parameters))}) -> bool:\n\t{code}";
        return this;
    }

    public WorldFactory AddCompoundLogicFunction(string token, string methodName, string rule, params string[] parameters)
    {
        AddLogicFunction(token, methodName, $"return {LogicCompiler.CompileRule(rule, LogicMethodTokens, OnCompilerError)}", parameters);
        return this;
    }

    public WorldFactory AddLogicRule(string key, string rule)
    {
        if (rule.Trim() is "") return this;
        LogicRules.Add($"\"{key}\": lambda state: {LogicCompiler.CompileRule(rule.Trim(), LogicMethodTokens, OnCompilerError)}");
        return this;
    }

    public WorldFactory AddLogicRules(Dictionary<string, string> rules) => rules.Aggregate(this, (factory, pair) => factory.AddLogicRule(pair.Key, pair.Value));

    public void GenerateRulesFile(string fileOutput, params string[] extraParams)
    {
        StringBuilder sb = new();
        sb.Append($"# File is Auto-generated, see: [{LogicGeneratorLink}]\n\n");

        sb.Append("def get_rule_map(").Append(string.Join(", ", DefaultParams.Concat(extraParams))).Append("):").Append("\n\treturn {");
        foreach (var rule in LogicRules)
        {
            sb.Append("\n\t\t").Append(rule).Append(',');
        }

        sb.Append("\n\t}");

        foreach (var method in LogicMethods.Values)
        {
            sb.Append("\n\n").Append(method);
        }

        File.WriteAllText(fileOutput, sb.ToString());
    }

    public WorldFactory SetOnCompilerError(Action<Exception, string>? onError)
    {
        OnCompilerError = onError;
        return this;
    }
}

public interface ILogicCompiler
{
    public string CompileRule(string rule, Dictionary<string, string> methodTokens, Action<Exception, string>? onError);
}

/// <summary>
/// how it calls functions:
/// func[param1, "param2"]
///
/// this is a simple compiler, do not put ] in a string
/// nor can you do funcA[funcB[]], must be traditional funcA[func_b()]
/// </summary>
public class DefaultLogicCompiler : ILogicCompiler
{
    private Dictionary<string, string> Cache = [];

    public string CompileRule(string rule, Dictionary<string, string> methodTokens, Action<Exception, string>? onError)
    {
        try
        {
            if (rule is "") return "True";
            if (Cache.TryGetValue(rule, out var alreadyCompiledRule)) return alreadyCompiledRule;
            List<string> tokens = [];
            var split = rule.Split(' ');

            for (var i = 0; i < split.Length;)
            {
                var key = split[i];

                if (key is "&&") key = "and";
                if (key is "||") key = "or";
                if (key is "and" or "or" or "(" or ")" || key.All(c => c is '(') || key.All(c => c is ')'))
                {
                    tokens.Add(key);
                    i++;
                    continue;
                }

                if (key.StartsWith("("))
                {
                    tokens.Add(string.Join("", key.TakeWhile(c => c is '(')));
                }

                var end = string.Join("", key.Reverse().TakeWhile(c => c is ')'));
                key = key.TrimStart('(').TrimEnd(')');

                if (key.Contains('['))
                {
                    key = key.Split('[')[0];
                    if (!methodTokens.TryGetValue(key, out var token)) throw new ArgumentException($"Unknown Method Token: [{key}]");
                    var listVer = split.ToList();
                    var lastClosing = listVer.FindIndex(i, s => s.EndsWith("]"));

                    var raw = string.Join(" ", listVer.GetRange(i, lastClosing - i + 1)).Trim();
                    var start = raw.IndexOf('[') + 1;
                    raw = string.Join("", raw.ToList().GetRange(start, raw.LastIndexOf(']') - start));
                    tokens.Add(raw == "" ? $"{token}(state, player)" : $"{token}(state, player, {raw})");

                    i += lastClosing - i + 1;
                }
                else
                {
                    if (!methodTokens.TryGetValue(key, out var token)) throw new ArgumentException($"Unknown Method Token: [{key}]");
                    tokens.Add($"{token}(state, player)");
                    i++;
                }

                if (end.Length == 0) continue;
                tokens.Add(end);
            }

            return Cache[rule] = string.Join(" ", tokens);
        }
        catch (Exception e)
        {
            onError?.Invoke(e, $"Error with logic: [{rule}]");
            return "True";
        }
    }
}