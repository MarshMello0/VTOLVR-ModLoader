using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Data
{
    public Update[] updateFeed;
    public FileUpdate[] fileUpdates;
    public Data() { }
}

public class Update
{
    public string Title { get; set; }
    public string Date { get; set; }
    public string Description { get; set; }
    public Update() { }
    public Update(string title, string date, string description)
    {
        Title = title;
        Date = date;
        Description = description;
    }
}

public class FileUpdate
{
    public int exeVersion { get; set; }
    public ModLoaderDLL dll {get;set;}
    public DateTime dateTime { get; set; }
    public FileUpdate() { }
    public FileUpdate(int exeVersion, ModLoaderDLL dll)
    {
        this.exeVersion = exeVersion;
        this.dll = dll;
        dateTime = DateTime.Now;
    }
}

public class ModLoaderDLL
{
    public int version { get; set; }
    public string hash { get; set; }
    public ModLoaderDLL() { }
    public ModLoaderDLL(int version, string hash)
    {
        this.version = version;
        this.hash = hash;
    }
}