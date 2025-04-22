namespace GameDatabase.Entities;

public class DanInfo
{
    public string Title { get; set; } = null!;
    public uint DanId { get; set; }
    public List<DanSong> AryOdaiSong { get; set; } = new();
    public List<DanBorder> AryOdaiBorder { get; set; } = new();
} 
