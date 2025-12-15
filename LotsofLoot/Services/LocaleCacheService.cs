using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace LotsofLoot.Services;

[Injectable(InjectionType.Singleton)]
public class LocaleCacheService(LocaleService localeService, DatabaseServer databaseServer)
{
    private readonly Dictionary<MongoId, string> _itemLocaleCache = [];

    public void HydrateCache()
    {
        var localeDb = localeService.GetLocaleDb();

        foreach ((MongoId itemId, TemplateItem item) in databaseServer.GetTables().Templates.Items)
        {
            var localeId = $"{itemId} Name";

            if (localeDb.TryGetValue(localeId, out var locale))
            {
                _itemLocaleCache.TryAdd(itemId, locale);
            }
            else
            {
                if (item.Name is not null)
                {
                    _itemLocaleCache.TryAdd(itemId, item.Name);
                }
            }
        }
    }

    public IEnumerable<MongoId> FetchCacheKeys()
    {
        return _itemLocaleCache.Keys;
    }

    public string FetchLocale(string id)
    {
        if (_itemLocaleCache.TryGetValue(id, out var cachedLocale))
        {
            return cachedLocale;
        }

        var localeDb = localeService.GetLocaleDb();
        var localeId = $"{id} Name";

        if (localeDb.TryGetValue(localeId, out var locale))
        {
            _itemLocaleCache[id] = locale;
            return locale;
        }

        return id;
    }
}
