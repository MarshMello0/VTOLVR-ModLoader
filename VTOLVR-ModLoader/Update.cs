using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;


public class Update
{
    public string Title { get;  set; }
    public string Date { get;  set; }
    public string Description { get;  set; }
    public Update() { }
    public Update(string title, string date, string description)
    {
        Title = title;
        Date = date;
        Description = description;
    }
}

public class UpdateFeed
{
    public Update[] feed;
}

