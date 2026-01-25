namespace Tickflo.Core.Exceptions;

public abstract class HttpException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public override string Message { get; } = message;
}

public class BadRequestException(string message) : HttpException(400, message);
public class UnauthorizedException(string message) : HttpException(401, message);
public class ForbiddenException(string message) : HttpException(403, message);
public class NotFoundException(string message) : HttpException(404, message);
public class InternalServerErrorException(string message) : HttpException(500, message);
