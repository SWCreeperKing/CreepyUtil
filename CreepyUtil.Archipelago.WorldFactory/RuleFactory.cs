using System.Text;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;
using static CreepyUtil.Archipelago.WorldFactory.RuleFactory;

namespace CreepyUtil.Archipelago.WorldFactory;

public partial class WorldFactory
{
    private RuleFactory? RuleFactory;

    public RuleFactory GetRuleFactory(string link = "No Link Given", ILogicCompiler? logicCompiler = null,
        bool addStarters = true)
    {
        if (RuleFactory is not null) return RuleFactory;
        RuleFactory = new RuleFactory(this)
        {
            LogicGeneratorLink = link, LogicCompiler = logicCompiler ?? new DefaultLogicCompiler(),
        };

        if (!addStarters) return RuleFactory;
        return RuleFactory
              .AddLogicFunction("yaml", "get_yaml_option", "return options.get_options_map(option).value", "option")
              .AddLogicFunction("hasN", "has_amount", StateHas("item", "amount", false), "item", "amount")
              .AddCompoundLogicFunction("has", "has", "hasN[item, 1]", "item")
              .AddLogicFunction(
                   "any", "has_any",
                   $"return any(has({string.Join(", ", DefaultParams)}, item) for item in items)", "items"
               )
              .AddLogicFunction(
                   "all", "has_all",
                   $"return all(has({string.Join(", ", DefaultParams)}, item) for item in items)", "items"
               );
    }
}

public class RuleFactory(WorldFactory worldFactory)
{
    public static readonly string[] DefaultParams = ["state", "player", "options"];

    public string LogicGeneratorLink = "No Link Given";

    private WorldFactory WorldFactory = worldFactory;
    private Dictionary<string, MethodFactory> LogicMethods = [];

    private Dictionary<string, string> LogicMethodTokens = [];

    // private HashSet<string> LogicRules = [];
    private Dictionary<string, string> LogicRuleAssociations = [];

    public ILogicCompiler LogicCompiler = new DefaultLogicCompiler();

    public RuleFactory AddLogicFunction(string token, string methodName, IEnumerable<string> code,
        params string[] parameters) => AddLogicFunction(token, methodName, string.Join("\n\t", code), parameters);

    public RuleFactory AddLogicFunction(string token, string methodName, string code, params string[] parameters)
    {
        LogicMethodTokens[token] = methodName;
        LogicMethods[methodName] = new MethodFactory(methodName).AddParams(DefaultParams).AddParams(parameters)
                                                                .SetReturn("bool").AddCode(code);
        return this;
    }

    public RuleFactory AddLogicFunction(string token, string methodName, Action<MethodFactory> code,
        params string[] parameters)
    {
        LogicMethodTokens[token] = methodName;
        LogicMethods[methodName] = new MethodFactory(methodName).AddParams(DefaultParams).AddParams(parameters)
                                                                .SetReturn("bool");
        code(LogicMethods[methodName]);
        return this;
    }

    public RuleFactory AddCompoundLogicFunction(string token, string methodName, string rule,
        params string[] parameters)
    {
        AddLogicFunction(token, methodName, $"return {GenerateCompiledRule(rule)}", parameters);
        return this;
    }

    public RuleFactory AddLogicRule(string key, string rule)
    {
        if (rule.Trim() is "") return this;
        LogicRuleAssociations[key] = rule;
        return this;
    }

    public RuleFactory AddLogicRules(Dictionary<string, string> rules) => rules.Aggregate(
        this, (factory, pair) => factory.AddLogicRule(pair.Key, pair.Value)
    );

    public void GenerateRulesFile(string fileOutput = "Rules.py",
        string imports = "import math\nfrom .Locations import *",
        params string[] extraParams)
    {
        var defaultParams = DefaultParams.ToList();
        defaultParams.RemoveAll(s => s is "state");

        var ruleMap = new MethodFactory("get_rule_map")
                     .AddParams(defaultParams.ToArray())
                     .AddParams(extraParams)
                     .AddCode("return {");

        LogicRuleAssociations.Aggregate(
                                  ruleMap,
                                  (factory, pair) => factory.AddCode(
                                      $"\t\"{pair.Key}\": lambda state: {GenerateCompiledRule(pair.Value)},"
                                  )
                              )
                             .AddCode("}");

        var rulePy = new PythonFactory()
                    .AddObject(new Comment($"File is Auto-generated, see: [{LogicGeneratorLink}]"))
                    .AddImports(imports)
                    .AddObject(ruleMap)
                    .AddObjects(LogicMethods.Values.ToArray<IPythonObject>());

        File.WriteAllText($"{WorldFactory.OutputDirectory}{fileOutput}", rulePy.GetText());
    }

    public string GenerateCompiledRule(string rule) => LogicCompiler.CompileRule(
        rule.Trim(), LogicMethodTokens, WorldFactory.OnCompilerError
    );

    public Dictionary<string, string> GetRuleMapAssociations()
        => LogicRuleAssociations.ToDictionary(kv => kv.Key, kv => kv.Value);
}

public interface ILogicCompiler
{
    public string CompileRule(string rule, Dictionary<string, string> methodTokens, Action<Exception, string>? onError);
}

/// <summary>
/// how it calls functions:
/// func[param1, "param2"]
///
/// can not do
/// func[param1, func2]
/// due to param names being mixed up in it 
/// </summary>
public class DefaultLogicCompiler : ILogicCompiler
{
    private static Dictionary<string, string> Cache = [];

    private List<string> TokenizeString(string rule)
    {
        List<string> tokenizedString = [];
        StringBuilder backlog = new(); // variable for placeholding
        var stringChar = 0; // 0 = none, 1 = ', 2 = "

        foreach (var c in rule)
        {
            if (stringChar is not 0)
            {
                if ((c is '\'' && stringChar is 1 || c is '"' && stringChar is 2)
                    && (backlog.Length <= 0 || backlog[^1] is not '\\')) // end string token
                {
                    stringChar = 0;
                    backlog.Append(c);
                    CheckForEndBacklog();
                    continue;
                }

                backlog.Append(c);
                continue;
            }

            switch (c)
            {
                case '"' or '\'':
                    CheckForEndBacklog();
                    stringChar = c is '"' ? 2 : 1;
                    backlog.Append(c);
                    break;
                case '[' or ']' or '(' or ')' or ' ' or ',':
                    CheckForEndBacklog();

                    // apparently the fastest char to string https://stackoverflow.com/a/74744433
                    tokenizedString.Add(new string(c, 1));
                    break;
                default: backlog.Append(c); break;
            }
        }

        CheckForEndBacklog();
        return tokenizedString;

        void CheckForEndBacklog()
        {
            if (backlog.Length == 0) return;
            tokenizedString.Add(backlog.ToString());
            backlog.Clear();
        }
    }

    public string CompileRule(string rule, Dictionary<string, string> methodTokens, Action<Exception, string>? onError)
    {
        if (rule is "") return "True";
        if (Cache.TryGetValue(rule, out var alreadyCompiledRule)) return alreadyCompiledRule;
        List<string> tokens = [];
        List<string> finalRule = [];

        try
        {
            var defParam = string.Join(", ", RuleFactory.DefaultParams);
            Stack<string> beginTokenStack = new();
            Stack<string> endTokenStack = new();
            tokens = TokenizeString(rule);

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                switch (token)
                {
                    case "(" or ")" or " " or ",": finalRule.Add(token); break;
                    case "&&" or "AND" or "And" or "&" or "and": finalRule.Add("and"); break;
                    case "||" or "OR" or "Or" or "|" or "or": finalRule.Add("or"); break;
                    case "[":
                    {
                        finalRule.Add(beginTokenStack.Count != 0 ? "(" : token);
                        if (beginTokenStack.Count != 0)
                        {
                            finalRule.AddRange([defParam, ", "]);
                            endTokenStack.Push(beginTokenStack.Pop());
                        }
                        else endTokenStack.Push("");
                        break;
                    }
                    case "]":
                    {
                        finalRule.Add(endTokenStack.Count != 0 && endTokenStack.Peek() is not "" ? ")" : token);
                        if (endTokenStack.Count != 0) endTokenStack.Pop();
                        break;
                    }
                    default:
                        if (methodTokens.TryGetValue(token, out var method) && beginTokenStack.Count == 0
                                                                            && endTokenStack.Count == 0)
                        {
                            finalRule.Add(method);
                            if (i != tokens.Count - 1 && tokens[i + 1] is "[") beginTokenStack.Push(method);
                            else finalRule.AddRange(["(", defParam, ")"]);
                        }
                        else finalRule.Add(token);
                        break;
                }
            }

            var output = Cache[rule] = string.Join("", finalRule);
            return output;
        }
        catch (Exception e)
        {
            onError?.Invoke(
                e,
                $"Error with logic: [{rule}]\nTokens: [{string.Join(", ", tokens)}]\nFinal: [{string.Join("", finalRule)}]"
            );
        }
        return "True";
    }
}