# SSLTerminate

Middleware to handle serving an ASP.NET Core application via Kestrel. Written in C#/.NET 5.

Notes:
* Packages available on Nuget
* If you simply want a client to interact with an ACME v2 server directly, use [SSLTerminate.ACME](SSLTerminate.ACME)

## Usage

Basic usage is a 2 step process:

1. **Configure Kestrel to respond to HTTPS requests with certificate lookup**. This would typically done within ```Program.cs```, 
note the ```options.UseSslTerminateCertificates()``` line:

```csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder
                .UseStartup<Startup>()
                .ConfigureKestrel(options =>
                {
                    // perform certificate lookup when request is https
                    options.UseSslTerminateCertificates();
                });
        });
```

2. **Add middleware to respond to http-01 challenges**. In a live environment http-01 challenge middleware
 **MUST be served on port 80** - this is a limitation in how the ACME protocol is implemented. 
 See the [spec](https://datatracker.ietf.org/doc/html/rfc8555#section-8.3) on http-01 challenges for details.

This would typically be done within ```Startup.cs``` and looks like:

```csharp

public void ConfigureServices(IServiceCollection services)
{
    services.AddSslTerminate(options =>
        {
            // account contact, used to create ACME account
            options.AccountContacts = new[]
            {
                "account-contact@example.com"
            };

            // list of hosts to allow ssl service for
            options.AllowHosts = new[] { "hosta.com", "hostb.com" }

            // this is required to configure the ACME client.
            // it is recommended to use Staging until you have 
            // tested the solution, to avoid hitting Let's Encrypt
            // rate limits
            options.DirectoryUrl = Directories.LetsEncrypt.Staging;
        });

    // configure other services/dependencies...
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    // listen for http 01 challenges where necessary
    app.UseHttp01ChallengeHandler();

    // this should be done after app.UseHttp01ChallengeHandler();
    app.UseHttpsRedirection();

    //.. use other middleware, etc
}
```

```services.AddSslTerminate(options => ...)``` is used to add the required services/dependencies.
 As noted in the comment above, it is best to use the Let's Encrypt Staging directory 
until the solution you are implementing has been tested and verified to be working. 
This is to avoid hitting Let's Encrypt [rate limits](https://letsencrypt.org/docs/rate-limits/).

```app.UseHttp01ChallengeHandler()``` is the line that adds the http-01 challenge response middleware.
This needs to be done before ```app.UseHttpsRedirection()``` and other middleware additions, so that it can
handle potential requests for the challenge on port 80.

## Where are certificates/data stored?

There are currently 2 supported options. File Storage and PostgreSQL. Using PostgreSQL allows more flexibility, File Storage is simpler.

### File storage

This is the default behaviour, and you don't have to do anything beyond the previously discussed ```serviceCollection.AddSslTerminate(...)``` 
as described above. Details of the stored data items are described here:

1. ACME account details. A single file, stored by default in: ```<app-path>/stores/acme-account.json```
2. Key Authorizations. A directory, used to store data that is used to respond to http-01 challenges. Default directory: ```<app-path>/stores/key-authz/```
3. Client Certificates. A directory, used to store the created SSL certs created via Let's Encrypt. The default path is: ```<app-path>/stores/client-certs```

It is possible to override one/all of the default storage locations via config at startup:

```csharp

services.AddFileSystemKeyAuthorizationsStore(opts => opts.KeyAuthorizationsPath = <path-to-directory>);

services.AddFileSystemCertificateStore(opts => opts.ClientCertificatePath = <path-to-directory>);

services.AddFileSystemAccountStore(opts => opts.AcmeAccountPath = <path-to-file>);

```

### PostgreSQL storage

#### Storing core data types in PostgreSQL
You have the option to store SSL certificates and all of the required data in PostgreSQL. Doing so will allow 
provides the following advantages over file storage:

* Multiple web servers can share the same storage
* If a web server needs to be terminated, we won't lose the stored data
* Supports adding/removing whitelisted hosts dynamically

Add a reference to SSLTerminate.Storage.Postgres. This is available on Nuget.

Each data type we need to store goes into its own table:

* ACME account details
* Key Authorizations
* Client Certificates

To enable storage for all of the data items, add the following in Startup.cs within ```ConfigureServices```:

```csharp
services.AddPostgresStores(options => options.ConnectionString = "<postgres-connection-string>");
```

**alternatively**

If you need more control over which stores are postgres vs some other option, you can add the stores 1 at a time
during ```Startup.cs``` within ```ConfigureServices```:

```csharp
serviceCollection.AddPostgresAcmeAccountStore(options => options.ConnectionString = "<postgres-connection-string>");
serviceCollection.AddPostgresClientCertificateStore(options => options.ConnectionString = "<postgres-connection-string>");
serviceCollection.AddPostgresKeyAuthorizationsStore(options => options.ConnectionString = "<postgres-connection-string>");
```

#### Storing whitelist in PostgreSQL
We can store whitelisted hosts in PostgreSQL. This allows us to support adding/removing hosts from the whitelist dynamically:

```csharp
serviceCollection.AddPostgresWhitelist(options => options.ConnectionString = "<postgres-connection-string>");
```

## Limitations

1. Deals with http-01 challenges only, so **no wildcard certs**
2. Url's/Ports that the app is served on must be configured using a mechanism that sets the ASPNETCORE_URLS environment variable
3. To deal with http-01 challenges, the app MUST be open on port 80. SSL traffic will still need the https port open

## Example

For a simple example, please see: [link](Examples/HelloWebApp)
