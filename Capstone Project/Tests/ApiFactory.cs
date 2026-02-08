using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests
{
    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        // Keep default for now. If you later need fake DB/config, we can override ConfigureWebHost.
    }
}
