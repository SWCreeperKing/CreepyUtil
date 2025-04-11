using System.Numerics;
using ImGuiNET;
using Raylib_cs;

namespace CreepyUtil.BackBone;

public abstract class Backbone
{
    public const ImGuiTableFlags TableFlags =
        ImGuiTableFlags.Borders
        | ImGuiTableFlags.RowBg
        | ImGuiTableFlags.BordersInner | ImGuiTableFlags.BordersOuter
        | ImGuiTableFlags.BordersH | ImGuiTableFlags.BordersV
        | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersInnerV
        | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.BordersOuterV;

    public const ImGuiChildFlags ChildFlags = ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeX;

    public const ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration |
                                                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;

    public Backbone(string title, int windowWidth = 1200, int windowHeight = 800)
    {
        Raylib.SetTargetFPS(60);
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(windowWidth, windowHeight, title);
        RlImgui.Setup();
    }

    public void Loop()
    {
        Init();
        while (!Raylib.WindowShouldClose())
        {
            Update();
            RenderWrapper();
        }
    }

    private void RenderWrapper()
    {
        Vector2 windowSize = new(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.RayWhite);
        RlImgui.Begin();

        // imgui window setup
        ImGui.PushFont(RlImgui.CascadiaCode);
        if (ImGui.Begin("Test", WindowFlags))
        {
            ImGui.SetWindowSize(windowSize);
            ImGui.SetWindowPos(Vector2.Zero);
            Render();
        }

        ImGui.End();

        ImGui.PopFont();
        RlImgui.End();
        Raylib.EndDrawing();
    }

    public abstract void Init();
    public abstract void Update();
    public abstract void Render();
}