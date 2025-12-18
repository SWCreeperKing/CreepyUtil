// See https://aka.ms/new-console-template for more information

using CreepyUtil.Archipelago.PackReader;
using NLua;

Porter.LoggerEvent += obj => Console.WriteLine($"{obj}\n\n");

Porter.LoadPack(@"D:\___ARCH\___poptracker\packs\tunic_sapphiresapphic_2.1.5.zip", out var pack1);
pack1.CreateLuaContext("");
pack1.OnLuaPrintCalled += s => Console.WriteLine($"[FROM LUA] {s}");

pack1.Context.DoString("""
Tracker:AddItems("items/common_pop.json");
Tracker:AddMaps("maps/maps.json");
Tracker:AddLocations("locations/locations_pop_er.json");
Tracker:AddLayouts("layouts/tracker.json");
""");
// pack1.Tracker.AddItems("items/common_pop.json");
// pack1.Tracker.AddMaps("maps/maps.json");
// pack1.Tracker.AddLocations("locations/locations_pop_er.json");
// pack1.Tracker.AddLayouts("layouts/tracker.json");

Porter.LoadPack(@"D:\___ARCH\___poptracker\packs\ahit-poptracker-1.6.0", out var pack2);
pack2.CreateLuaContext("");
pack2.Tracker.AddItems("items/items.json");
pack2.Tracker.AddMaps("maps/maps.json");
pack2.Tracker.AddLocations("locations/deathwish.json");
pack2.Tracker.AddLocations("locations/mafiatown.json");
pack2.Tracker.AddLocations("locations/entrances/spaceship_lab.json");
pack2.Tracker.AddLayouts("layouts/items.json");

Console.WriteLine("e");