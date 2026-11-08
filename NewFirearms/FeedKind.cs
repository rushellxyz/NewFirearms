namespace NewFirearms
{
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum FeedKind
    {
        // Keep these values stable: older JSON configs use numbers.
        Magazine = 1,
        Tube = 2,
        Barrel = 3,
        Revolver = 4,
        Battery = 5
    }
}
