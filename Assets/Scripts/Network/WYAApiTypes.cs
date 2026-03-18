using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WhereYouAre.Network
{
    // -----------------------------------------------------------------------
    // Shared item state types — must match the desktop game's backend contract
    // -----------------------------------------------------------------------

    public enum ItemType
    {
        SupplyCache,
        DeadDrop,
        WardenMarker,
        FactionCache
    }

    public enum FactionId
    {
        None,
        Machines,
        Humans,
        Wardens,
        Excommunicado
    }

    [Serializable]
    public class WorldItem
    {
        [JsonProperty("id")]          public string   Id          { get; set; }
        [JsonProperty("type")]        public ItemType Type        { get; set; }
        [JsonProperty("latitude")]    public double   Latitude    { get; set; }
        [JsonProperty("longitude")]   public double   Longitude   { get; set; }
        [JsonProperty("altitude")]    public double   Altitude    { get; set; }
        [JsonProperty("faction")]     public FactionId Faction    { get; set; }
        [JsonProperty("ownerId")]     public string   OwnerId     { get; set; }
        [JsonProperty("contents")]    public List<ItemContents> Contents { get; set; }
        [JsonProperty("placedAt")]    public DateTime PlacedAt   { get; set; }
        [JsonProperty("expiresAt")]   public DateTime? ExpiresAt { get; set; }
        [JsonProperty("claimedBy")]   public string   ClaimedBy  { get; set; }
        [JsonProperty("claimedAt")]   public DateTime? ClaimedAt { get; set; }
    }

    [Serializable]
    public class ItemContents
    {
        [JsonProperty("itemId")]   public string ItemId   { get; set; }
        [JsonProperty("quantity")] public int    Quantity { get; set; }
    }

    [Serializable]
    public class PlaceItemRequest
    {
        [JsonProperty("type")]      public ItemType  Type      { get; set; }
        [JsonProperty("latitude")]  public double    Latitude  { get; set; }
        [JsonProperty("longitude")] public double    Longitude { get; set; }
        [JsonProperty("altitude")]  public double    Altitude  { get; set; }
        [JsonProperty("faction")]   public FactionId Faction   { get; set; }
        [JsonProperty("contents")]  public List<ItemContents> Contents { get; set; }
    }

    [Serializable]
    public class ClaimItemRequest
    {
        [JsonProperty("itemId")]   public string ItemId   { get; set; }
        [JsonProperty("playerId")] public string PlayerId { get; set; }
    }

    [Serializable]
    public class NearbyItemsResponse
    {
        [JsonProperty("items")]  public List<WorldItem> Items  { get; set; }
        [JsonProperty("cursor")] public string          Cursor { get; set; }
    }

    [Serializable]
    public class FactionTerritory
    {
        [JsonProperty("faction")]       public FactionId    Faction       { get; set; }
        [JsonProperty("boundaryPoints")] public List<GeoPoint> BoundaryPoints { get; set; }
        [JsonProperty("strength")]      public float        Strength      { get; set; } // 0-1
    }

    [Serializable]
    public class GeoPoint
    {
        [JsonProperty("lat")] public double Lat { get; set; }
        [JsonProperty("lon")] public double Lon { get; set; }
    }
}
