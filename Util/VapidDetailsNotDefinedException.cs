using System;

public class VapidDetailsNotDefinedException : Exception
{
    public VapidDetailsNotDefinedException()
    {
    }

    public VapidDetailsNotDefinedException(string message)
        : base(message)
    {
    }

    public VapidDetailsNotDefinedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}