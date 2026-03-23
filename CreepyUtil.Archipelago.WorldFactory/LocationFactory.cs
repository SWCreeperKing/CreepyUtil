namespace CreepyUtil.Archipelago.WorldFactory;

public partial class WorldFactory
{
    private LocationFactory? LocationFactory;

    public LocationFactory GetLocationFactory(string link = "No Link Given")
    {
        if (LocationFactory is not null) return LocationFactory;
        LocationFactory = new LocationFactory(this) { LocationGeneratorLink = link, };

        return LocationFactory;
    }
}

public class LocationFactory(WorldFactory worldFactory)
{
    public string
        LocationGeneratorLink
            = "No Link Given"; // link to (preferably github) of where you use the location generator, inserts it into the .py

    private List<string> AllLocations = [];
    private WorldFactory WorldFactory = worldFactory;
    private List<IPythonVariable> OtherVariables = [];
    private Dictionary<string, IEnumerable<string>> LocationVariablesSingle = [];
    private Dictionary<string, IEnumerable<IEnumerable<string>>> LocationVariablesDouble = [];
    private HashSet<string> LocationVariablesFinalList = [];

    /// <param name="selectionModifier">selects from the array what to put in the final location dict, "*[selectionModifier for items in variable]"</param>
    public LocationFactory AddLocations(
        string variable, IEnumerable<string> locations, string selectionModifier = "items", bool addToFinalList = true
    )
    {
        LocationVariablesSingle[variable] = locations;

        if (!addToFinalList) return this;
        AllLocations.AddRange(locations);
        LocationVariablesFinalList.Add($"*[{selectionModifier} for items in {variable}]");

        return this;
    }

    /// <param name="selectionModifier">selects from the array what to put in the final location dict, "*[selectionModifier for items in variable]"</param>
    public LocationFactory AddLocations(
        string variable, IEnumerable<IEnumerable<string>> locations, string selectionModifier = "items[0]",
        bool addToFinalList = true, int internalSelectionModifier = 0
    )
    {
        LocationVariablesDouble[variable] = locations;

        if (!addToFinalList) return this;
        AllLocations.AddRange(locations.Select(loc => loc.ElementAt(internalSelectionModifier)));
        LocationVariablesFinalList.Add($"*[{selectionModifier} for items in {variable}]");

        return this;
    }

    public LocationFactory AddIndependentVariable(string name, string value, string addToFinalList = "")
        => AddIndependentVariable(new Variable(name, value), addToFinalList);

    public LocationFactory AddIndependentVariable(IPythonVariable variable, string addToFinalList = "")
    {
        OtherVariables.Add(variable);
        AddToFinalLocationList(addToFinalList);
        return this;
    }

    public LocationFactory AddToFinalLocationList(string addToFinalList)
    {
        if (addToFinalList is "") return this;
        LocationVariablesFinalList.Add(addToFinalList);
        return this;
    }

    public void GenerateLocationFile(
        out string[] locationsArray, string fileOutput = "Locations.py", Action<PythonFactory>? injectCode = null
    )
    {
        var locationPy = new PythonFactory()
           .AddObject(new Comment($"File is Auto-generated, see: [{LocationGeneratorLink}]"));

        LocationVariablesSingle.Aggregate(
            locationPy, (factory, pair) => factory.AddObject(new StringArray(pair.Key, pair.Value))
        );
        LocationVariablesDouble.Aggregate(
            locationPy, (factory, pair) => factory.AddObject(new StringDoubleArray(pair.Key, pair.Value))
        );

        locationPy.AddObjects(OtherVariables.ToArray());

        locationsArray = AllLocations.ToArray();
        locationPy.AddObject(new StringArray("location_dict", LocationVariablesFinalList, stringify: false));

        injectCode?.Invoke(locationPy);

        File.WriteAllText($"{worldFactory.OutputDirectory}{fileOutput}", locationPy.GetText());
    }

    public Dictionary<string, string[][]> ReadLocationsDoubleArray()
        => LocationVariablesDouble.ToDictionary(kv => kv.Key, kv => kv.Value.Select(arr => arr.ToArray()).ToArray());
}