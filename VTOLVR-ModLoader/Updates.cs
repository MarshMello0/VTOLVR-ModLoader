using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Updates
{
    public int exeversion;
    public int dllversion;
    public int assetversion;
    public Updates() { }
    public Updates(int exeversion, int dllversion, int assetversion)
    {
        this.exeversion = exeversion;
        this.dllversion = dllversion;
        this.assetversion = assetversion;
    }
}
