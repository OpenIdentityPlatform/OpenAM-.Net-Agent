# OpenAM .Net SDK and IIS policy agent
[![Latest release](https://img.shields.io/github/release/OpenIdentityPlatform/OpenAM-.Net-Agent.svg)](https://github.com/OpenIdentityPlatform/OpenAM-.Net-Agent/releases)
[![Build Status](https://travis-ci.org/OpenIdentityPlatform/OpenAM-.Net-Agent.svg)](https://travis-ci.org/OpenIdentityPlatform/OpenAM-.Net-Agent)
[![Build status](https://ci.appveyor.com/api/projects/status/a518k1mp0a0p95cn/branch/master?svg=true)](https://ci.appveyor.com/project/OpenIdentityPlatfom/openam-net-agent/branch/master)
[![Issues](https://img.shields.io/github/issues/OpenIdentityPlatform/OpenAM-.Net-Agent.svg)](https://github.com/OpenIdentityPlatform/OpenAM-.Net-Agent/issues)
[![Last commit](https://img.shields.io/github/last-commit/OpenIdentityPlatform/OpenAM-.Net-Agent.svg)](https://github.com/OpenIdentityPlatform/OpenAM-.Net-Agent/commits/master)
[![License](https://img.shields.io/badge/license-CDDL-blue.svg)](https://github.com/OpenIdentityPlatform/OpenAM-.Net-Agent/blob/master/LICENSE.md)
[![Gitter](https://img.shields.io/gitter/room/nwjs/nw.js.svg)](http://gitter.im/OpenIdentityPlatform/OpenAM)
[![GitHub top language](https://img.shields.io/github/languages/top/OpenIdentityPlatform/OpenAM-.Net-Agent.svg)](https://github.com/OpenIdentityPlatform/OpenAM-.Net-Agent)
[![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/OpenIdentityPlatform/OpenAM-.Net-Agent.svg)](https://github.com/OpenIdentityPlatform/OpenAM-.Net-Agent)


## Setup and Installation
Identify ${site} folder, where your application files are by finding ${site}/web.config file

### Install binary distribution:
*  Download [binary distribution file](https://github.com/OpenIdentityPlatform/OpenAM-.Net-Agent/releases)
*  Extract archive contents to ${site}/bin

### Setup Policy Agent Logging:
* Create folder ${site}/App_Data/Logs
* Grant write access rights to ${site}/App_Data/Logs folder for account IUSER_XXX

### Application Setup:
Policy agent settings are in ${site}/web.config file. add following settings to \<appSettings\> section:
*  \<add key="com.sun.identity.agents.config.naming.url" value="" /\>
*  \<add key="com.sun.identity.agents.config.organization.name" value="/" /\>
*  \<add key="com.sun.identity.agents.app.username" value="" /\>
*  \<add key="com.iplanet.am.service.password" value="" /\>
*  \<add key="com.sun.identity.agents.config.key" value="" /\> (skip this setting, if password is not encrypted)
*  \<add key="com.sun.identity.agents.config.local.log.path" value="${basedir}/App_Data/Logs"/\> (override log files path)

Settings values provided by OpenAM server administrator or could be found in c:\iis7_agent\Identifier_${site_id}\config\OpenSSOAgentBootstrap.properties file from previous installation.

### Enable Policy Agent
Policy Agent could be enabled in section \<httpModules\> in ${site}/web.config file:
* Remove previous policy agent version:  \<add name="iis7agent" /\>
* Add new policy agent version, by adding entry: \<add name="OpenAM" type="ru.org.openam.iis.OpenAMHttpModule"\>
* Check application functionality and log files in ${site}/App_Data/Logs

IMPORTANT: new section must be first entry after \<httpModules\> tag or after \<clear/\> tag inside \<httpModules\>, if it exists

### Disable Policy Agent
Policy Agent could be disabled in \<httpModules\> section of  ${site}/web.config file:
* Remove entry:  \<add name="OpenAM" type="ru.org.openam.iis.OpenAMHttpModule"\>

### Example Settings
Example settings ${site}/web.config: https://github.com/OpenIdentityPlatform/OpenAM-.Net-Agent/blob/master/ru.org.openam.iis.site.sample/web.config

## Possible Issues

#### System.Net.WebException: The underlying connection was closed: Could not establish trust relationship for the SSL/TLS secure channel
The server uses non-trusted certificate. Add server certificate to trusted list or disable strict certificate check (not recommended in production):

\<add key="com.sun.identity.agents.config.trust.server.certs" value="true"/\>

#### System.Net.WebException: The underlying connection was closed: A connection that was expected to be kept alive was closed by the server. at System.Net.HttpWebRequest.GetResponse()
Networking equipment does not properly handle maintaining keepalive network connections, try to prohibit keepalive connections:

\<add key="org.openidentityplatform.agents.config.keepalive.disable" value="true"/\>
