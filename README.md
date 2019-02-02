# Volyar

>Automated media transcoding, indexing, and storage.

Getting media files to playback on all platforms is a real chore. There are media library managers and transcoders, but none are simultaneously comprehensive, easy to use, and free.
Volyar is a media library transcoder built to fill the gap.

## Contents
  - [Setup](#setup)
    - [Systemd](#setup-systemd)
  - [Configuration](#configuration)
    - [Web Configuration](#web-configuration)
    - [Database Configuration](#database-configuration)
    - [Misc Configuration](#misc-configuration)
    - [Library Configuration](#library-configuration)
      - [Qualities](#library-quality-configuration)
      - [Storage Backend](#library-storage-configuration)
        - [Filesystem](#library-storage-filesystem-configuration) 
        - [AmazonS3](#library-storage-s3-configuration) 
        - [Azure Files](#library-storage-azure-configuration) 
        - [BunnyCDN](#library-storage-bunny-configuration) 
  - [Web UI](#web-ui)
  - [Web API](#web-api)
  - [Differentials](#differentials)

<a name="setup"></a>
## Setup

This is a dotnet core application, so you'll need the dotnet framework installed. Many platforms have dotnet core available via package management, but if you just want install binaries they're available [here](https://dotnet.microsoft.com/download/dotnet-core/2.1).
You'll also need [ffmpeg, ffprobe](https://ffmpeg.org/) and [mp4box](https://gpac.wp.imt.fr/downloads/) installed.

With the prerequisites handled, clone this repository:
```
>git clone https://github.com/bloomtom/Volyar
```
Enter the repo directory:
```
>cd Volyar
```
Run the build script. This will also update you to the latest commit:
```
>chmod +x build.sh
>./build.sh
```
Now run the run script:
```
>chmod +x run.sh
>./run.sh
Hosting environment: Production
Now listening on: http://0.0.0.0:7014
Application started. Press Ctrl+C to shut down.
```
You can now navigate to the web UI at `http:/localhost:7014/voly/external/ui`.

<a name="setup-systemd"></a>
##### Systemd Service Configuration
A service file is also included. Modify it to your needs, and install it the typical way:
```
sudo cp volyar.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable volyar
sudo systemctl start volyar
sudo systemctl status volyar
```

<a name="configuration"></a>
## Configuration

On first start, a configuration file (`vsettings.json`) is created in the working directory. The default configuration works, but won't be terribly useful without modification.

<a name="web-configuration"></a>
#### Web interface settings
 - `Listen`
   - The IP address to listen on. Use 0.0.0.0 to listen on all interfaces.
 - `Port`
   - The port to listen on for the web UI and APIs.
 - `BasePath`
   - The web UI and API base path. 

<a name="database-configuration"></a>
#### Database settings
 - `DatabaseType`
   - The type of database to use. Accepts: temp, sqlite, sqlserver, mysql.
 - `DatabaseConnection`
   - The database connection to use. Example: `Data Source=mydata.sqlite;`

<a name="dependency-configuration"></a>
#### External Dependency Settings
 - `FFmpegPath`
   - An absolute or relative path to an ffmpeg executable.
 - `FFprobePath`
   - An absolute or relative path to an ffprobe executable.
 - `Mp4BoxPath`
   - An absolute or relative path to an mp4box executable.

<a name="misc-configuration"></a>
#### Misc Settings
 - `TempPath`
   - The global temp path to use. If none is given, the working directory is used.
 - `Parallelization`
   - The number of media files to process at once.
 - `CompleteQueueLength`
   - Specifies the number of items to keep track of after they've gone through conversion.

<a name="library-configuration"></a>
#### Library Settings
Libraries are given as a collection of the following properties.
 - `Name`
   - The library name. Must be unique.
 - `Enable`
   - Set to false to disable processing of this library.
 - `OriginPath`
   - The path to source media items from.
 - `SourceHandling`
   - Defines what should be done with source files after successful processing.
     - none
	   - Nothing will be done to source files.
	 - truncate
	   - Source files will be truncated to zero bytes.
	 - delete
	   - Source files will be deleted.
	   - Avoid using this with DeleteWithSource: true.
 - `DeleteWithSource`
   - If true, transcoded media objects are deleted from the database and storage backend when the source file cannot be found. Avoid using this with SourceHandling: "delete" unless you don't care about your files.
 - `TempPath`
   - The temporary path to store intermediate files when encoding.
 - `ForceFramerate`
   - If not zero, the output framerate is forced to this value on encode.
 - `ValidExtensions`
   - The file extensions allowed for uptake in a scan. Should start with a dot (.mp4, .mkv, etc.)
 - `Qualities`
   - A collection of qualities to encode into.
 - `StorageBackend`
   - The storage backend setting to use for this library.
 
 The qualities and storage backend setting for a library are a bit more complex than the others, so they're broken out below.
 
<a name="library-quality-configuration"></a>
##### Qualities
Qualities determine how media will be encoded. You can configure multiple qualities to take advantage of MPEG DASH adaptive bitrate streaming.
 - `Width`
   - Width of frame in pixels.
 - `Height`
   - Height of frame in pixels.
 - `Bitrate`
   - Bitrate of media in kb/s.
 - `Preset`
   - ffmpeg preset (veryfast, fast, medium, slow, veryslow).
 - `Profile`
   - ffmpeg h264 encoding profile (Baseline, Main, High).
 - `Level`
   - ffmpeg h264 encoding profile level (3.0, 4.0, 4.1...)
 - `PixelFormat`
   - ffmpeg pixel format or pix_fmt (yuv420p for best compatibility).

<a name="library-storage-configuration"></a>
##### Storage Backend
The storage backend determines how media will be stored. You can store to a local disk, or upload media to a cloud provider.
You should only set one of the following, and leave the rest as `null`. Setting more than one will cause only one to be used.
<a name="library-storage-filesystem-configuration"></a>
##### `Filesystem`
```
"StorageBackend": {
  "Filesystem": {
	"Directory": "C:\\Users\\SAMSA\\Documents\\volytest\\library"
  }
},
```
  - `Directory`
    - The filesystem path to store at
<a name="library-storage-s3-configuration"></a>
##### `AmazonS3`
```
"StorageBackend": {
  "AmazonS3": {
    "Filesystem": {
      "AccessKey": "AAAABBBBCCCCDDDDEEEE",
      "ApiKey": "abcdefghijklmnopqrstuvwxyz0123456789ABCD",
      "Endpoint": "apigateway.us-east-1.amazonaws.com",
      "Bucket": "mybucket"
    }
  }
},
```
  - `AccessKey`
    - Your AWS access key.
  - `ApiKey`
    - The API key to use for authentication.
  - `Endpoint`
    - The [endpoint](https://docs.aws.amazon.com/general/latest/gr/rande.html) domain to connect to.
  - `Bucket`
    - The S3 bucket to store files in.
<a name="library-storage-azure-configuration"></a>
##### `Azure`
```
"StorageBackend": {
  "Azure": {
    "Account": "myaccount",
    "SasToken": "?sv=2019-01-01&ss=b&srt=co&sp=abcdef&se=2019-12-31T12:00:00Z&st=2019-12-31T12:00:00Z&spr=https&sig=abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHI",
    "Container": "mycontainer"
  }
},
```
  - `Account`
    - The Azure account to connect to.
  - `SasToken`
    - The Azure SAS token to authenticate with.
  - `Container`
    - The container to store files in.
<a name="library-storage-bunny-configuration"></a>
##### `BunnyCDN`
```
"StorageBackend": {
  "BunnyCDN": {
    "ApiKey": "abcdefgh-ijkl-mnop-qrstuvwxyz01-2345-6789",
    "StorageZone": "myzone"
  }
},
```
  - `ApiKey`
    - Your BunnyCDN API key.
  - `StorageZone`
    - The storage zone to store files in.



<a name="web-ui"></a>
## Web UI

The Web UI can be accessed by default at [http://localhost:7014/voly/external/ui](http://localhost:7014/voly/external/ui). The port and "/voly/" (`BasePath`) component of the URL are configurable. See [Web Configuration](#web-configuration) section for more info.

<a name="web-api"></a>
## Web API

 - `/external/api/version`
   - GET 
     - Returns the program version.


 - `/external/api/conversion/status`
   - GET
     - Returns a collection of items currently being processed, and in queue for processing.

	 
 - `/external/api/conversion/complete`
   - GET
     - Returns a collection of items which have either completed, or errored out (ErrorText != null).

	 
 - `/external/api/media/allmedia`
   - GET
     - Returns a collection of all indexed media from all libraries.


 - `/external/api/media/diff/[transactionId]`
   - GET
     - Returns a [differential](#differentials) between some point, given by the transaction ID, and now.

 - `/internal/api/scan/fullscan`
   - POST
     - Starts a scan across all libraries.


 - `/internal/api/scan/scanlib/[library]`
   - POST
     - Starts a scan of a single library.

<a name="differentials"></a>
## Differentials

Your media library might be quite large, and the queries to `allmedia` quite slow. A much faster way for an external service to determine what's in a library is to keep its local database synchronized with the Volyar database by using differentials. A differential is just a set of data showing what's been added, removed and changed since the last query. Each differential you get will include a `TransactionId`, which can be used for the next diff query to determine what's been changed in the meantime.










