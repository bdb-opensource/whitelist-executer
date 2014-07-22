# whitelist-executer

## What is it?

Whitelist Executer is a simple tool for securely managing Windows-based production environments. It consists of a *web frontend* that controls a number of *agents* running on different servers. The agents allow executing a static set of commands, deliberately not allowing arbitrary script execution. 

The current set of commands assumes your production environment uses git-based deployment ("git pull" on the server to get the latest production binaries). As for the deployed applications, whitelist executer commands support two use cases:

- Static files that don't need extra setup for deployment
- Windows services that require stop/start when deploying


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

The agent (which runs as a windows service) supports the following appkeys in app.config:

- BaseDirs - semicolon-separated list of directories under which the agent searches for git repositories to manage.
- GitExe - full path to git executable (should be cygwin git or msysgit `bin` directory)
- ProcessTimeoutSeconds - how long to wait on a command before timing out
- ServicesFilePath - name of file to look for in each repository that contains a line-by-line list of windows service names.

#### Example

    <add key="BaseDirs" value="D:\Deployment;C:\Web\Images"/>
    <add key="GitExe" value="c:\cygwin\bin\git.exe"/>
    <add key="ProcessTimeoutSeconds" value="120"/>
    <add key="ServicesFilePath" value="services.txt"/>
    
### Web app

The web app's web.config contains a list of WCF client endpoints, which are used to determine the addresses of agents that should be managed. 

#### Example

The following fragment from web.config includes two agents. One is called 'Web' and runs on the same server as the web app; the other is called 'Backend' and runs on a machine called backend.

    <system.serviceModel>
        <client>
          <endpoint name="Web" contract="WhitelistExecuter.Lib.IWhitelistExecuter" binding="basicHttpBinding" address="http://localhost:10000/"/>
          <endpoint name="Backend" contract="WhitelistExecuter.Lib.IWhitelistExecuter" binding="basicHttpBinding" address="http://backend:10000/"/>
        </client>
    </system.serviceModel>
  
## Deployment

- Web app: use standard procedures for deploying an ASP.NET web application (right click on project and select 'Publish...' or use some other method).
- Agents: copy the resulting binaries from bin\Debug to the production machine, and install the executable as a windows service.

