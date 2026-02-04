using System.Text;

namespace CreepyUtil.Archipelago.WorldFactory;

public partial class WorldFactory
{
    private LocationFactory? LocationFactory;

    public LocationFactory GetLocationFactory(string link = "No Link Given")
    {
        if (LocationFactory is not null) return LocationFactory;
        LocationFactory = new LocationFactory(this)
        {
            LocationGeneratorLink = link,
        };

        return LocationFactory;
    }
}

public class LocationFactory(WorldFactory worldFactory)
{
    public string LocationGeneratorLink = "No Link Given"; // link to (preferably github) of where you use the location generator, inserts it into the .py

    private WorldFactory WorldFactory = worldFactory;
    private Dictionary<string, IEnumerable<string>> LocationVariablesSingle = [];
    private Dictionary<string, IEnumerable<IEnumerable<string>>> LocationVariablesDouble = [];
    private HashSet<string> LocationVariablesFinalList = [];

    /// <param name="selectionModifier">selects from the array what to put in the final location dict, "*[selectionModifier for items in variable]"</param>
    public LocationFactory AddLocations(string variable, IEnumerable<string> locations, string selectionModifier = "items", bool addToFinalList = true)
    {
        LocationVariablesSingle[variable] = locations;

        if (addToFinalList)
        {
            LocationVariablesFinalList.Add($"*[{selectionModifier} for items in {variable}]");
        }

        return this;
    }

    /// <param name="selectionModifier">selects from the array what to put in the final location dict, "*[selectionModifier for items in variable]"</param>
    public LocationFactory AddLocations(string variable, IEnumerable<IEnumerable<string>> locations, string selectionModifier = "items[0]", bool addToFinalList = true)
    {
        LocationVariablesDouble[variable] = locations;

        if (addToFinalList)
        {
            LocationVariablesFinalList.Add($"*[{selectionModifier} for items in {variable}]");
        }

        return this;
    }

    public void GenerateLocationFile(string fileOutput = "Locations.py")
    {
        var locationPy = new PythonFactory()
           .AddObject(new Comment($"File is Auto-generated, see: [{LocationGeneratorLink}]"));

        LocationVariablesSingle.Aggregate(locationPy, (factory, pair) => factory.AddObject(new StringArray(pair.Key, pair.Value)));
        LocationVariablesDouble.Aggregate(locationPy, (factory, pair) => factory.AddObject(new StringDoubleArray(pair.Key, pair.Value)));

        locationPy.AddObject(new StringArray("location_dict", LocationVariablesFinalList, stringify: false));
        File.WriteAllText($"{worldFactory.OutputDirectory}{fileOutput}", locationPy.GetText());
    }
}