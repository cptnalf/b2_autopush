# b2_autopush

C# application to push local files to a backblaze b2 storage bucket.

compares local folders to remote bucket to figure out which files need to be uploaded.
uses an RSA key to encrypt AES key (128bit) which is used to encrypt the contents of the files sent to B2.
each file gets a new key.

uses https://github.com/corywest/B2.NET 

## Commands
* accounts  
  lists known accounts
* addAccount  
  adds an account to the current config.
  supported types:
  - local  
    a local directory  
    uses the '-d' option  
  - b2  
    a backblaze b2 account  
    uses the -u and -p options.  
    currently i think b2 only needs the -p for a key?
* containers  
  pulls the list of buckets or directories from the remote source.
* ls  
  lists the contents of a container.  
  requires the container and account values.  
  can only use the cache, or can contact the remote source.
* sync  
  makes the remote mirror the source by deleting or copying source contents  
  destination files will be encrypted.  
  can either use the cache, or can pull from the remote system before it starts the compare process.  
  supports both filters and exclusions (regex)

## Build

Pull this repository and [B2.NET]https://github.com/corywest/B2.NET  
eg:
```
git clone https://github.com/cptnalf/b2_autopush.git
git clone https://github.com/corywest/B2.NET
```

Note:
> Currently this targets netcoreapp3.1, which B2.net isn't necessarily compatibile with (according to dotnet on linux at least).  So, changing or adding that target is necessary, along with specifying that target framework.
```
foo@bar:/home/foo/src$ cd b2_autopush
foo@bar:/home/foo/src/b2_autopush/b2app$ dotnet build -f netcoreapp3.1
```
