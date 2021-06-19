# SSLTerminate.ACME

Simple ACME Client written in C#. Intended use is creating free SSL certificates via Let's Encrypt. Compatible and tested end to end with ACME v2 protocol.

## ACME v2 protocol
To use this library, it is advisable to have a basic understanding of how the works. You can find the details here: [rfc8555](https://datatracker.ietf.org/doc/html/rfc8555)

This is another useful link: [Let's Encrypt - How It Works](https://letsencrypt.org/how-it-works/)

The TLDR is:

1. Create account - this is used to create further orders and manage certificates. It is recommended by Let's Encrypt that you create a single account for all orders.
2. Create order for identifier (host) you would like a certificate for
3. Get authorizations for order (you get an authorization for each identifier)
4. Get challenges for authorizations, i.e. http-01 challenge. You will need to complete a challenge for each identifier you requested in the order
5. Generate key authorization and use it to complete challenge
6. Signal you are ready for challenge(s)
7. Complete challenge(s)
8. Finalize order (send CSR)
9. Download certificate

## Usage

### Register with IServiceCollection

This makes ```IAcmeClientFactory``` available via dependency injection in a dotnet core project.

```charp

// points to Let's Encrypt staging url by default
services.AddAcmeServices();

// alternatively, use prod:
services.AddAcmeServices(options: x => x.DirectoryUrl = Directories.LetsEncrypt.Production);

```

Note: It is recommended to point to Let's Encrypt staging environment 
until you have a working solution. This will stop you hitting 
[rate limits](https://letsencrypt.org/docs/rate-limits/)

### Use IAcmeClientFactory factory

Assuming you are using an ASP.NET Core project or similar, add to constructor of some service/controller:

```csharp

public class MyController : Controller
{
    private readonly IAcmeClientFactory _factory;

    public MyController(
        IAcmeClientFactory factory, 
        // other dependencies
        ...)
    {
        _factory = factory;
    }
}

```

### Create client

FYI this will make a call to whatever ACME server you are targetting (i.e. Let's Encrypt) to configure the directory. The directory is a list of endpoints to create and manage ACME accounts/orders

```csharp
var client = _factory.Create();
```

### Create an account

```csharp
var privateKey = new RsaPrivateKey();

var account = await client.CreateAccount(
    AcmeAccountRequest.CreateForEmail(<some contact email goes here>),
    privateKey);

// you will need to keep hold of this for each 
// call to Let's Encrypt involving the same account
var accountKeys = new AcmeAccountKeys
{
    PrivateKey = privateKey,
    KeyId = account.Locaton
};

```

### Place an order

```csharp
var order = await client.CreateOrder(accountKeys, AcmeOrderRequest.CreateForHost("test.com"));
```

### Get order - you can do this to check the order status

```csharp
var order = await client.GetOrder(accountKeys, order.Location);
```

### Get authorizations from order
To get a list of available challenges (i.e. http-01), get the authorizations for an order.

```csharp
var authResponse = await client.GetAuthorizations(accountKeys, authorizationUrl: order.Authorizations.First());
```

### Generate key authorization
You will need to do this to complete a given challenge. For example, if you wanted to 
pass the http-01 challenge, then you will need to serve the key authorization on:

```
<host>/.well-known/acme-challenge/<token>
```

Where ```<host>``` is the host you are ordering a certificate for, and ```<token>``` is the the token from the given challenge.

```csharp
var keyAuthorization = accountKeys.PrivateKey.KeyAuthorization;
```

### Signal ready for challenge
Signal you are ready for one of the known ACME challenges, i.e. http-01
```csharp
var challengeResponse = await client.SignalReadyForChallenge(accountKeys, authResponse.Challenges.First());
```

### Poll server until order changes status
```csharp
order = await client.PollWhileOrderInStatus(accountKeys, order.Location, statuses: new[] { AcmeOrderStatus.Pending });
```

### Finalize order
Assuming you have passed one of the challenges associated with the order (e.g. the http-01 challenge), you can
finalize your order. The status of the order should be ```ready``` and the ```Finalize``` property of the order
should be populated.

```csharp
var csr = ...// code to generate CSR bytes
order = await client.Finalize(accountKeys, order.Finalize, AcmeFinalizeRequest.ForCsr(csr));
```

### Download certificate
To do this, the order must be in the ```valid``` state and the ```Certificate``` property of the order 
should be populated.

```csharp
var pemChain = await client.DownloadCertificate(accountKeys, order.Certificate);
```

## Full example

```csharp

var privateKey = new RsaPrivateKey();

var account = await client.CreateAccount(
    AcmeAccountRequest.CreateForEmail(<some contact email goes here>),
    privateKey);

var accountKeys = new AcmeAccountKeys
{
    PrivateKey = privateKey,
    KeyId = account.Locaton
};

var order = await client.CreateOrder(accountKeys, AcmeOrderRequest.CreateForHost("test.com"));

var authResponse = await client.GetAuthorizations(accountKeys, authorizationUrl: order.Authorizations.First());

var http01Challenge = authResponse.Challenges.First(x => x.Type == "http-01");

var keyAuthorization = accountKeys.PrivateKey.KeyAuthorization(http01Challenge.Token);

// do something here to prepare for which ever challenge you want to accept
// for http-01 you will have to generate a key authorization and serve it
// at a given url
GetReadyToServe(http01Challenge.Token, keyAuthorization);

var challengeResponse = await client.SignalReadyForChallenge(accountKeys, http01Challenge);

// wait for challenge to complete
order = await client.PollWhileOrderInStatus(accountKeys, order.Location, statuses: new[] { AcmeOrderStatus.Pending });

// check order status is ready, otherwise we have a problem
if (order.Status != AcmeOrderStatus.Ready)
    throw new ApplicationException("Expected order to be ready, got: " + order.Status);

// generate certificate private key/request
var certificatePrivateKey = GenerateCertificatePrivateKey();
var csr = GenerateCsr(certificatePrivateKey);

order = await client.Finalize(accountKeys, order.Finalize, AcmeFinalizeRequest.ForCsr(csr))

// poll while order is procesed:
order = await acmeClient.PollWhileOrderInStatus(accountKeys, order.Location, statuses: new[]
{
    AcmeOrderStatus.Ready,
    AcmeOrderStatus.Processing
});

if (order.Status != AcmeOrderStatus.Valid)
    throw new ApplicationException("Expected order to be valid, got: " + order.Status);

// download cert!
var pemChain = await client.DownloadCertificate(accountKeys, order.Certificate);

// do something with cert - store/use/whatever
var certificate = new X509Certificate2(Encoding.ASCII.GetBytes(pemChain))
    .CopyWithRsaPrivateKey(certificatePrivateKey);

// clean up key auth if necessary
CleanUp(http01Challenge.Token, keyAuthorization);
```


## Limitations

* Currently unable to revoke certificates
* Currently only supports RSA encryption
