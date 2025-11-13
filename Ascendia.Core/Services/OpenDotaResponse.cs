namespace Ascendia.Core.Services
{
    public record class OpenDotaResponse<T>(T? Value, bool Valid, bool LimitReached)
    {
        public static OpenDotaResponse<T> Invalid => new(default, false, false);
    }
}