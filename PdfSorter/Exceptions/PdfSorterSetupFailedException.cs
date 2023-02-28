namespace PdfSorter.Exceptions
{
    internal class PdfSorterSetupFailedException : Exception
    {
        internal PdfSorterSetupFailedException()
        {
        }

        internal PdfSorterSetupFailedException(string message)
            : base(message)
        {
        }

        internal PdfSorterSetupFailedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
