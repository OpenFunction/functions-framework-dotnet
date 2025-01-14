// Copyright 2020, Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using OpenFunction.Framework;
using OpenFunction.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace OpenFunction.Examples.Configuration
{
    /// <summary>
    /// An options class to be bound from the configuration.
    /// </summary>
    public class DatabaseConnectionOptions
    {
        public const string ConfigurationSection = "DbConnection";

        public string Instance { get; set; }
        public string Database { get; set; }
    }

    /// <summary>
    /// A service connecting to a database, configured using <see cref="DatabaseConnectionOptions"/>.
    /// </summary>
    public class DatabaseService
    {
        public string Instance { get; }
        public string Database { get; }

        public DatabaseService(DatabaseConnectionOptions options) =>
            (Instance, Database) = (options.Instance, options.Database);
    }

    /// <summary>
    /// The startup class is provided with a host builder which exposes a service collection
    /// and configuration. This can be used to make additional dependencies available.
    /// In this case, we configure a <see cref="DatabaseService"/> based on the configuration.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            // Bind the connection options based on the current configuration.
            DatabaseConnectionOptions options = new DatabaseConnectionOptions();
            context.Configuration
                .GetSection(DatabaseConnectionOptions.ConfigurationSection)
                .Bind(options);

            // Build the database service from the connection options.
            DatabaseService database = new DatabaseService(options);

            // Add the database service to the service collection.
            services.AddSingleton(database);
        }
    }

    /// <summary>
    /// The actual Cloud Function using the DatabaseService dependency configured in
    /// Startup.
    /// </summary>
    [FunctionsStartup(typeof(Startup))]
    public class Function : IHttpFunction
    {
        private readonly DatabaseService _database;

        public Function(DatabaseService database) =>
            _database = database;

        public async Task HandleAsync(HttpContext context)
        {
            await context.Response.WriteAsync($"Retrieving data from instance '{_database.Instance}', database '{_database.Database}'");
        }
    }
}
