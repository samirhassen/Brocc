using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.PluginApis
{
    public interface IApplicationDataSourceResponse
    {
        List<string> ItemNames(string dataSourceName);

        string Opt(string datasourceName, string itemName);

        string Req(string datasourceName, string itemName);

        T DeserializeJsonValue<T>(string value);
    }
}