public class UpdateData
{
    public Update[] Updates;
    public UpdateData() { }

    public UpdateData(Update[] updates)
    {
        Updates = updates;
    }
}

public class Update
{
    public string Title { set; get; }
    public string ChangeLog { set; get; }
    public Item[] Files;
    public Update() { }

    public Update(string title, string changeLog, Item[] files)
    {
        Title = title;
        ChangeLog = changeLog;
        Files = files;
    }
}
public class Item
{
    public string URLDownload;
    public string FileLocation;
    public string FileHash;
    public string FileName;
    public Item() { }

    public Item(string uRLDownload, string fileLocation, string fileHash, string fileName)
    {
        URLDownload = uRLDownload;
        FileLocation = fileLocation;
        FileHash = fileHash;
        FileName = fileName;
    }
}