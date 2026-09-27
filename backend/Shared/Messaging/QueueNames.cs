namespace Shared.Messaging;

public static class QueueNames
{
    public const string CvUploaded = "cv.uploaded";
    public const string CvParsed = "cv.parsed";
    public const string CvAnalyzed = "cv.analyzed";

    // İşlenemeyen mesajlar kaybolmasın diye "<kuyruk>.error" kuyruğuna taşınır.
    public static string ErrorQueueFor(string queue) => $"{queue}.error";
}
