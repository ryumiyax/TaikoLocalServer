
using SharedProject.Enums;

namespace GameDatabase.Entities;

public class DanSlot
{
    // currently game only support max 256 gaiden (gaiden score limit)
    public uint DanId { get; set; }
    public uint BindDanId { get; set; }
    public uint VerupNo { get; set; }
    public DanInfo? DanInfo { get; set; }
    public DanType DanType { get; set; }
}
