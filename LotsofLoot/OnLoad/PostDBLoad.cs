using LotsofLoot.Helpers;
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

namespace LotsofLoot.OnLoad
{
    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader + LotsofLootLoadPriority.LotsofLootPriorityOffset)]
    public class PostDBLoad(
        LoadPresetService loadPresetService,
        DatabaseServer databaseServer,
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

            return loadPresetService.OnLoad();
        }
    }
}
