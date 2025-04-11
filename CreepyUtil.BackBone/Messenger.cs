using ImGuiNET;
using static ImGuiNET.ImGuiInputTextFlags;

namespace CreepyUtil.BackBone;

public abstract class Messenger<TMessageType>
{
    public static int MaxScrollback
    {
        get => LimitedQueue<TMessageType>.Limit;
        set => LimitedQueue<TMessageType>.Limit = value;
    } 
    
    public bool ScrollToBottom = true;
    public bool ShowInput = true;
    private LimitedQueue<TMessageType> Scrollback = new();
    private string Input = "";
    private bool ToScroll;

    public void Render()
    {
        var wSize = ImGui.GetContentRegionAvail();
        
        ImGui.BeginChild("messenger", ShowInput ? wSize with { Y = wSize.Y - 50 * RlImgui.GetScale()} : wSize, ImGuiChildFlags.Borders, ImGuiWindowFlags.HorizontalScrollbar);
        {
            Scrollback.ForEach(RenderMessage);

            if (ScrollToBottom && ToScroll)
            {
                ImGui.SetScrollHereY();
                ToScroll = false;
            }
            else if (ToScroll)
            {
                ToScroll = false;
            }
        }

        ImGui.EndChild();

        try
        {
            if (!ShowInput) return;
            if (!ImGui.InputText("Command", ref Input, 999, EnterReturnsTrue)) return;
            OnSentMessage(Input);
            UpdateScrollBack();
            Input = "";
        }
        catch
        {
            //ignored
        }
    }

    public void SendMessage(TMessageType message)
    {
        if (!Scrollback.Add(message)) return;
        UpdateScrollBack();
    }

    public void UpdateScrollBack()
    {
        if (ScrollToBottom) ToScroll = true;
    }

    protected abstract void OnSentMessage(string message);
    protected abstract void RenderMessage(TMessageType message);
}