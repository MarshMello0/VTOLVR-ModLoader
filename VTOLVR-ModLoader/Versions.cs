using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;

public class Versions
{
    public int currentDLLVersion;
    public int currentEXEVersion;
    public Versions(){}
    public Versions(int currentDLL, int currentExe)
    {
        currentDLLVersion = currentDLL;
        currentEXEVersion = currentExe;
    }
}
