using System;

using Serilog;
using Serilog.Core;

namespace RMass
{
    internal static class LogCreator
    {
        public static Logger Create( String name )
        {
            return new LoggerConfiguration().WriteTo
                                            .Console(outputTemplate:
                                                     "[{Timestamp:HH:mm:ss} {Level:u3}] [{Name}] {Message}{NewLine}{Exception}")
                                            .MinimumLevel.Debug()
                                            .Enrich.WithProperty("Name", name)
                                            .Enrich.FromLogContext()
                                            .CreateLogger();
        }
    }
}