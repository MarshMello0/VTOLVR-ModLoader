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
    public int dllVersion { get; set; }
    public DateTime dateTime { get; set; }
    public FileUpdate() { }
    public FileUpdate(int exeVersion, int dllVersion)
    {
        this.exeVersion = exeVersion;
        this.dllVersion = dllVersion;
        dateTime = DateTime.Now;
    }
}