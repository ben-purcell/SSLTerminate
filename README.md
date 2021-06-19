# SSLTerminate

Middleware to handle serving an ASP.NET Core application via Kestrel. Written in C#/.NET 5.

Note:
If you simply want a client to interact with an ACME v2 server directly, use [SSLTerminate.ACME](SSLTerminate.ACME/README.md)

## Usage

There are 2 middlewares/handlers that need to be added.

1. Configure Kestrel to respond to HTTPS requests with certificate lookup. This would typically done within ```Program.cs```, 
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

2. Add middleware to respond to http-01 challenges. For this to work in a live environment the http-01 challenge middleware
 **MUST be served on port 80** this is how the ACME protocol is implemented. See the [https://datatracker.ietf.org/doc/html/rfc8555#section-8.3](spec) on
http-01 challenges for details.

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
 As noted in the comment above, it is best to use the Staging Let's Encrypt directory 
until the solution you are implementing has been tested and verified to be working. 
This is to avoid hitting Let's Encrypt [rate limits](https://letsencrypt.org/docs/rate-limits/).

```app.UseHttp01ChallengeHandler()``` is the line that adds the http-01 challenge response middleware.
This needs to be done before ```app.UseHttpsRedirection();``` and other middleware runs, so that it can
handle potential requests for the challenge on port 80.

## Where are certificates/data stored?

Currently the only storage option available is file storage (more options coming soon). There are 3 items that this library concerns itself with storing:

1. ACME account details. A single file, stored by default in: ```<app-path>/stores/acme-account.json```
2. Key Authorizations. A directory, used to store data that is used to respond to http-01 challenges. Default directory: ```<app-path>/stores/key-authz/```
3. Client Certificates. A directory, used to store the created SSL certs created via Let's Encrypt. The default path is: ```<app-path>/stores/client-certs```

It is possible to override one/all of the default storage locations via config at startup:

```csharp

services.AddFileSystemKeyAuthorizationsStore(opts => opts.KeyAuthorizationsPath = <path-to-directory>);

services.AddFileSystemCertificateStore(opts => opts.ClientCertificatePath = <path-to-directory>);

services.AddFileSystemAccountStore(opts => opts.AcmeAccountPath = <path-to-file>);

```

## Limitations

1. Deals with http-01 challenges only, so **no wildcard certs**
2. Storage is file only for now
3. Url's/Ports that the app is served on must be configured using a mechanism that sets the ASPNETCORE_URLS urls
4. To deal with http-01 challenges, the app MUST be open on port 80. SSL traffic will still need the https port open.

## Example

For a simple example, please see the example: [link](Examples/HelloWebApp)
