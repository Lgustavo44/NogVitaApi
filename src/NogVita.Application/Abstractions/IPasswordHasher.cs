using System;
using System.Collections.Generic;
using System.Text;

namespace NogVita.Application.Abstractions
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string passwordHash, string password);
    }
}
