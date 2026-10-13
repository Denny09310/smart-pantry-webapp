namespace Client.Components;

public partial class UpdateNotifier
{
    /// <summary>All mutable notifier state in one place; nothing here is a parameter.</summary>
    private sealed class State
    {
        public Bit.Butil.ButilSubscription? Subscription { get; set; }
        public Bit.Butil.ButilSubscription? Visibility { get; set; }
        public bool Shown { get; set; }
        public bool Started { get; set; }
        public bool Reloading { get; set; }
        public bool Checking { get; set; }
        public bool Disposed { get; set; }
    }

    private readonly State _state = new();
}
