using System;

public class WrongVapidPrivateKeyException : Exception
{
    public WrongVapidPrivateKeyException()
    {
    }

    public WrongVapidPrivateKeyException(string message)
        : base(message)
    {
    }

    public WrongVapidPrivateKeyException(string message, Exception inner)
        : base(message, inner)
    {
    }
}