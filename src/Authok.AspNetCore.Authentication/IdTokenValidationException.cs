using System;

namespace Authok.AspNetCore.Authentication
{
    internal class IdTokenValidationException : Exception
    {
        public IdTokenValidationException(string message): base(message)
        {

        }
    }
}
