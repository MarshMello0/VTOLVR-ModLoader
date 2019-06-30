using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Extensions
{
    public static ModItem GetInfo(this Type type)
    {
        VTOLMOD.Info info = type.GetCustomAttributes(typeof(VTOLMOD.Info), true).FirstOrDefault<object>() as VTOLMOD.Info;
        ModItem item = new ModItem(info.name, info.description, info.downloadURL, info.version, true);
        return item;
    }
}
