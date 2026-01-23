namespace CreepyUtil.Archipelago.WorldFactory;

public partial class WorldFactory
{
    private RuleFactory? RuleFactory;

    public RuleFactory GetRuleFactory(string link = "No Link Given", ILogicCompiler? logicCompiler = null)
    {
        if (RuleFactory is not null) return RuleFactory;
        RuleFactory = new RuleFactory(this)
        {
            LogicGeneratorLink = link,
            LogicCompiler = logicCompiler ?? new DefaultLogicCompiler(),
        };

        return RuleFactory;
    }
}

public class RuleFactory(WorldFactory worldFactory)
{
    private static readonly string[] DefaultParams = ["state", "player"];

    public string LogicGeneratorLink = "No Link Given"; // see above

    private WorldFactory WorldFactory = worldFactory;
    private Dictionary<string, MethodFactory> LogicMethods = [];
    private Dictionary<string, string> LogicMethodTokens = [];
    private HashSet<string> LogicRules = [];

    public ILogicCompiler LogicCompiler = new DefaultLogicCompiler();

    public RuleFactory AddLogicFunction(string token, string methodName, IEnumerable<string> code, params string[] parameters) => AddLogicFunction(token, methodName, string.Join("\n\t", code), parameters);

    public RuleFactory AddLogicFunction(string token, string methodName, string code, params string[] parameters)
    {
        LogicMethodTokens[token] = methodName;
        LogicMethods[methodName] = new MethodFactory(methodName).AddParams(DefaultParams).AddParams(parameters).SetReturn("bool").AddCode(code);
        return this;
    }

    public RuleFactory AddCompoundLogicFunction(string token, string methodName, string rule, params string[] parameters)
    {
        AddLogicFunction(token, methodName, $"return {LogicCompiler.CompileRule(rule, LogicMethodTokens, WorldFactory.OnCompilerError)}", parameters);
        return this;
    }

    public RuleFactory AddLogicRule(string key, string rule)
    {
        if (rule.Trim() is "") return this;
        LogicRules.Add($"\"{key}\": lambda state: {LogicCompiler.CompileRule(rule.Trim(), LogicMethodTokens, WorldFactory.OnCompilerError)}");
        return this;
    }

    public RuleFactory AddLogicRules(Dictionary<string, string> rules) => rules.Aggregate(this, (factory, pair) => factory.AddLogicRule(pair.Key, pair.Value));

    public void GenerateRulesFile(string fileOutput = "Rules.py", params string[] extraParams)
    {
        var defaultParams = DefaultParams.ToList();
        defaultParams.RemoveAll(s => s is "state");

        var ruleMap = new MethodFactory("get_rule_map")
                     .AddParams(defaultParams.ToArray())
                     .AddParams(extraParams)
                     .AddCode("return {");

        LogicRules.Aggregate(ruleMap, (factory, s) => factory.AddCode($"\t{s},"))
                  .AddCode("}");

        var rulePy = new PythonFactory()
                    .AddObject(new Comment($"File is Auto-generated, see: [{LogicGeneratorLink}]"))
                    .AddObject(ruleMap)
                    .AddObjects(LogicMethods.Values.ToArray<IPythonObject>());

        File.WriteAllText($"{WorldFactory.OutputDirectory}{fileOutput}", rulePy.GetText());
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