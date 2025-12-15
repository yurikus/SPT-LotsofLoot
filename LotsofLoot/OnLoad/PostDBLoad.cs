using System.Diagnostics;
using LotsofLoot.Helpers;
using LotsofLoot.Models.Preset;
using LotsofLoot.Services;
using LotsofLoot.Utilities;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Templates;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace LotsofLoot.OnLoad
{
    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader + LotsofLootLoadPriority.LotsofLootPriorityOffset)]
    public class PostDBLoad(
        ConfigService configService,
        DatabaseServer databaseServer,
        LocaleCacheService localeCacheService,
        LazyLoadHandlerService lazyLoadHandlerService,
        IEnumerable<IOnPresetUpdate> onPresetUpdates,
        LotsOfLootLogger logger
    ) : IOnLoad
    {
        public Task OnLoad()
        {
            Templates? databaseTemplates = databaseServer.GetTables().Templates;

            if (databaseTemplates is null || databaseTemplates.Items is null || databaseTemplates.Prices is null)
            {
                logger.Critical("Database templates are null, aborting!");

                return Task.CompletedTask;
            }

            localeCacheService.HydrateCache();

            // This only needs initialisation once, it will get the current values out of the config service when a raid is loaded
            lazyLoadHandlerService.OnPostDBLoad();

            // Apply the currently loaded preset
            foreach(IOnPresetUpdate presetUpdate in onPresetUpdates)
            {
                presetUpdate.Apply(configService.LotsofLootPresetConfig);
            }

            return Task.CompletedTask;
        }
    }
}
