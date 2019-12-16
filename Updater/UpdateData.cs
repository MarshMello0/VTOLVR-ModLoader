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
    public string ChangeLog;
    public Item[] Files;
    public Update() { }

    public Update(string changeLog, Item[] files)
    {
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