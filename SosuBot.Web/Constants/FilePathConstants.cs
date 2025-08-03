namespace SosuBot.Web.Constants;

public static class FilePathConstants
{
    public static readonly string VideoPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "danser", "videos");
    public static readonly string ReplaysPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "danser", "replays");
}