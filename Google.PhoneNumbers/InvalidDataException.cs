using System;

namespace Google.PhoneNumbers
{
    public sealed class InvalidDataException : Exception
    {
        const int Result = unchecked((int)0x80131503);

        public InvalidDataException(string message)
            : base(message)
        {
            HResult = Result;
        }
    }
}
