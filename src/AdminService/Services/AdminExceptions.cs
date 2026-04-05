namespace AdminService.Services;

public sealed class AdminValidationException : Exception
{
    public AdminValidationException(string message) : base(message) { }
}

public sealed class AdminConflictException : Exception
{
    public AdminConflictException(string message) : base(message) { }
}

public sealed class AdminNotFoundException : Exception
{
    public AdminNotFoundException(string message) : base(message) { }
}
