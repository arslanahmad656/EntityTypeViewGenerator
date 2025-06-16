using System.Text;

namespace ViewGenerator.App;

static class GeneralHelper
{
    public static string GetFullExceptionMessage(this Exception exception)
    {
        var sb = new StringBuilder();
        Exception? ex = exception;
        while (ex != null)
        {
            sb.AppendLine(ex.Message);
            ex = ex.InnerException;
        }

        return sb.ToString();
    }
}
