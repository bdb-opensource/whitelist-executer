# whitelist-executer

## What is it?

Whitelist Executer is a simple tool for securely managing Windows-based production environments. It consists of a *web frontend* that controls a number of *agents* running on different servers. The agents allow executing a static set of commands, deliberately not allowing arbitrary script execution. 

The current set of commands assumes your production environment uses git-based deployment ("git pull" on the server to get the latest production binaries). As for the deployed applications, whitelist executer commands support two use cases:

- Static files that don't need extra setup for deployment
- Windows services that require stop/start when deploying


**Important Security Notes**: You are encouraged to limit access to the whitelist executer website using IP address restrictions. For the agents, the current version doesn't limit access to the WCF endpoint. You're encouraged to change the WCF configuration to allow access only from the whitelist executer web app.

## Supported Commands

Please consult [IWhitelistExecutor.cs](https://github.com/bdb-opensource/whitelist-executer/blob/master/WhitelistExecuter.Lib/IWhitelistExecuter.cs) for the most up-to-date list of commands.

- git status
- git fetch
- git pull
- display windows services status
- deploy a windows service (stop services, git pull, then start services)

The commands related to windows services expect a file (normally `services.txt`) to exist in the deployment target. The file is a simple line-by-line list of windows service names.

## Configuration

The deployed whitelist executer web app and agents use app.config/web.config for configuration.

### Agent

#### AppSetting keys

The agent (which runs as a windows service) supports the following appkeys in app.config:

- BaseDirs - semicolon-separated list of directories under which the agent searches for git repositories to manage.
- GitExe - full path to git executable (should be cygwin git or msysgit `bin` directory)
- ProcessTimeoutSeconds - how long to wait on a command before timing out
- ServicesFilePath - name of file to look for in each repository that contains a line-by-line list of windows service names.

#### WCF Service Endpoint

The agent exposes a WCF service endpoint that is also configurable in the app.config. Set the address of the endpoint in accordance to your production policies / firewall configuration so that the web app on the web server will be able to access the service.



#### Example app keys (appSettings) configuration

    <add key="BaseDirs" value="D:\Deployment;C:\Web\Images"/>
    <add key="GitExe" value="c:\cygwin\bin\git.exe"/>
    <add key="ProcessTimeoutSeconds" value="120"/>
    <add key="ServicesFilePath" value="services.txt"/>
    
    
### Web app

- WCF client endpoints: the web app's web.config contains a list of WCF client endpoints, which are used to determine the addresses of agents that should be managed.
- ASP.Net Identity database: the web app uses [ASP.Net Identity](http://www.asp.net/identity/overview/getting-started/aspnet-identity-recommended-resources#gettingstarted) and a SQL database to manage authentication. For development, the connection string is configured to use LocalDB. When hosting on IIS on production, you can change the connection string to use a SQL Server instance or jump through (many) hoops to make LocalDB work on IIS. In any case if the database configured as the data source doesn't exist, it will be created when the web application starts and an initial admin user will be created.

#### Example

The following fragment from web.config includes two agents. One is called 'Web' and runs on the same server as the web app; the other is called 'Backend' and runs on a machine called backend.

    <system.serviceModel>
        <client>
          <endpoint name="Web" contract="WhitelistExecuter.Lib.IWhitelistExecuter" binding="basicHttpBinding" address="http://localhost:10000/"/>
          <endpoint name="Backend" contract="WhitelistExecuter.Lib.IWhitelistExecuter" binding="basicHttpBinding" address="http://backend:10000/"/>
        </client>
    </system.serviceModel>
  
## Deployment

### Requirements

- Web server:
  - IIS (tested on 7.5) with .NET 4.5 installed and registered (see [ASP.NET IIS Registration](http://msdn.microsoft.com/en-us/library/k6h9cz8h%28v=vs.100%29.aspx)). May work with .NET 4.0 but not tested.
  - SQL Server or any other EF-compatible data source. You should change the connection string in web.config to point to your data source.
- Agent servers: Administrative access in order to install the agent windows service.
- "Modern" version of Windows for both web app and agent. Tested on Windows 7 and Windows Server 2008R2.

### Deployment "instructions"

- Web app: use standard procedures for deploying an ASP.NET web application (right click on project and select 'Publish...' or use some other method).
- Agents: copy the resulting binaries from bin\Debug to the production machine, and install the executable as a windows service.
 
Don't forget to edit the web.config (and for the agent, the .exe.config) so that the client and service endpoint addresses match.

Also, see the security notes in the intro at the top of this document.

## Usage

Once the web application and agents have been deployed, you should be able to access the whitelist executer website using a browser.
