using SharedProject.Enums;

namespace GameDatabase.Entities;

public class DanSong
{
    public uint DanId { get; set; }
    public uint SongIdx { get; set; }
    public uint SongNo { get; set; }
    public Difficulty Level { get; set; }
    public bool IsHiddenSongName { get; set; }
    public DanInfo? DanInfo { get; set; }
}
