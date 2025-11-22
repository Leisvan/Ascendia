namespace Ascendia.Core.Services
{
    public record class OpenDotaResponse<T>(T? Value, bool Valid, bool LimitReached)
    {
        public static OpenDotaResponse<T> Invalid => new(default, false, false);

        public int RemainingLastMinutes { get; set; } = 0;
        public int RemainingToday { get; set; } = 0;
        public string? Ip { get; set; }
    }
}