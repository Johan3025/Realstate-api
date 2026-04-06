namespace RealState.Services.Exceptions
{
    public sealed class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}
