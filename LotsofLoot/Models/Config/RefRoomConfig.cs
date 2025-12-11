using SPTarkov.Server.Core.Models.Common;

namespace LotsofLoot.Models.Config
{
    public class RefRoomConfig
    {
        /// <summary>
        /// Ref room loot multiplier, higher = more loot probability
        /// </summary>
        public Dictionary<string, double> Multiplier { get; set; } =
            new Dictionary<string, double>()
            {
                // Shatun
                { "woods", 1 },
                // Grumpy
                { "interchange", 1 },
                // Voron
                { "shoreline", 1 },
                // Leon
                { "lighthouse", 1 },
            };

        /// <summary>
        /// Multiplies the chance for a group of items to spawn in the room, higher number = more common
        /// </summary>
        public Dictionary<MongoId, double> ItemGroups { get; set; } =
            new Dictionary<MongoId, double>()
            {
                // Jewelery group
                { "57864a3d24597754843f8721", 0d },
            };
    }
}
